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

// Local authoring helper for the B12 random-events system. Serves ui.html and a small
// REST API over the live RandomEvents collection so events can be listed, authored,
// edited and deleted without hand-writing BSON. Localhost only; started via run.ps1.
//
// Compiled with the .NET Framework 4 csc (C# 5) against the bot's own MongoDB driver
// DLLs - keep the syntax C#-5-compatible (no string interpolation / ?. / out var).
class RandomEventBuilderServer
{
    // Mirrors CurseProcessor.CatalogMap keys - the engine's ApplyEventReward validates
    // curse rewards against that map, so only these are grantable. Re-sync if curses
    // are added there.
    static readonly string[] CurseKeys = new string[]
    {
        "meekness", "chastity", "cooties", "costume", "poverty", "laziness", "hunger",
        "greed", "antisocial", "mooing", "tsundere", "blushing", "horny", "bimbo", "vibrating",
    };

    // Mirrors RandomEventEngine.KeywordPool - used only for the UI's announce preview.
    static readonly string[] KeywordPool = new string[]
    {
        "rose", "velvet", "candle", "satin", "amber", "ivory", "ribbon", "lace",
        "ember", "petal", "feather", "crimson", "violet", "honey", "pearl", "thorn",
    };

    static readonly string[] ResponseTypes = new string[] { "none", "keyword", "challenge" };
    static readonly string[] WinnerRules = new string[] { "firstValid", "allInWindow", "nth", "random" };
    static readonly string[] RewardTypes = new string[] { "currency", "title", "training", "corruption", "purity", "curse", "none" };

    static IMongoDatabase _db;
    static string _uiPath;

