using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

// Local authoring helper for the !work / !volunteer duty system. Serves ui.html and a
// small REST API over the live Duties collection so duties can be listed, authored,
// edited and deleted without hand-writing BSON. Localhost only; started via run.ps1.
//
// The UI exchanges duties in an editor-friendly shape (choices as an ordered array,
// conditionals split into kind/key/value); Normalize() converts that into the exact
// document shape ChateauWork deserializes (dutyResults and rewardList as keyed
// subdocuments, conditional.type as the 3-letter prefix + key, e.g. "jobadventurer").
//
// Compiled with the .NET Framework 4 csc (C# 5) against the bot's own MongoDB driver
// DLLs - keep the syntax C#-5-compatible (no string interpolation / ?. / out var).
class WorkDutyBuilderServer
{
    // Mirrors the duty pickers' switch in ChateauWork/ChateauVolunteer (via
    // DutyConditionalSupport): none = always shown, job = job experience >= value,
    // trn = has the training, cur = holds >= value of the currency, mon = the player's
    // species identifier carries the category.
    static readonly string[] ConditionalKinds = new string[] { "none", "job", "trn", "cur", "mon" };

    static IMongoDatabase _db;
    static string _uiPath;

    static void Main(string[] args)
    {
        string port = "8788";
        string conn = "mongodb://localhost:27017";
        string dbName = "ChateauDb";
        bool openBrowser = false;
        _uiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "ui.html");
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--open") openBrowser = true;
            if (i + 1 >= args.Length) continue;
            if (args[i] == "--port") port = args[i + 1];
            if (args[i] == "--conn") conn = args[i + 1];
            if (args[i] == "--db") dbName = args[i + 1];
            if (args[i] == "--ui") _uiPath = args[i + 1];
        }

        MongoClient client = new MongoClient(conn);
        _db = client.GetDatabase(dbName);

        HttpListener listener = new HttpListener();
        string prefix = "http://localhost:" + port + "/";
        listener.Prefixes.Add(prefix);
        listener.Start();
        Console.WriteLine("Work Duty Builder listening on " + prefix);
        Console.WriteLine("  database: " + dbName + " @ " + conn);
        Console.WriteLine("  ui file:  " + Path.GetFullPath(_uiPath));
        Console.WriteLine("Press Ctrl+C to stop.");

        // --open: launch the default browser once we're listening (used by the
        // desktop-shortcut deployment, which runs this exe without run.ps1).
        if (openBrowser)
            System.Diagnostics.Process.Start(prefix);

        while (true)
        {
            HttpListenerContext ctx = listener.GetContext();
            try
            {
                Handle(ctx);
            }
            catch (Exception exc)
            {
                TryWrite(ctx, 500, "application/json", "{\"error\":" + JsonEscape(exc.Message) + "}");
            }
        }
    }

    static void Handle(HttpListenerContext ctx)
    {
        string method = ctx.Request.HttpMethod;
        string path = ctx.Request.Url.AbsolutePath;

        // CORS: lets the UI drive the API when ui.html is opened outside the server
        // (file:// or an embedded preview panel). The listener only binds localhost and
        // the data is a hobby bot's duty list, so a permissive policy is acceptable.
        if (method == "OPTIONS")
        {
            Write(ctx, 200, "text/plain", "");
            return;
        }

        if (method == "GET" && (path == "/" || path == "/index.html"))
        {
            Write(ctx, 200, "text/html; charset=utf-8", File.ReadAllText(_uiPath, Encoding.UTF8));
            return;
        }
        if (method == "GET" && path == "/api/duties")
        {
            List<BsonDocument> docs = Duties().Find(new BsonDocument()).ToList();
            docs.Sort(delegate(BsonDocument a, BsonDocument b)
            {
                int byJob = string.Compare(Str(a, "job"), Str(b, "job"), StringComparison.OrdinalIgnoreCase);
                if (byJob != 0) return byJob;
                return string.Compare(Str(a, "label"), Str(b, "label"), StringComparison.OrdinalIgnoreCase);
            });
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < docs.Count; i++)
            {
                BsonDocument d = docs[i];
                if (d.Contains("_id"))
                    d["_id"] = d["_id"].ToString();
                if (i > 0) sb.Append(",");
                sb.Append(ToStrictJson(d));
            }
            sb.Append("]");
            Write(ctx, 200, "application/json", sb.ToString());
            return;
        }
        if (method == "GET" && path == "/api/catalogs")
        {
            BsonDocument catalogs = new BsonDocument
            {
                { "jobs", new BsonArray(IdentifierTypes("job")) },
                { "currencies", new BsonArray(IdentifierTypes("currency")) },
                { "trainings", new BsonArray(IdentifierTypes("training")) },
                { "monsterCategories", new BsonArray(MonsterCategories()) },
                { "conditionalKinds", new BsonArray(ConditionalKinds) },
            };
            Write(ctx, 200, "application/json", ToStrictJson(catalogs));
            return;
        }
        if (method == "POST" && path == "/api/duties")
        {
            BsonDocument body = ReadBody(ctx);
            string error;
            BsonDocument doc = Normalize(body, out error);
            if (error != null) { Fail(ctx, error); return; }
            if (LabelExists(Str(doc, "label"), null)) { Fail(ctx, "A duty labeled \"" + Str(doc, "label") + "\" already exists."); return; }
            Duties().InsertOne(doc);
            Ok(ctx, "Inserted \"" + Str(doc, "label") + "\".");
            return;
        }
        if ((method == "PUT" || method == "DELETE") && path.StartsWith("/api/duties/"))
        {
            string label = WebUtility.UrlDecode(path.Substring("/api/duties/".Length));
            BsonDocument existing = Duties().Find(LabelFilter(label)).FirstOrDefault();
            if (existing == null) { Fail(ctx, "No duty labeled \"" + label + "\"."); return; }

            if (method == "DELETE")
            {
                Duties().DeleteOne(LabelFilter(label));
                Ok(ctx, "Deleted \"" + label + "\".");
                return;
            }

            BsonDocument body = ReadBody(ctx);
            string error;
            BsonDocument doc = Normalize(body, out error);
            if (error != null) { Fail(ctx, error); return; }
            // Renames are allowed as long as the new label doesn't collide with a
            // different document.
            if (LabelExists(Str(doc, "label"), existing["_id"])) { Fail(ctx, "Another duty is already labeled \"" + Str(doc, "label") + "\"."); return; }
            doc.InsertAt(0, new BsonElement("_id", existing["_id"]));
            Duties().ReplaceOne(LabelFilter(label), doc);
            Ok(ctx, "Updated \"" + Str(doc, "label") + "\".");
            return;
        }

        Write(ctx, 404, "application/json", "{\"error\":\"not found\"}");
    }

    // ============================ Validation / normalization ============================

    // Rebuilds the incoming editor JSON as a clean document in the exact shape the bot's
    // Duty model deserializes, or reports the first structural problem. The UI sends
    // choices as an ordered array with split conditionals; this produces the keyed
    // dutyResults/rewardList subdocuments (key order is what !work numbers choices by).
    // Key-existence checks for job/currency/training/species keys are left to the UI as
    // soft warnings (identifiers may be seeded later).
    static BsonDocument Normalize(BsonDocument body, out string error)
    {
        error = null;

        string label = Trimmed(body, "label");
        if (string.IsNullOrEmpty(label)) { error = "label is required."; return null; }

        // GetDutiesByJob is an exact match against the job identifier (lowercase), so
        // normalize the authored casing rather than storing a duty no job can roll.
        string job = Trimmed(body, "job");
        if (string.IsNullOrEmpty(job)) { error = "job is required (\"default\" is the fallback set)."; return null; }
        job = job.ToLowerInvariant();

        string startText = Trimmed(body, "startText");
        if (string.IsNullOrEmpty(startText)) { error = "startText is required."; return null; }

        BsonArray categories = new BsonArray();
        BsonValue rawCategories;
        if (body.TryGetValue("categories", out rawCategories) && rawCategories.IsBsonArray)
        {
            foreach (BsonValue c in rawCategories.AsBsonArray)
            {
                if (c.IsString && c.AsString.Trim().Length > 0)
                    categories.Add(c.AsString.Trim());
            }
        }

        BsonValue rawChoices;
        if (!body.TryGetValue("choices", out rawChoices) || !rawChoices.IsBsonArray || rawChoices.AsBsonArray.Count == 0)
        {
            error = "At least one choice is required.";
            return null;
        }

        BsonDocument dutyResults = new BsonDocument();
        bool hasUnconditional = false;
        int choiceIndex = 0;
        foreach (BsonValue rawChoice in rawChoices.AsBsonArray)
        {
            choiceIndex++;
            if (!rawChoice.IsBsonDocument) { error = "Choice " + choiceIndex + " is not an object."; return null; }
            BsonDocument c = rawChoice.AsBsonDocument;

            string choiceName = Trimmed(c, "choiceName");
            if (string.IsNullOrEmpty(choiceName)) { error = "Choice " + choiceIndex + ": choiceName is required."; return null; }

            string resultText = Trimmed(c, "resultText");
            if (string.IsNullOrEmpty(resultText)) { error = "Choice " + choiceIndex + " (\"" + choiceName + "\"): resultText is required."; return null; }

            string kind = OrDefault(Trimmed(c, "conditionalKind"), "none");
            if (!ContainsIgnoreCase(ConditionalKinds, kind)) { error = "Choice " + choiceIndex + ": conditionalKind must be one of: " + string.Join(", ", ConditionalKinds); return null; }
            kind = Canonical(ConditionalKinds, kind);

            string key = OrDefault(Trimmed(c, "conditionalKey"), "");
            int value = Int(c, "conditionalValue", 0);
            if (kind == "none")
            {
                hasUnconditional = true;
                key = "";
                value = 0;
            }
            else
            {
                if (key.Length == 0) { error = "Choice " + choiceIndex + ": the " + kind + " conditional needs a key (which job/training/currency/species tag)."; return null; }
                if (value < 0) { error = "Choice " + choiceIndex + ": conditional value cannot be negative."; return null; }
                if (kind == "trn" || kind == "mon")
                    value = 0; // presence checks; the bot ignores value for these
            }

            BsonDocument rewardList = new BsonDocument();
            BsonValue rawRewards;
            if (c.TryGetValue("rewards", out rawRewards) && rawRewards.IsBsonArray)
            {
                int rewardIndex = 0;
                foreach (BsonValue rawReward in rawRewards.AsBsonArray)
                {
                    rewardIndex++;
                    string where = "Choice " + choiceIndex + " reward " + rewardIndex;
                    if (!rawReward.IsBsonDocument) { error = where + " is not an object."; return null; }
                    BsonDocument r = rawReward.AsBsonDocument;

                    string currency = Trimmed(r, "currency");
                    if (string.IsNullOrEmpty(currency)) { error = where + ": a currency is required."; return null; }

                    // Zero-weight entries aren't "never": the roll's <= comparison can
                    // still land on them, so refuse the ambiguity outright.
                    int weight = Int(r, "weight", 1);
                    if (weight < 1) { error = where + ": weight must be at least 1."; return null; }

                    int min = Int(r, "min", 0);
                    int max = Int(r, "max", 0);
                    if (max < min) { int t = min; min = max; max = t; }

                    rewardList[UniqueKey(rewardList, currency)] = new BsonDocument
                    {
                        { "currency", currency },
                        { "weight", weight },
                        { "min", min },
                        { "max", max },
                    };
                }
            }

            dutyResults[UniqueKey(dutyResults, choiceName)] = new BsonDocument
            {
                { "choiceName", choiceName },
                { "resultText", resultText },
                { "conditional", new BsonDocument
                    {
                        { "type", kind == "none" ? "none" : kind + key },
                        { "value", value },
                    }
                },
                { "rewardList", rewardList },
            };
        }

        if (!hasUnconditional)
        {
            // A player who matches none of the conditionals would receive a duty with no
            // choices and a dead work day - the model requires an always-available choice.
            error = "At least one choice must have no conditional (always available).";
            return null;
        }

        return new BsonDocument
        {
            { "label", label },
            { "categories", categories },
            { "job", job },
            { "startText", startText },
            { "dutyResults", dutyResults },
        };
    }

    // Subdocument field names come from authored text: strip the characters Mongo
    // forbids in keys and suffix duplicates ("copper", "copper_2", ...).
    static string UniqueKey(BsonDocument host, string desired)
    {
        string cleaned = (desired ?? "").Replace(".", "_").TrimStart('$').Trim();
        if (cleaned.Length == 0) cleaned = "entry";
        string candidate = cleaned;
        int n = 2;
        while (host.Contains(candidate))
        {
            candidate = cleaned + "_" + n;
            n++;
        }
        return candidate;
    }

    // ============================ Mongo helpers ============================

    static IMongoCollection<BsonDocument> Duties()
    {
        return _db.GetCollection<BsonDocument>("Duties");
    }

    static FilterDefinition<BsonDocument> LabelFilter(string label)
    {
        return Builders<BsonDocument>.Filter.Regex("label",
            new BsonRegularExpression("^" + Regex.Escape(label) + "$", "i"));
    }

    static bool LabelExists(string label, BsonValue excludeId)
    {
        BsonDocument found = Duties().Find(LabelFilter(label)).FirstOrDefault();
        if (found == null) return false;
        if (excludeId != null && found["_id"].Equals(excludeId)) return false;
        return true;
    }

    static List<string> IdentifierTypes(string category)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.AnyEq("categories", category);
        List<BsonDocument> docs = _db.GetCollection<BsonDocument>("Identifiers").Find(filter).ToList();
        List<string> names = new List<string>();
        foreach (BsonDocument d in docs)
        {
            string name = Str(d, "type");
            if (!string.IsNullOrEmpty(name)) names.Add(name);
        }
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    // The "mon" conditional matches against the categories of the player's species
    // identifier, so offer the union of categories across identifiers tagged "monster"
    // (minus the marker itself). The UI keeps free entry as a fallback.
    static List<string> MonsterCategories()
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.AnyEq("categories", "monster");
        List<BsonDocument> docs = _db.GetCollection<BsonDocument>("Identifiers").Find(filter).ToList();
        SortedSet<string> categories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (BsonDocument d in docs)
        {
            BsonValue c;
            if (!d.TryGetValue("categories", out c) || !c.IsBsonArray) continue;
            foreach (BsonValue v in c.AsBsonArray)
            {
                if (v.IsString && v.AsString.Trim().Length > 0 &&
                    !string.Equals(v.AsString, "monster", StringComparison.OrdinalIgnoreCase))
                    categories.Add(v.AsString.Trim());
            }
        }
        return categories.ToList();
    }

    // ============================ Field helpers ============================

    static string Str(BsonDocument d, string field)
    {
        BsonValue v;
        return d.TryGetValue(field, out v) && v.IsString ? v.AsString : null;
    }

    static string Trimmed(BsonDocument d, string field)
    {
        string s = Str(d, field);
        return s == null ? null : s.Trim();
    }

    static string OrDefault(string s, string fallback)
    {
        return string.IsNullOrEmpty(s) ? fallback : s;
    }

    static int Int(BsonDocument d, string field, int fallback)
    {
        BsonValue v;
        if (!d.TryGetValue(field, out v)) return fallback;
        if (v.IsInt32) return v.AsInt32;
        if (v.IsInt64) return (int)v.AsInt64;
        if (v.IsDouble) return (int)v.AsDouble;
        if (v.IsString)
        {
            int parsed;
            if (int.TryParse(v.AsString, out parsed)) return parsed;
        }
        return fallback;
    }

    static bool ContainsIgnoreCase(string[] values, string candidate)
    {
        return values.Any(delegate(string v) { return string.Equals(v, candidate, StringComparison.OrdinalIgnoreCase); });
    }

    static string Canonical(string[] values, string candidate)
    {
        return values.First(delegate(string v) { return string.Equals(v, candidate, StringComparison.OrdinalIgnoreCase); });
    }

    // ============================ HTTP helpers ============================

    static BsonDocument ReadBody(HttpListenerContext ctx)
    {
        using (StreamReader reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
        {
            return BsonDocument.Parse(reader.ReadToEnd());
        }
    }

    static string ToStrictJson(BsonDocument doc)
    {
        return doc.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict });
    }

    static void Ok(HttpListenerContext ctx, string message)
    {
        Write(ctx, 200, "application/json", "{\"ok\":true,\"message\":" + JsonEscape(message) + "}");
    }

    static void Fail(HttpListenerContext ctx, string message)
    {
        Write(ctx, 400, "application/json", "{\"error\":" + JsonEscape(message) + "}");
    }

    static void Write(HttpListenerContext ctx, int status, string contentType, string body)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
        ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = contentType;
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        ctx.Response.OutputStream.Close();
    }

    static void TryWrite(HttpListenerContext ctx, int status, string contentType, string body)
    {
        try { Write(ctx, status, contentType, body); }
        catch (Exception) { /* response already gone */ }
    }

    static string JsonEscape(string s)
    {
        StringBuilder sb = new StringBuilder("\"");
        foreach (char c in s ?? "")
        {
            if (c == '"') sb.Append("\\\"");
            else if (c == '\\') sb.Append("\\\\");
            else if (c == '\n') sb.Append("\\n");
            else if (c == '\r') sb.Append("\\r");
            else if (c == '\t') sb.Append("\\t");
            else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
            else sb.Append(c);
        }
        return sb.Append("\"").ToString();
    }
}
