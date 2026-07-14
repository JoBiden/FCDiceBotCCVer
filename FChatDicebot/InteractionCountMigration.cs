using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.Model;
using MongoDB.Bson.Serialization;

namespace FChatDicebot.Migration
{
    /// <summary>
    /// One-time migration script to backfill interaction counts into user profiles.
    /// This populates the counts dictionary with historical interaction data from the Interactions collection.
    ///
    /// HOW TO USE:
    /// 1. Add this file to your project
    /// 2. Call BackfillInteractionCounts() from your main program or a test harness
    /// 3. Review the console output to verify counts
    /// 4. Run exactly ONCE. This is NOT idempotent (L3): it always re-derives every count
    ///    from the full Interactions collection and adds the result to whatever's already
    ///    in profile.counts, so a second run double-counts everything. If you need to
    ///    re-run it, restore from a backup first — don't run it again on live data.
    /// </summary>
    public class InteractionCountMigration
    {
        private static string connectionString = "mongodb://localhost:27017";
        private static MongoClient monClient = new MongoClient(connectionString);
        private static string dbString = "ChateauDb";

        /// <summary>
        /// Main migration method - backfills all interaction counts for all users
        /// </summary>
        public static void BackfillInteractionCounts()
        {
            Console.WriteLine("=== Starting Interaction Count Migration ===");
            Console.WriteLine($"Time: {DateTime.Now}");
            Console.WriteLine();

            // Get all interactions
            var interactionsCollection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Interactions");
            var allInteractions = interactionsCollection.Find(new BsonDocument()).ToList();

            Console.WriteLine($"Found {allInteractions.Count} total interactions in database");
            Console.WriteLine();

            // Build a dictionary of counts per user
            // Key: userName, Value: Dictionary of (countKey, count)
            Dictionary<string, Dictionary<string, int>> userCounts = new Dictionary<string, Dictionary<string, int>>();

            // Process each interaction
            foreach (var interactionDoc in allInteractions)
            {
                Interaction interaction = BsonSerializer.Deserialize<Interaction>(interactionDoc);

                string interactionType = interaction.type;
                string initiator = interaction.initiator;
                string recipient = interaction.recipient;

                // Determine the count keys based on interaction type
                List<(string userName, string countKey)> countsToIncrement = DetermineCountKeys(interactionType, initiator, recipient);

                // Increment counts for each user
                foreach (var entry in countsToIncrement)
                {
                    if (!userCounts.ContainsKey(entry.userName))
                    {
                        userCounts[entry.userName] = new Dictionary<string, int>();
                    }

                    if (!userCounts[entry.userName].ContainsKey(entry.countKey))
                    {
                        userCounts[entry.userName][entry.countKey] = 0;
                    }

                    userCounts[entry.userName][entry.countKey]++;
                }
            }

            Console.WriteLine($"Processed interactions for {userCounts.Count} unique users");
            Console.WriteLine();

            // Now update each user's profile
            var profilesCollection = monClient.GetDatabase(dbString).GetCollection<Profile>("RegisteredProfiles");
            int profilesUpdated = 0;
            int totalCountsAdded = 0;

            foreach (var userEntry in userCounts)
            {
                string userName = userEntry.Key;
                Dictionary<string, int> counts = userEntry.Value;

                // Get the user's profile
                var filter = Builders<Profile>.Filter.Eq("userName", userName);
                var profile = profilesCollection.Find(filter).FirstOrDefault();

                if (profile == null)
                {
                    Console.WriteLine($"WARNING: User '{userName}' has interactions but no profile. Skipping.");
                    continue;
                }

                // Add counts to profile (adding to existing counts if present)
                foreach (var countEntry in counts)
                {
                    if (profile.counts.ContainsKey(countEntry.Key))
                    {
                        profile.counts[countEntry.Key] += countEntry.Value;
                    }
                    else
                    {
                        profile.counts[countEntry.Key] = countEntry.Value;
                    }
                    totalCountsAdded++;
                }

                // Save the updated profile
                profilesCollection.ReplaceOne(filter, profile);
                profilesUpdated++;

                // Log progress for users with many counts
                if (counts.Count > 5)
                {
                    Console.WriteLine($"Updated {userName}: {counts.Count} different interaction types");
                }
            }

            Console.WriteLine();
            Console.WriteLine("=== Migration Complete ===");
            Console.WriteLine($"Profiles updated: {profilesUpdated}");
            Console.WriteLine($"Total count entries added: {totalCountsAdded}");
            Console.WriteLine($"Time: {DateTime.Now}");
            Console.WriteLine();
            Console.WriteLine("You can now safely delete this migration file if desired.");
        }