    static void Main(string[] args)
    {
        string port = "8787";
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
        Console.WriteLine("Random Event Builder listening on " + prefix);
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
        // the data is a hobby bot's event list, so a permissive policy is acceptable.
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
        if (method == "GET" && path == "/api/events")
        {
            List<BsonDocument> docs = Events().Find(new BsonDocument()).ToList();
            docs.Sort(delegate(BsonDocument a, BsonDocument b)
            {
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
                { "currencies", new BsonArray(IdentifierTypes("currency")) },
                { "trainings", new BsonArray(IdentifierTypes("training")) },
                { "curses", new BsonArray(CurseKeys) },
                { "keywordPool", new BsonArray(KeywordPool) },
                { "responseTypes", new BsonArray(ResponseTypes) },
                { "winnerRules", new BsonArray(WinnerRules) },
                { "rewardTypes", new BsonArray(RewardTypes) },
            };
            Write(ctx, 200, "application/json", ToStrictJson(catalogs));
            return;
        }
        if (method == "POST" && path == "/api/events")
        {
            BsonDocument body = ReadBody(ctx);
            string error;
            BsonDocument doc = Normalize(body, out error);
            if (error != null) { Fail(ctx, error); return; }
            if (LabelExists(Str(doc, "label"), null)) { Fail(ctx, "An event labeled \"" + Str(doc, "label") + "\" already exists."); return; }
            Events().InsertOne(doc);
            Ok(ctx, "Inserted \"" + Str(doc, "label") + "\".");
            return;
        }
        if ((method == "PUT" || method == "DELETE") && path.StartsWith("/api/events/"))
        {
            string label = WebUtility.UrlDecode(path.Substring("/api/events/".Length));
            BsonDocument existing = Events().Find(LabelFilter(label)).FirstOrDefault();
            if (existing == null) { Fail(ctx, "No event labeled \"" + label + "\"."); return; }

            if (method == "DELETE")
            {
                Events().DeleteOne(LabelFilter(label));
                Ok(ctx, "Deleted \"" + label + "\".");
                return;
            }

            BsonDocument body = ReadBody(ctx);
            string error;
            BsonDocument doc = Normalize(body, out error);
            if (error != null) { Fail(ctx, error); return; }
            // Renames are allowed as long as the new label doesn't collide with a
            // different document.
            if (LabelExists(Str(doc, "label"), existing["_id"])) { Fail(ctx, "Another event is already labeled \"" + Str(doc, "label") + "\"."); return; }
            doc.InsertAt(0, new BsonElement("_id", existing["_id"]));
            Events().ReplaceOne(LabelFilter(label), doc);
            Ok(ctx, "Updated \"" + Str(doc, "label") + "\".");
            return;
        }

        Write(ctx, 404, "application/json", "{\"error\":\"not found\"}");
    }

    // ============================ Validation / normalization ============================

    // Rebuilds the incoming JSON as a clean document in the exact shape the bot's
    // RandomEvent model deserializes (field order and Int32 numerics included), or
    // reports the first structural problem. Key-existence checks for currency/training
    // are left to the UI as soft warnings (identifiers may be seeded later); curse keys
    // are hard-checked because ApplyEventReward silently grants nothing for unknown ones.
    static BsonDocument Normalize(BsonDocument body, out string error)
    {
        error = null;

        string label = Trimmed(body, "label");
        if (string.IsNullOrEmpty(label)) { error = "label is required."; return null; }

        string announceText = Trimmed(body, "announceText");
        if (string.IsNullOrEmpty(announceText)) { error = "announceText is required."; return null; }

        string responseType = OrDefault(Trimmed(body, "responseType"), "none");
        if (!ContainsIgnoreCase(ResponseTypes, responseType)) { error = "responseType must be one of: " + string.Join(", ", ResponseTypes); return null; }
        responseType = Canonical(ResponseTypes, responseType);

        string winnerRule = OrDefault(Trimmed(body, "winnerRule"), "firstValid");
        if (!ContainsIgnoreCase(WinnerRules, winnerRule)) { error = "winnerRule must be one of: " + string.Join(", ", WinnerRules); return null; }
        winnerRule = Canonical(WinnerRules, winnerRule);

        int weight = Int(body, "weight", 10);
        if (weight < 0) { error = "weight cannot be negative."; return null; }

        int windowSeconds = Int(body, "responseWindowSeconds", 60);
        if (windowSeconds < 0) { error = "responseWindowSeconds cannot be negative."; return null; }

        int winnerN = Int(body, "winnerN", 0);
        if (winnerRule == "nth" && winnerN < 1) { error = "winnerN must be at least 1 for the nth winner rule."; return null; }

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

        BsonValue rawOutcomes;
        if (!body.TryGetValue("outcomes", out rawOutcomes) || !rawOutcomes.IsBsonArray || rawOutcomes.AsBsonArray.Count == 0)
        {
            // With no outcomes the engine resolves to an empty announcement and the
            // event silently grants nothing - refuse rather than author a dud.
            error = "At least one outcome is required.";
            return null;
        }

        BsonArray outcomes = new BsonArray();
        int outcomeIndex = 0;
        foreach (BsonValue rawOutcome in rawOutcomes.AsBsonArray)
        {
            outcomeIndex++;
            if (!rawOutcome.IsBsonDocument) { error = "Outcome " + outcomeIndex + " is not an object."; return null; }
            BsonDocument o = rawOutcome.AsBsonDocument;

            int outcomeWeight = Int(o, "weight", 1);
            if (outcomeWeight < 0) { error = "Outcome " + outcomeIndex + ": weight cannot be negative."; return null; }

            string resultText = Trimmed(o, "resultText");
            BsonArray rewards = new BsonArray();
            BsonValue rawRewards;
            if (o.TryGetValue("rewards", out rawRewards) && rawRewards.IsBsonArray)
            {
                int rewardIndex = 0;
                foreach (BsonValue rawReward in rawRewards.AsBsonArray)
                {
                    rewardIndex++;
                    string where = "Outcome " + outcomeIndex + " reward " + rewardIndex;
                    if (!rawReward.IsBsonDocument) { error = where + " is not an object."; return null; }
                    BsonDocument r = rawReward.AsBsonDocument;

                    string type = OrDefault(Trimmed(r, "type"), "none");
                    if (!ContainsIgnoreCase(RewardTypes, type)) { error = where + ": type must be one of: " + string.Join(", ", RewardTypes); return null; }
                    type = Canonical(RewardTypes, type);

                    string key = OrDefault(Trimmed(r, "key"), "");
                    int min = Int(r, "min", 0);
                    int max = Int(r, "max", 0);
                    if (max < min) { int t = min; min = max; max = t; }

                    bool needsKey = type == "currency" || type == "title" || type == "training" || type == "curse";
                    if (needsKey && key.Length == 0) { error = where + ": a key is required for " + type + " rewards."; return null; }
                    if (type == "curse" && !ContainsIgnoreCase(CurseKeys, key))
                    {
                        error = where + ": \"" + key + "\" is not in the engine's curse catalog (valid: " + string.Join(", ", CurseKeys) + ").";
                        return null;
                    }
                    bool needsAmount = type == "currency" || type == "training" || type == "corruption" || type == "purity";
                    if (needsAmount && max <= 0)
                    {
                        error = where + ": " + type + " rewards need a positive amount (rolled between min and max); use type \"none\" for pure flavor.";
                        return null;
                    }

                    rewards.Add(new BsonDocument
                    {
                        { "type", type },
                        { "key", key },
                        { "min", min },
                        { "max", max },
                    });
                }
            }

            if (string.IsNullOrEmpty(resultText) && rewards.Count == 0)
            {
                error = "Outcome " + outcomeIndex + " has no result text and no rewards - it would resolve to a blank message.";
                return null;
            }

            outcomes.Add(new BsonDocument
            {
                { "weight", outcomeWeight },
                { "resultText", resultText ?? "" },
                { "rewards", rewards },
            });
        }

        return new BsonDocument
        {
            { "label", label },
            { "categories", categories },
            { "weight", weight },
            { "announceText", announceText },
            { "responseType", responseType },
            { "responseWindowSeconds", windowSeconds },
            { "winnerRule", winnerRule },
            { "winnerN", winnerN },
            { "outcomes", outcomes },
        };
    }

    // ============================ Mongo helpers ============================

    static IMongoCollection<BsonDocument> Events()
    {
        return _db.GetCollection<BsonDocument>("RandomEvents");
    }

    static FilterDefinition<BsonDocument> LabelFilter(string label)
    {
        return Builders<BsonDocument>.Filter.Regex("label",
            new BsonRegularExpression("^" + Regex.Escape(label) + "$", "i"));
    }

    static bool LabelExists(string label, BsonValue excludeId)
    {
        BsonDocument found = Events().Find(LabelFilter(label)).FirstOrDefault();
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
