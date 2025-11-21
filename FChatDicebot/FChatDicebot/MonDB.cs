using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using FChatDicebot.Model;
using FChatDicebot.BotCommands;
using System.Collections.ObjectModel;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;

namespace FChatDicebot
{

    internal class MonDB
    {

        static string connectionString = "mongodb://localhost:27017";
        static MongoClient monClient = new MongoClient(connectionString);
        static string dbString = "ChateauDb";

        internal static string modMessage(string messageLabel) {

            messageLabel = messageLabel.ToLower();
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("ModMessages");

            if (messageLabel == "all") //list all modMessage labels instead of retrieving one specifically 
            {
                string messageText = "The available Mod Messages are: \n";
                List<string> modMessages = new List<string>();
                var cursor = collection.AsQueryable();
                foreach (var document in cursor)
                {
                    modMessages.Add((string)document.GetElement("name").Value);
                }
                messageText += Utils.sortedListDisplayText(modMessages);
                return messageText;
            }
            
            var filter = Builders<BsonDocument>.Filter.Eq("name", messageLabel);
            var message = collection.Find(filter).FirstOrDefault();

            if (message == null)
            {
                return "That Mod Message was not found. Try \"!modmessage all\" to get a list of all available messages";
            }
            //message successfully found
            return (string)message.GetElement("text").Value;
        }

        internal static string registerUserChateau(string userName) 
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var profile = collection.Find(filter).FirstOrDefault();
            if (profile == null)
            {
                var toRegister = new Profile();
                toRegister.userName = userName;
                toRegister.displayName = userName;
                collection.InsertOne(toRegister.ToBsonDocument());

                return "Welcome to the Chateau, " + toRegister.displayName + "! We hope you enjoy your time here. Use !help for a list of commands, and feel free to ask around. I promise, we only bite if you want us to~ \n[b]The bot is currently under construction. If you are seeing this message, your profile will almost certainly be reset before release, so don't get too attached![/b]";

            } 
            else //profile already exists - don't register
            {
                return "You're already signed up, " + profile.GetValue("displayName") + ".";
            }
        }

