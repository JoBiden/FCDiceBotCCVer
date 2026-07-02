using FChatDicebot.Model;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace FChatDicebot.Database
{
    /// <summary>
    /// Interface for Chateau database operations.
    /// Allows for dependency injection and testing with mock databases.
    /// </summary>
    public interface IChateauDatabase
    {
        // Profile Operations
        string RegisterUserChateau(string userName);
        Profile GetProfile(string userName);
        List<Profile> GetAllProfiles();
        void SetProfile(string userName, Profile newProfile);
        void IncrementCount(string userName, string countLabel);
        void DecrementCount(string userName, string countLabel);
        void ChangeCount(string userName, string countLabel, int changeAmount);
        bool IncrementCountWithRateLimit(string userName, string countLabel, TimeSpan rateLimitDuration);
        bool ChangeCountByWithRateLimit(string userName, string countLabel, int changeAmount, TimeSpan rateLimitDuration);
        bool IsCountRateLimited(string userName, string countLabel);
        void SetCharacteristic(string userName, string characteristicLabel, string characteristicValue);
        void AddToList(string userName, string listLabel, string listValue);
        void RemoveFromList(string userName, string listLabel, string listValue);
        void SetTimer(string userName, string timerLabel, CoolDown timer);
        void ChangeCurrency(string userName, string currencyLabel, int changeAmount);
        void ChangeEscrow(string userName, string escrowLabel, int changeAmount);
        void ChangeJobExperience(string userName, string jobLabel, int changeAmount);

        // Profile Query Operations (optimized queries that don't fetch entire profile)
        string GetDisplayName(string userName);
        Dictionary<string, int> GetCurrencies(string userName);
        string GetCharacteristic(string userName, string characteristic);

        // Interaction Operations
        void AddInteraction(Interaction toAdd);
        Interaction GetInteraction(ObjectId interactionId);
        void SetInteraction(Interaction editedInteraction);
        List<Interaction> GetInteractionsByInitiator(string initiator);
        List<Interaction> GetInteractionsByRecipient(string recipient);
        List<Interaction> GetInteractionsByType(string type);
        int CountInteractionsByInitiatorAndType(string initiator, string type);
        int CountInteractionsByRecipientAndType(string recipient, string type);
        int CountInteractionsBetweenUsersAndType(string user1, string user2, string type);
        long GetTypeCount(string profileName, string identifierType, string initiatorRecipientOrBoth);

        // Pending Command Operations
        void AddPendingCommand(PendingCommand toAdd);
        void UpdatePendingCommand(PendingCommand updated);
        PendingCommand GetPendingCommand(ObjectId commandId);
        PendingCommand GetPendingCommandAwaitingConsent(string awaitingConsentFrom);
        List<PendingCommand> GetPendingCommands(string awaitingConsentFrom);
        List<PendingCommand> GetPendingCommandsByGroupId(string groupId);
        List<PendingCommand> GetPendingCommandsByInitiator(string initiator);
        void DeletePendingCommand(ObjectId commandId);

        // Duty Operations
        Duty GetDuty(string dutyLabel);
        void SetDuty(string label, Duty newDuty);
        List<Duty> GetDutiesByJob(string job);
        List<Duty> GetDutiesByCategory(string category);
        void AddPendingDuty(PendingDuty toAdd);
        PendingDuty GetPendingDuty(string awaitingInputFrom);
        void SetPendingDuty(PendingDuty updatedDuty);
        void DeletePendingDuty(ObjectId dutyId);

        // Random Event Operations (read-only in-bot; seeded externally like Duties)
        List<RandomEvent> GetRandomEvents();
        List<RandomEvent> GetRandomEventsByCategory(string category);

        // Command Operations
        Command GetCommand(string commandName);
        List<Command> GetAllCommands();

        // Identifier Operations
        Identifier GetIdentifier(string type);
        List<Identifier> GetIdentifiersByCategory(string category);
        List<Identifier> GetAllIdentifiers();

        // Monster Stats Operations (global aggregate counts for breed/birth)
        MonsterStats GetMonsterStats(string key);
        List<MonsterStats> GetAllMonsterStats();
        void IncrementMonsterStats(string key, int pregnancyDelta, int offspringDelta);

        // Slots jackpot persistence (per "{machine}|{currency}"); -1 when unset.
        int GetSlotsJackpot(string key);
        void SetSlotsJackpot(string key, int amount);

        // Mod Message Operations
        string GetModMessage(string messageLabel);

        // Pledge Operations
        void AddPledge(Pledge pledge);
        Pledge GetPledge(ObjectId pledgeId);
        List<Pledge> GetPledgesByPledger(string pledger);
        List<Pledge> GetPledgesByPledgee(string pledgee);
        List<Pledge> GetActivePledges(string pledger, string pledgee, string interactionType);
        void UpdatePledge(Pledge pledge);
        void DeletePledge(ObjectId pledgeId);

        // Feedback Operations
        void AddFeedback(FeedbackEntry entry);
        List<FeedbackEntry> GetRecentFeedback(int count);

        // Database Management
        void ClearDatabase();
        void ClearCollection(string collectionName);
    }
}