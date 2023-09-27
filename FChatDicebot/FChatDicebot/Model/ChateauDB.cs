using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    public class Profile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string userName { get; set; }
        public string displayName { get; set; }
        public Dictionary<string, int> counts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> characteristics { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<string>> lists { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, Timer> timers { get; set; } = new Dictionary<string, Timer>();
    }

    public class Timer
    {
        public DateTime timerStart { get; set; } = DateTime.UtcNow;
        public DateTime timerEnd { get; set; }

    }

    public class PendingCommand
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public Interaction pendingInteraction { get; set; } 
        public string awaitingConsentFrom { get; set; } 
        public DateTime startTime { get; set; } = DateTime.UtcNow;

    }

    public class Command
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string commandName { get; set; }
        public string[] aliases { get; set; }
        public string[] neededParams { get; set; }
        public string[] extraParams { get; set; }
        public string description { get; set; }
    }
    public class Identifier
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string[] categories { get; set; }
    }
    public class Interaction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string initiator { get; set; }
        public string recipient { get; set; }
        public string type { get; set; }
        public string identifier { get; set; }
        public string investmentLevel { get; set; }
        public BsonArray extraParameters { get; set; }
        public DateTime interactionTime { get; set; }
    }
}