        /// <summary>
        /// Determine which count keys should be incremented based on interaction type.
        /// Returns a list of (userName, countKey) tuples to increment.
        /// </summary>
        private static List<(string userName, string countKey)> DetermineCountKeys(string interactionType, string initiator, string recipient)
        {
            var result = new List<(string, string)>();

            switch (interactionType.ToLower())
            {
                // CASUAL INTERACTIONS - both participants get same count
                case "kiss":
                    result.Add((initiator, "kiss"));
                    result.Add((recipient, "kiss"));
                    break;

                case "cuddle":
                    result.Add((initiator, "cuddle"));
                    result.Add((recipient, "cuddle"));
                    break;

                case "handhold":
                    result.Add((initiator, "handhold"));
                    result.Add((recipient, "handhold"));
                    break;

                case "spank":
                    result.Add((initiator, "spankgive"));
                    result.Add((recipient, "spanktake"));
                    break;

                case "bully":
                    result.Add((initiator, "bullygive"));
                    result.Add((recipient, "bullytake"));
                    break;

                case "boobhat":
                    result.Add((initiator, "boobhatgive"));
                    result.Add((recipient, "boobhattake"));
                    break;

                case "lick":
                    result.Add((initiator, "lickgive"));
                    result.Add((recipient, "licktake"));
                    break;

                case "pet":
                    result.Add((initiator, "petgive"));
                    result.Add((recipient, "pettake"));
                    break;

                // lapsit: !lap → initiator is the lap (lapsittake), recipient sits (lapsitgive)
                case "lap":
                    result.Add((initiator, "lapsittake"));
                    result.Add((recipient, "lapsitgive"));
                    break;

                // !sit → initiator sits (lapsitgive), recipient is the lap (lapsittake)
                case "sit":
                    result.Add((initiator, "lapsitgive"));
                    result.Add((recipient, "lapsittake"));
                    break;

                // INVOLVED INTERACTIONS - give/take variants
                case "milk":
                    result.Add((initiator, "milkgive"));
                    result.Add((recipient, "milktake"));
                    break;

                case "paymentgive":
                    result.Add((initiator, "paymentGivegive"));
                    result.Add((recipient, "paymentGivetake"));
                    break;

                case "paymentreceive":
                    result.Add((initiator, "paymentReceivegive"));
                    result.Add((recipient, "paymentReceivetake"));
                    break;

                case "feed":
                    result.Add((initiator, "feedgive"));
                    result.Add((recipient, "feedtake"));
                    break;

                case "golden":
                    result.Add((initiator, "goldengive"));
                    result.Add((recipient, "goldentake"));
                    break;

                case "pledge":
                    result.Add((initiator, "pledge"));
                    result.Add((recipient, "pledge"));
                    break;

                case "dressup":
                    result.Add((initiator, "dressupgive"));
                    result.Add((recipient, "dressuptake"));
                    break;

                case "climax":
                    result.Add((initiator, "climaxgive"));
                    result.Add((recipient, "climaxtake"));
                    break;

                case "bond":
                    result.Add((initiator, "bond"));
                    result.Add((recipient, "bond"));
                    break;

                // COMMITMENT INTERACTIONS - give/take variants
                case "mark":
                    result.Add((initiator, "markgive"));
                    result.Add((recipient, "marktake"));
                    break;

                case "consume":
                    result.Add((initiator, "consumegive"));
                    result.Add((recipient, "consumetake"));
                    break;

                case "petrify":
                    result.Add((initiator, "petrifygive"));
                    result.Add((recipient, "petrifytake"));
                    break;

                case "plant":
                    result.Add((initiator, "plantgive"));
                    result.Add((recipient, "planttake"));
                    break;

                case "objectify":
                    result.Add((initiator, "objectifygive"));
                    result.Add((recipient, "objectifytake"));
                    break;

                case "entitle":
                    result.Add((initiator, "entitlegive"));
                    result.Add((recipient, "entitletake"));
                    break;

                case "breed":
                    result.Add((initiator, "breedgive"));
                    result.Add((recipient, "breedtake"));
                    break;

                case "employ":
                    result.Add((initiator, "employgive"));
                    result.Add((recipient, "employtake"));
                    break;

                case "train":
                    result.Add((initiator, "traingive"));
                    result.Add((recipient, "traintake"));
                    break;

                case "corrupt":
                    result.Add((initiator, "corruptgive"));
                    result.Add((recipient, "corrupttake"));
                    break;

                // CONSEQUENCE INTERACTIONS - give/take variants
                case "monsterize":
                    result.Add((initiator, "monsterizegive"));
                    result.Add((recipient, "monsterizetake"));
                    break;

                case "infest":
                    result.Add((initiator, "infestgive"));
                    result.Add((recipient, "infesttake"));
                    break;

                case "rename":
                    result.Add((initiator, "renamegive"));
                    result.Add((recipient, "renametake"));
                    break;

                case "odorize":
                    result.Add((initiator, "odorizegive"));
                    result.Add((recipient, "odorizetake"));
                    break;

                case "curse":
                    result.Add((initiator, "cursegive"));
                    result.Add((recipient, "cursetake"));
                    break;

                case "break":
                    result.Add((initiator, "breakgive"));
                    result.Add((recipient, "breaktake"));
                    break;

                case "dose":
                    result.Add((initiator, "dosegive"));
                    result.Add((recipient, "dosetake"));
                    break;

                default:
                    // Unknown interaction type - log it but don't fail
                    Console.WriteLine($"WARNING: Unknown interaction type '{interactionType}' - skipping count");
                    break;
            }

            return result;
        }

