using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;

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
        public Dictionary<string, CoolDown> timers { get; set; } = new Dictionary<string, CoolDown>();
        public Dictionary<string, int> currencies { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> jobExperience { get; set; } = new Dictionary<string, int>();
        public List<Title> titles { get; set; } = new List<Title>();
        public int[] displayedTitleSlots { get; set; } = new int[9] { -1, -1, -1, -1, -1, -1, -1, -1, -1 };



    }

    public class Title
    {
        public string titleText { get; set; }
        public string givenBy { get; set; } // "system" for system-granted titles, otherwise character name
        public DateTime grantedTime { get; set; } = DateTime.UtcNow;

        // Helper property to check if this is a system title
        public bool IsSystemTitle => givenBy == "Chateau";

        // Get the formatted title text (with or without markers)
        public string GetFormattedTitle()
        {
            if (IsSystemTitle)
            {
                return $"·{titleText}·";
            }
            return titleText;
        }
    }

    public class CoolDown
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

    public class PendingDuty
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public List<DutyResult> dutyResults { get; set; } = new List<DutyResult>();
        public string dutyLabel { get; set; }
        public string job { get; set; }
        public string awaitingInputFrom { get; set; }
        public DateTime startTime { get; set; } = DateTime.UtcNow.Date;

    }

    public class Duty
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string label { get; set; }
        public string[] categories { get; set; }
        public string job { get; set; }
        public string startText { get; set; }
        public Dictionary<string, DutyResult> dutyResults { get; set; } = new Dictionary<string, DutyResult>(); //must have one result with no condition
    }

    public class DutyResult
    {
        public string choiceName { get; set; }
        public string resultText { get; set; }
        public Conditional conditional { get; set; }
        public Dictionary<string, Reward> rewardList { get; set; } = new Dictionary<string, Reward>();

    }

    public class Reward
    {
        public string currency { get; set; }

        public int weight { get; set; }
        public int min { get; set; }
        public int max { get; set; }
    }

    public class Conditional
    {
        public string type { get; set; }
        public int value { get; set; }
    }

    public class Pledge
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string pledger { get; set; }
        public string pledgee { get; set; }
        public string interactionType { get; set; }
        public string identifier { get; set; }
        public string investmentLevel { get; set; }
        public DateTime pledgeTime { get; set; } = DateTime.UtcNow;
        public string status { get; set; } = "active"; // active, fulfilled, cancelled
        public DateTime? fulfilledTime { get; set; }
        public bool pledgeHonored { get; set; } = false; // true if fulfilled 1+ days after creation

        // Helper to check if this pledge is active
        public bool IsActive => status == "active";
    }
}
