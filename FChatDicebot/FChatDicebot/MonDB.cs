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

        public static string modMessage(string messageLabel) {

            messageLabel = messageLabel.ToLower();
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("ModMessages");

            if (messageLabel == "all") //list all modMessage labels instead of retrieving one specifically 
            {
                string messageList = "The available Mod Messages are:";
                var cursor = collection.AsQueryable();
                foreach (var document in cursor)
                {
                    messageList += " " + (string)document.GetElement("name").Value;
                }
                return messageList;
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

        public static string registerUserChateau(string userName) 
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

        public static void addPendingCommand(PendingCommand toAdd)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingCommands");
            collection.InsertOne(toAdd.ToBsonDocument());
        }

        public static void addInteraction(Interaction toAdd)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingCommands");
            collection.InsertOne(toAdd.ToBsonDocument());
        }

        public static void incrementCount(string userName, string countLabel)
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

        public static Profile getProfile(string userName)
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

        public static List<PendingCommand> getPending(string userName)
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

        public static List<Identifier> getIdentifiers(string category)
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

        public static Identifier getIdentifier(string identifier)
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

        public static void removePending(ObjectId toDelete)
        {
            var collection = monClient.GetDatabase(dbString).GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", toDelete);
            collection.DeleteOne(filter);
        }

    }
}