        internal static void addPendingCommand(PendingCommand toAdd)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingCommands");
            collection.InsertOne(toAdd.ToBsonDocument());
        }

        internal static void addInteraction(Interaction toAdd)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Interactions");
            collection.InsertOne(toAdd.ToBsonDocument());
        }

        internal static void incrementCount(string userName, string countLabel)
        {
            changeCount(userName, countLabel, 1);
        }

        internal static void changeCount(string userName, string countLabel, int changeAmount)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();
            if (document.counts.ContainsKey(countLabel))
            {
                document.counts[countLabel]+= changeAmount;
            }
            else
            {
                document.counts.Add(countLabel, changeAmount);
            }
            collection.ReplaceOne(filter, document);

        }

        internal static Profile getProfile(string userName)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();
            if (document != null)
            {
                Profile profile = BsonSerializer.Deserialize<Profile>(document);
                return profile;
            } //no document found
            return null;
            
        }

        internal static void setInteraction(Interaction editedInteraction)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", editedInteraction.Id);
            collection.ReplaceOne(filter, editedInteraction.ToBsonDocument());
        }

        internal static Duty getDuty(string dutyLabel)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Duties");
            var filter = Builders<BsonDocument>.Filter.Eq("label", dutyLabel);
            var document = collection.Find(filter).FirstOrDefault();
            if (document != null)
            {
                Duty duty = BsonSerializer.Deserialize<Duty>(document);
                return duty;
            } //no document found
            return null;

        }

        internal static void setProfile(string userName, Profile newProfile)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            collection.ReplaceOne(filter, newProfile.ToBsonDocument());

        }

        internal static void setDuty(string label, Duty newDuty)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Duties");
            var filter = Builders<BsonDocument>.Filter.Eq("label", label);
            collection.InsertOne(newDuty.ToBsonDocument());

        }

        internal static List<PendingCommand> getPending(string userName)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("awaitingConsentFrom", userName);
            var documents = collection.Find(filter).ToList();
            List<PendingCommand> pendingInteractions = new List<PendingCommand>();
            foreach (var pending in documents) { 
                pendingInteractions.Add(BsonSerializer.Deserialize<PendingCommand>(pending));
            }

            return pendingInteractions;
        }

        internal static List<Duty> getDutiesByJob(string job)
        {
            job = job.ToLower();
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Duties");
            var filter = Builders<BsonDocument>.Filter.Eq("job", job); ;
            var documents = collection.Find(filter).ToList();
            List<Duty> duties = new List<Duty>();
            foreach (var document in documents)
            {
                duties.Add(BsonSerializer.Deserialize<Duty>(document));
            }
            return duties;
        }

        internal static List<Duty> getDutiesByCategory(string category)
        {
            category = category.ToLower();
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Duties");
            var filter = Builders<BsonDocument>.Filter.AnyEq("categories", category);
            var documents = collection.Find(filter).ToList();
            List<Duty> duties = new List<Duty>();
            foreach (var document in documents)
            {
                duties.Add(BsonSerializer.Deserialize<Duty>(document));
            }
            return duties;
        }

        internal static List<Identifier> getIdentifiers(string category)
        {
            category = category.ToLower();
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Identifiers");
            var filter = Builders<BsonDocument>.Filter.AnyEq("categories", category);
            var documents = collection.Find(filter).ToList();
            List<Identifier> identifiers = new List<Identifier>();
            foreach (var identifier in documents)
            {
                identifiers.Add(BsonSerializer.Deserialize<Identifier>(identifier));
            }
            return identifiers;
        }

        internal static Identifier getIdentifier(string identifier)
        {
            identifier = identifier.ToLower();
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Identifiers");
            var filter = Builders<BsonDocument>.Filter.Eq("type", identifier);
            var document = collection.Find(filter).FirstOrDefault();
            if (document != null)
            {
                return BsonSerializer.Deserialize<Identifier>(document);
            } //no document found
            return null;

        }

        internal static long getTypeCount(string profileName, string identifierType, string initiatorRecipientOrBoth)
        {
            identifierType = identifierType.ToLower();
            initiatorRecipientOrBoth = initiatorRecipientOrBoth.ToLower();
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Interactions");
            var builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter;
            switch (initiatorRecipientOrBoth) 
            {
                case "initiator": 
                    filter = builder.Eq("type", identifierType) & builder.Eq("initiator", profileName);
                    break;
                case "recipient":
                    filter = builder.Eq("type", identifierType) & builder.Eq("recipient", profileName);
                    break;
                default:
                    filter = builder.Eq("type", identifierType) & (builder.Eq("initiator", profileName) | builder.Eq("recipient", profileName)) ;
                    break;
            }
            return collection.CountDocuments(filter);
        }

        internal static void removePendingInteraction(ObjectId idToRemove)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", idToRemove);
            collection.DeleteOne(filter);
        }

        internal static void getInteractions(string location)
        {
            throw new NotImplementedException();
        }

        internal static List<Interaction> getInteractionsByInitiator(string initiator)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("initiator", initiator);
            var documents = collection.Find(filter).ToList();
            List<Interaction> interactions = new List<Interaction>();
            foreach (BsonDocument document in documents)
            {
                interactions.Add(BsonSerializer.Deserialize<Interaction>(document));
            }
            return interactions;
        }

        internal static List<Interaction> getInteractionsByRecipient(string recipient)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("recipient", recipient);
            var documents = collection.Find(filter).ToList();
            List<Interaction> interactions = new List<Interaction>();
            foreach (BsonDocument document in documents)
            {
                interactions.Add(BsonSerializer.Deserialize<Interaction>(document));
            }
            return interactions;
        }

        internal static List<Interaction> getInteractionsByType(string type)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("type", type);
            var documents = collection.Find(filter).ToList();
            List<Interaction> interactions = new List<Interaction>();
            foreach (BsonDocument document in documents)
            {
                interactions.Add(BsonSerializer.Deserialize<Interaction>(document));
            }
            return interactions;
        }

        internal static void addPendingDuty(PendingDuty dutyToAdd)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingDuties");
            collection.InsertOne(dutyToAdd.ToBsonDocument());
        }

        internal static void removePendingDuty(ObjectId idToRemove)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingDuties");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", idToRemove);
            collection.DeleteOne(filter);
        }

        internal static PendingDuty getPendingDuty(string profileName)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingDuties");
            var filter = Builders<BsonDocument>.Filter.Eq("awaitingInputFrom", profileName);
            var document = collection.Find(filter).FirstOrDefault();
            if (document != null)
            {
                PendingDuty pending = BsonSerializer.Deserialize<PendingDuty>(document);
                return pending;
            } //no document found
            return null;

        }

        internal static string getDisplayName(string userName)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var projection = Builders<BsonDocument>.Projection.Include("displayName").Include("userName");

            var document = collection.Find(filter).Project(projection).FirstOrDefault();

            if (document != null)
            {
                return document.GetValue("displayName").AsString;
            }
            return null;
        }

        internal static Dictionary<string, int> getCurrencies(string userName)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var projection = Builders<BsonDocument>.Projection.Include("currencies");

            var document = collection.Find(filter).Project(projection).FirstOrDefault();

            if (document != null && document.Contains("currencies"))
            {
                return BsonSerializer.Deserialize<Dictionary<string, int>>(document.GetValue("currencies").AsBsonDocument);
            }
            return new Dictionary<string, int>();
        }

        /// <summary>
        /// Increment a count with rate limiting. Returns true if count was incremented, false if rate limited.
        /// </summary>
        /// <param name="userName">User whose count to increment</param>
        /// <param name="countLabel">The count key to increment</param>
        /// <param name="rateLimitDuration">How long to wait between increments</param>
        /// <returns>True if incremented, false if rate limited</returns>
        internal static bool IncrementCountWithRateLimit(string userName, string countLabel, TimeSpan rateLimitDuration)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var profile = collection.Find(filter).FirstOrDefault();

            if (profile == null) return false;

            // Check if there's a rate limit timer for this specific count
            string timerKey = $"ratelimit_{countLabel}";

            if (profile.timers != null && profile.timers.ContainsKey(timerKey))
            {
                if (DateTime.UtcNow < profile.timers[timerKey].timerEnd)
                {
                    // Still on cooldown, don't increment
                    return false;
                }
            }

            // Not on cooldown - increment count
            if (profile.counts.ContainsKey(countLabel))
            {
                profile.counts[countLabel] += 1;
            }
            else
            {
                profile.counts.Add(countLabel, 1);
            }

            // Set new rate limit timer
            Timer rateLimitTimer = new Timer();
            rateLimitTimer.timerEnd = DateTime.UtcNow.Add(rateLimitDuration);
            profile.timers[timerKey] = rateLimitTimer;

            // Save profile
            collection.ReplaceOne(filter, profile);

            return true;
        }

        /// <summary>
        /// Check if a count increment would be rate limited without actually incrementing
        /// </summary>
        internal static bool IsCountRateLimited(string userName, string countLabel)
        {
            Profile profile = getProfile(userName);
            if (profile == null) return false;

            string timerKey = $"ratelimit_{countLabel}";

            if (profile.timers != null && profile.timers.ContainsKey(timerKey))
            {
                return DateTime.UtcNow < profile.timers[timerKey].timerEnd;
            }

            return false; // No timer exists, so not rate limited
        }

        internal static string getCharacteristic(string userName, string characteristic)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var projection = Builders<BsonDocument>.Projection.Include("characteristics");

            var document = collection.Find(filter).Project(projection).FirstOrDefault();

            if (document != null && document.Contains("characteristics"))
            {
                var characteristics = BsonSerializer.Deserialize<Dictionary<string, string>>(document.GetValue("characteristics").AsBsonDocument);
                return characteristics.ContainsKey(characteristic) ? characteristics[characteristic] : null;
            }
            return null;
        }
    }
}
