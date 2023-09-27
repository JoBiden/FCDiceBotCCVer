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

    public class MonDB
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
            var collection = monClient.GetDatabase(dbString).GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();
            if (document.counts.ContainsKey(countLabel))
            {
                document.counts[countLabel]++;
            }
            else
            {
                document.counts.Add(countLabel, 1);
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

        internal static void setProfile(string userName, Profile newProfile)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            collection.ReplaceOne(filter, newProfile.ToBsonDocument());

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

        internal static void removePending(ObjectId toDelete)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", toDelete);
            collection.DeleteOne(filter);
        }

        internal static void getInteractions(string location)
        {
            throw new NotImplementedException();
        }
    }
}
