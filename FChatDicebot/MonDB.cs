using FChatDicebot.Database;
using FChatDicebot.Model;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace FChatDicebot
{
    /// <summary>
    /// Static adapter class that maintains backward compatibility with existing code.
    /// All methods delegate to the singleton ChateauDatabase instance.
    /// This allows gradual migration to dependency injection without breaking existing code.
    /// </summary>
    internal class MonDB
    {
        private static IChateauDatabase _database;

        /// <summary>
        /// Initialize the database connection.
        /// Call this once at application startup.
        /// </summary>
        public static void Initialize(string connectionString, string databaseName)
        {
            _database = new ChateauDatabase(connectionString, databaseName);
        }

        /// <summary>
        /// Gets the current database instance.
        /// Used by new code that needs the database instance for DI.
        /// </summary>
        public static IChateauDatabase GetDatabase()
        {
            if (_database == null)
            {
                // Default to production database if not initialized
                Initialize("mongodb://localhost:27017", "ChateauDb");
            }
            return _database;
        }

        // All existing static methods now delegate to the database instance

        internal static string modMessage(string messageLabel)
        {
            return GetDatabase().GetModMessage(messageLabel);
        }

        internal static string registerUserChateau(string userName)
        {
            return GetDatabase().RegisterUserChateau(userName);
        }

        internal static void addPendingCommand(PendingCommand toAdd)
        {
            GetDatabase().AddPendingCommand(toAdd);
        }

        internal static void addInteraction(Interaction toAdd)
        {
            GetDatabase().AddInteraction(toAdd);
        }

        internal static void incrementCount(string userName, string countLabel)
        {
            GetDatabase().IncrementCount(userName, countLabel);
        }

        internal static void decrementCount(string userName, string countLabel)
        {
            GetDatabase().DecrementCount(userName, countLabel);
        }

        internal static void changeCount(string userName, string countLabel, int changeAmount)
        {
            GetDatabase().ChangeCount(userName, countLabel, changeAmount);
        }

        internal static Profile getProfile(string userName)
        {
            return GetDatabase().GetProfile(userName);
        }

        internal static List<Profile> getAllProfiles()
        {
            return GetDatabase().GetAllProfiles();
        }

        internal static List<MonsterStats> getAllMonsterStats()
        {
            return GetDatabase().GetAllMonsterStats();
        }

        internal static void setInteraction(Interaction editedInteraction)
        {
            GetDatabase().SetInteraction(editedInteraction);
        }

        internal static Duty getDuty(string dutyLabel)
        {
            return GetDatabase().GetDuty(dutyLabel);
        }

        internal static void setProfile(string userName, Profile newProfile)
        {
            GetDatabase().SetProfile(userName, newProfile);
        }

        internal static void setDuty(string label, Duty newDuty)
        {
            GetDatabase().SetDuty(label, newDuty);
        }

        internal static List<PendingCommand> getPending(string userName)
        {
            return GetDatabase().GetPendingCommands(userName);
        }

        internal static List<Duty> getDutiesByJob(string job)
        {
            return GetDatabase().GetDutiesByJob(job);
        }

        internal static List<Duty> getDutiesByCategory(string category)
        {
            return GetDatabase().GetDutiesByCategory(category);
        }

        internal static List<Identifier> getIdentifiers(string category)
        {
            return GetDatabase().GetIdentifiersByCategory(category);
        }

        internal static Identifier getIdentifier(string identifier)
        {
            return GetDatabase().GetIdentifier(identifier);
        }

        internal static long getTypeCount(string profileName, string identifierType, string initiatorRecipientOrBoth)
        {
            identifierType = identifierType.ToLower();
            initiatorRecipientOrBoth = initiatorRecipientOrBoth.ToLower();

            switch (initiatorRecipientOrBoth)
            {
                case "initiator":
                    return GetDatabase().CountInteractionsByInitiatorAndType(profileName, identifierType);
                case "recipient":
                    return GetDatabase().CountInteractionsByRecipientAndType(profileName, identifierType);
                default:
                    // For "both", we need to sum initiator and recipient counts
                    int asInitiator = GetDatabase().CountInteractionsByInitiatorAndType(profileName, identifierType);
                    int asRecipient = GetDatabase().CountInteractionsByRecipientAndType(profileName, identifierType);
                    return asInitiator + asRecipient;
            }
        }

        internal static void removePendingInteraction(ObjectId idToRemove)
        {
            GetDatabase().DeletePendingCommand(idToRemove);
        }

        internal static void getInteractions(string location)
        {
            throw new System.NotImplementedException();
        }

        internal static List<Interaction> getInteractionsByInitiator(string initiator)
        {
            return GetDatabase().GetInteractionsByInitiator(initiator);
        }

        internal static List<Interaction> getInteractionsByRecipient(string recipient)
        {
            return GetDatabase().GetInteractionsByRecipient(recipient);
        }

        internal static List<Interaction> getInteractionsByType(string type)
        {
            return GetDatabase().GetInteractionsByType(type);
        }

        internal static void addPendingDuty(PendingDuty dutyToAdd)
        {
            GetDatabase().AddPendingDuty(dutyToAdd);
        }

        internal static void removePendingDuty(ObjectId idToRemove)
        {
            GetDatabase().DeletePendingDuty(idToRemove);
        }

        internal static PendingDuty getPendingDuty(string profileName)
        {
            return GetDatabase().GetPendingDuty(profileName);
        }

        internal static string getDisplayName(string userName)
        {
            return GetDatabase().GetDisplayName(userName);
        }

        internal static Dictionary<string, int> getCurrencies(string userName)
        {
            return GetDatabase().GetCurrencies(userName);
        }

        internal static bool IncrementCountWithRateLimit(string userName, string countLabel, TimeSpan rateLimitDuration)
        {
            return GetDatabase().IncrementCountWithRateLimit(userName, countLabel, rateLimitDuration);
        }

        internal static bool IsCountRateLimited(string userName, string countLabel)
        {
            return GetDatabase().IsCountRateLimited(userName, countLabel);
        }

        internal static string getCharacteristic(string userName, string characteristic)
        {
            return GetDatabase().GetCharacteristic(userName, characteristic);
        }

        internal static void addPledge(Pledge pledge)
        {
            GetDatabase().AddPledge(pledge);
        }

        internal static Pledge getPledge(ObjectId pledgeId)
        {
            return GetDatabase().GetPledge(pledgeId);
        }

        internal static List<Pledge> getPledgesByPledger(string pledger)
        {
            return GetDatabase().GetPledgesByPledger(pledger);
        }

        internal static List<Pledge> getPledgesByPledgee(string pledgee)
        {
            return GetDatabase().GetPledgesByPledgee(pledgee);
        }

        internal static List<Pledge> getActivePledges(string pledger, string pledgee, string interactionType)
        {
            return GetDatabase().GetActivePledges(pledger, pledgee, interactionType);
        }

        internal static void updatePledge(Pledge pledge)
        {
            GetDatabase().UpdatePledge(pledge);
        }

        internal static void deletePledge(ObjectId pledgeId)
        {
            GetDatabase().DeletePledge(pledgeId);
        }

        internal static void addFeedback(FeedbackEntry entry)
        {
            GetDatabase().AddFeedback(entry);
        }

        internal static List<FeedbackEntry> getRecentFeedback(int count)
        {
            return GetDatabase().GetRecentFeedback(count);
        }
    }
}