        /// <summary>
        /// Optional: Generate a report of current counts without modifying the database
        /// </summary>
        public static void GenerateCountReport()
        {
            Console.WriteLine("=== Interaction Count Report (Dry Run) ===");
            Console.WriteLine($"Time: {DateTime.Now}");
            Console.WriteLine();

            var interactionsCollection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Interactions");
            var allInteractions = interactionsCollection.Find(new BsonDocument()).ToList();

            Dictionary<string, Dictionary<string, int>> userCounts = new Dictionary<string, Dictionary<string, int>>();

            foreach (var interactionDoc in allInteractions)
            {
                Interaction interaction = BsonSerializer.Deserialize<Interaction>(interactionDoc);
                var countsToIncrement = DetermineCountKeys(interaction.type, interaction.initiator, interaction.recipient);

                foreach (var entry in countsToIncrement)
                {
                    if (!userCounts.ContainsKey(entry.userName))
                    {
                        userCounts[entry.userName] = new Dictionary<string, int>();
                    }
                    if (!userCounts[entry.userName].ContainsKey(entry.countKey))
                    {
                        userCounts[entry.userName][entry.countKey] = 0;
                    }
                    userCounts[entry.userName][entry.countKey]++;
                }
            }

            // Print top 10 users by interaction count
            var topUsers = userCounts.OrderByDescending(u => u.Value.Values.Sum()).Take(10);
            Console.WriteLine("Top 10 Users by Total Interactions:");
            foreach (var user in topUsers)
            {
                int total = user.Value.Values.Sum();
                Console.WriteLine($"{user.Key}: {total} total interactions");
                foreach (var count in user.Value.OrderByDescending(c => c.Value).Take(3))
                {
                    Console.WriteLine($"  - {count.Key}: {count.Value}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Report complete. No database changes were made.");
        }
    }
}
