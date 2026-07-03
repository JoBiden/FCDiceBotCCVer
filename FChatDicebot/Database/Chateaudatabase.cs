using FChatDicebot.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.Database
{
    /// <summary>
    /// Production implementation of IChateauDatabase.
    /// Handles all MongoDB operations for the Chateau system.
    /// </summary>
    public class ChateauDatabase : IChateauDatabase
    {
        private readonly IMongoClient _client;
        private readonly string _databaseName;
        private IMongoDatabase Database => _client.GetDatabase(_databaseName);

        public ChateauDatabase(string connectionString, string databaseName)
        {
            _client = new MongoClient(connectionString);
            _databaseName = databaseName;
        }

        // Profile Operations
        public string RegisterUserChateau(string userName)
        {
            var collection = Database.GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var profile = collection.Find(filter).FirstOrDefault();

            if (profile == null)
            {
                var toRegister = new Profile
                {
                    userName = userName,
                    displayName = userName
                };
                collection.InsertOne(toRegister.ToBsonDocument());

                return "Welcome to the Chateau, " + toRegister.displayName + "! We hope you enjoy your time here. Use !help for a list of commands, and feel free to ask around. I promise, we only bite if you want us to~";
            }
            else
            {
                return "You're already signed up, " + profile.GetValue("displayName") + ".";
            }
        }

        public Profile GetProfile(string userName)
        {
            var collection = Database.GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                Profile profile = BsonSerializer.Deserialize<Profile>(document);
                return profile;
            }
            return null;
        }

        public List<Profile> GetAllProfiles()
        {
            var collection = Database.GetCollection<BsonDocument>("RegisteredProfiles");
            var documents = collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();
            return documents.Select(doc => BsonSerializer.Deserialize<Profile>(doc)).ToList();
        }

        // Whole-profile ReplaceOne. This reverts any concurrent atomic $inc (ChangeCurrency,
        // ChangeEscrow, TryDebitCurrency, ChangeCount) that lands between this call's read and
        // write. Never use this to persist currency/escrow/count/timer changes — use the
        // targeted atomic helpers for those instead.
        public void SetProfile(string userName, Profile newProfile)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            collection.ReplaceOne(filter, newProfile);
        }

        public void IncrementCount(string userName, string countLabel)
        {
            ChangeCount(userName, countLabel, 1);
        }

        public void DecrementCount(string userName, string countLabel)
        {
            ChangeCount(userName, countLabel, -1);
        }

        public void ChangeCount(string userName, string countLabel, int changeAmount)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null) return;

            if (document.counts.ContainsKey(countLabel))
            {
                document.counts[countLabel] += changeAmount;
            }
            else
            {
                document.counts.Add(countLabel, changeAmount);
            }
            collection.ReplaceOne(filter, document);
        }

        public bool IncrementCountWithRateLimit(string userName, string countLabel, TimeSpan rateLimitDuration)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
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
            CoolDown rateLimitTimer = new CoolDown();
            rateLimitTimer.timerEnd = DateTime.UtcNow.Add(rateLimitDuration);
            if (profile.timers == null)
            {
                profile.timers = new Dictionary<string, CoolDown>();
            }
            profile.timers[timerKey] = rateLimitTimer;

            // Save profile
            collection.ReplaceOne(filter, profile);

            return true;
        }

        public bool ChangeCountByWithRateLimit(string userName, string countLabel, int changeAmount, TimeSpan rateLimitDuration)
        {
            // +N variant of IncrementCountWithRateLimit used by group resolution: one
            // rate-limit check per (participant, key); if not limited apply the full
            // changeAmount and arm the timer, otherwise apply nothing and report false so
            // the caller can surface the "clerks were busy" sub-note for that participant.
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var profile = collection.Find(filter).FirstOrDefault();

            if (profile == null) return false;

            string timerKey = $"ratelimit_{countLabel}";

            if (profile.timers != null && profile.timers.ContainsKey(timerKey))
            {
                if (DateTime.UtcNow < profile.timers[timerKey].timerEnd)
                {
                    // Still on cooldown, don't apply anything.
                    return false;
                }
            }

            if (profile.counts.ContainsKey(countLabel))
            {
                profile.counts[countLabel] += changeAmount;
            }
            else
            {
                profile.counts.Add(countLabel, changeAmount);
            }

            CoolDown rateLimitTimer = new CoolDown();
            rateLimitTimer.timerEnd = DateTime.UtcNow.Add(rateLimitDuration);
            if (profile.timers == null)
            {
                profile.timers = new Dictionary<string, CoolDown>();
            }
            profile.timers[timerKey] = rateLimitTimer;

            collection.ReplaceOne(filter, profile);

            return true;
        }

        public bool IsCountRateLimited(string userName, string countLabel)
        {
            Profile profile = GetProfile(userName);
            if (profile == null) return false;

            string timerKey = $"ratelimit_{countLabel}";

            if (profile.timers != null && profile.timers.ContainsKey(timerKey))
            {
                // Rate-limited means the timer hasn't expired yet (now is before timerEnd).
                return DateTime.UtcNow < profile.timers[timerKey].timerEnd;
            }

            return false; // No timer exists, so not rate limited
        }

        public void SetCharacteristic(string userName, string characteristicLabel, string characteristicValue)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null) return;

            if (document.characteristics.ContainsKey(characteristicLabel))
            {
                document.characteristics[characteristicLabel] = characteristicValue;
            }
            else
            {
                document.characteristics.Add(characteristicLabel, characteristicValue);
            }
            collection.ReplaceOne(filter, document);
        }

        public void AddToList(string userName, string listLabel, string listValue)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null) return;

            if (document.lists.ContainsKey(listLabel))
            {
                if (!document.lists[listLabel].Contains(listValue))
                {
                    document.lists[listLabel].Add(listValue);
                }
            }
            else
            {
                document.lists.Add(listLabel, new List<string> { listValue });
            }
            collection.ReplaceOne(filter, document);
        }

        public void RemoveFromList(string userName, string listLabel, string listValue)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null) return;

            if (document.lists.ContainsKey(listLabel))
            {
                document.lists[listLabel].Remove(listValue);
            }
            collection.ReplaceOne(filter, document);
        }

        public void SetTimer(string userName, string timerLabel, CoolDown timer)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null) return;

            if (document.timers.ContainsKey(timerLabel))
            {
                document.timers[timerLabel] = timer;
            }
            else
            {
                document.timers.Add(timerLabel, timer);
            }
            collection.ReplaceOne(filter, document);
        }

        public void SetMilkInventory(string userName, List<MilkBottle> inventory)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null) return;

            document.milkInventory = inventory;
            collection.ReplaceOne(filter, document);
        }

        public void ChangeCurrency(string userName, string currencyLabel, int changeAmount)
        {
            // Server-side atomic $inc (not a read-modify-write ReplaceOne). This matters for the
            // wager economy: a player can be in two games at once, and a payout can race a bet on
            // the same profile. $inc upserts the currency key if missing, so it preserves the old
            // "create on first credit" behavior. The userName filter means an unknown profile is a
            // no-op, exactly as the previous `document == null` guard.
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var update = Builders<Profile>.Update.Inc("currencies." + currencyLabel, changeAmount);
            collection.UpdateOne(filter, update);
        }

        public bool TryDebitCurrency(string userName, string currencyLabel, int amount, bool allowNegative)
        {
            if (amount <= 0) return false; // callers pass a positive magnitude
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var fb = Builders<Profile>.Filter;
            var filter = fb.Eq("userName", userName);
            if (!allowNegative)
            {
                // $gte doesn't match a missing field, so a player with no balance at all
                // correctly fails the guarded debit with no special-casing needed.
                filter = fb.And(filter, fb.Gte("currencies." + currencyLabel, amount));
            }
            var update = Builders<Profile>.Update.Inc("currencies." + currencyLabel, -amount);
            return collection.UpdateOne(filter, update).ModifiedCount == 1;
        }

        public void ChangeEscrow(string userName, string escrowLabel, int changeAmount)
        {
            // Atomic $inc on the wager-escrow sub-field, same contract as ChangeCurrency. The
            // escrowLabel is expected to already be Mongo-safe (no '.'/'$'); see WagerEscrow.Key.
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var update = Builders<Profile>.Update.Inc("escrow." + escrowLabel, changeAmount);
            collection.UpdateOne(filter, update);
        }

        public void ChangeJobExperience(string userName, string jobLabel, int changeAmount)
        {
            var collection = Database.GetCollection<Profile>("RegisteredProfiles");
            var filter = Builders<Profile>.Filter.Eq("userName", userName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null) return;

            if (document.jobExperience.ContainsKey(jobLabel))
            {
                document.jobExperience[jobLabel] += changeAmount;
            }
            else
            {
                document.jobExperience.Add(jobLabel, changeAmount);
            }
            collection.ReplaceOne(filter, document);
        }

        // Interaction Operations
        public void AddInteraction(Interaction toAdd)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            collection.InsertOne(toAdd.ToBsonDocument());
        }

        public Interaction GetInteraction(ObjectId interactionId)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", interactionId);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                return BsonSerializer.Deserialize<Interaction>(document);
            }
            return null;
        }

        public void SetInteraction(Interaction editedInteraction)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", editedInteraction.Id);
            collection.ReplaceOne(filter, editedInteraction.ToBsonDocument());
        }

        public List<Interaction> GetInteractionsByInitiator(string initiator)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("initiator", initiator);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Interaction>(doc)).ToList();
        }

        public List<Interaction> GetInteractionsByRecipient(string recipient)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("recipient", recipient);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Interaction>(doc)).ToList();
        }

        public List<Interaction> GetInteractionsByType(string type)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.Eq("type", type);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Interaction>(doc)).ToList();
        }

        public int CountInteractionsByInitiatorAndType(string initiator, string type)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("initiator", initiator),
                Builders<BsonDocument>.Filter.Eq("type", type)
            );
            return (int)collection.CountDocuments(filter);
        }

        public int CountInteractionsByRecipientAndType(string recipient, string type)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("recipient", recipient),
                Builders<BsonDocument>.Filter.Eq("type", type)
            );
            return (int)collection.CountDocuments(filter);
        }

        public int CountInteractionsBetweenUsersAndType(string user1, string user2, string type)
        {
            var collection = Database.GetCollection<BsonDocument>("Interactions");
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("type", type),
                Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("initiator", user1),
                        Builders<BsonDocument>.Filter.Eq("recipient", user2)
                    ),
                    Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("initiator", user2),
                        Builders<BsonDocument>.Filter.Eq("recipient", user1)
                    )
                )
            );
            return (int)collection.CountDocuments(filter);
        }

        public long GetTypeCount(string profileName, string identifierType, string initiatorRecipientOrBoth)
        {
            // Interaction.type is stored with its canonical case (e.g. "paymentGive",
            // "paymentReceive") and Mongo string equality is case-sensitive, so lowercasing
            // it here meant this query could never match those camelCase types — silently
            // zeroing out any specialist count derived from them (M4).
            initiatorRecipientOrBoth = initiatorRecipientOrBoth.ToLower();

            switch (initiatorRecipientOrBoth)
            {
                case "initiator":
                    return CountInteractionsByInitiatorAndType(profileName, identifierType);
                case "recipient":
                    return CountInteractionsByRecipientAndType(profileName, identifierType);
                default:
                    // For "both", we need to sum initiator and recipient counts
                    int asInitiator = CountInteractionsByInitiatorAndType(profileName, identifierType);
                    int asRecipient = CountInteractionsByRecipientAndType(profileName, identifierType);
                    return asInitiator + asRecipient;
            }
        }

        // Pending Command Operations
        public void AddPendingCommand(PendingCommand toAdd)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingCommands");
            collection.InsertOne(toAdd.ToBsonDocument());
        }

        public void UpdatePendingCommand(PendingCommand updated)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", updated.Id);
            collection.ReplaceOne(filter, updated.ToBsonDocument());
        }

        public PendingCommand GetPendingCommand(ObjectId commandId)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", commandId);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                return BsonSerializer.Deserialize<PendingCommand>(document);
            }
            return null;
        }

        public List<PendingCommand> GetPendingCommandsByGroupId(string groupId)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("groupId", groupId);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<PendingCommand>(doc)).ToList();
        }

        public List<PendingCommand> GetPendingCommandsByInitiator(string initiator)
        {
            // The initiator lives on the nested interaction document, so the filter has to
            // reach through pendingInteraction.initiator. Used by !withdraw and by group
            // resolution to recover every seat the initiator has out.
            var collection = Database.GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("pendingInteraction.initiator", initiator);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<PendingCommand>(doc)).ToList();
        }

        public PendingCommand GetPendingCommandAwaitingConsent(string awaitingConsentFrom)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("awaitingConsentFrom", awaitingConsentFrom);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                return BsonSerializer.Deserialize<PendingCommand>(document);
            }
            return null;
        }

        public List<PendingCommand> GetPendingCommands(string awaitingConsentFrom)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("awaitingConsentFrom", awaitingConsentFrom);
            var documents = collection.Find(filter).ToList();

            List<PendingCommand> pendingInteractions = new List<PendingCommand>();
            foreach (var pending in documents)
            {
                pendingInteractions.Add(BsonSerializer.Deserialize<PendingCommand>(pending));
            }
            return pendingInteractions;
        }

        public void DeletePendingCommand(ObjectId commandId)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingCommands");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", commandId);
            collection.DeleteOne(filter);
        }

        // Duty Operations
        public Duty GetDuty(string dutyLabel)
        {
            var collection = Database.GetCollection<BsonDocument>("Duties");
            var filter = Builders<BsonDocument>.Filter.Eq("label", dutyLabel);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                return BsonSerializer.Deserialize<Duty>(document);
            }
            return null;
        }

        public void SetDuty(string label, Duty newDuty)
        {
            var collection = Database.GetCollection<BsonDocument>("Duties");
            var filter = Builders<BsonDocument>.Filter.Eq("label", label);
            collection.InsertOne(newDuty.ToBsonDocument());
        }

        public List<Duty> GetDutiesByJob(string job)
        {
            var collection = Database.GetCollection<BsonDocument>("Duties");
            var filter = Builders<BsonDocument>.Filter.Eq("job", job);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Duty>(doc)).ToList();
        }

        public List<Duty> GetDutiesByCategory(string category)
        {
            var collection = Database.GetCollection<BsonDocument>("Duties");
            var filter = Builders<BsonDocument>.Filter.AnyEq("categories", category);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Duty>(doc)).ToList();
        }

        public void AddPendingDuty(PendingDuty toAdd)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingDuties");
            collection.InsertOne(toAdd.ToBsonDocument());
        }

        public PendingDuty GetPendingDuty(string awaitingInputFrom)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingDuties");
            var filter = Builders<BsonDocument>.Filter.Eq("awaitingInputFrom", awaitingInputFrom);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                return BsonSerializer.Deserialize<PendingDuty>(document);
            }
            return null;
        }

        public void SetPendingDuty(PendingDuty updatedDuty)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingDuties");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", updatedDuty.Id);
            collection.ReplaceOne(filter, updatedDuty.ToBsonDocument());
        }

        public void DeletePendingDuty(ObjectId dutyId)
        {
            var collection = Database.GetCollection<BsonDocument>("PendingDuties");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", dutyId);
            collection.DeleteOne(filter);
        }

        // Random Event Operations (read-only in-bot; seeded externally like Duties)
        public List<RandomEvent> GetRandomEvents()
        {
            var collection = Database.GetCollection<BsonDocument>("RandomEvents");
            var documents = collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<RandomEvent>(doc)).ToList();
        }

        public List<RandomEvent> GetRandomEventsByCategory(string category)
        {
            var collection = Database.GetCollection<BsonDocument>("RandomEvents");
            var filter = Builders<BsonDocument>.Filter.AnyEq("categories", category);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<RandomEvent>(doc)).ToList();
        }

        // Command Operations
        public Command GetCommand(string commandName)
        {
            var collection = Database.GetCollection<BsonDocument>("Commands");
            var filter = Builders<BsonDocument>.Filter.Eq("commandName", commandName);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                return BsonSerializer.Deserialize<Command>(document);
            }
            return null;
        }

        public List<Command> GetAllCommands()
        {
            var collection = Database.GetCollection<BsonDocument>("Commands");
            var documents = collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Command>(doc)).ToList();
        }

        // Identifier Operations
        public Identifier GetIdentifier(string type)
        {
            var collection = Database.GetCollection<BsonDocument>("Identifiers");
            var filter = Builders<BsonDocument>.Filter.Eq("type", type);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                return BsonSerializer.Deserialize<Identifier>(document);
            }
            return null;
        }

        public List<Identifier> GetIdentifiersByCategory(string category)
        {
            var collection = Database.GetCollection<BsonDocument>("Identifiers");
            var filter = Builders<BsonDocument>.Filter.AnyEq("categories", category);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Identifier>(doc)).ToList();
        }

        public List<Identifier> GetAllIdentifiers()
        {
            var collection = Database.GetCollection<BsonDocument>("Identifiers");
            var documents = collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Identifier>(doc)).ToList();
        }

        // Profile Query Operations (optimized - don't fetch entire profile)
        public string GetDisplayName(string userName)
        {
            var collection = Database.GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var projection = Builders<BsonDocument>.Projection.Include("displayName").Include("userName");
            var document = collection.Find(filter).Project(projection).FirstOrDefault();

            if (document != null)
            {
                return document.GetValue("displayName").AsString;
            }
            return null;
        }

        public Dictionary<string, int> GetCurrencies(string userName)
        {
            var collection = Database.GetCollection<BsonDocument>("RegisteredProfiles");
            var filter = Builders<BsonDocument>.Filter.Eq("userName", userName);
            var projection = Builders<BsonDocument>.Projection.Include("currencies");
            var document = collection.Find(filter).Project(projection).FirstOrDefault();

            if (document != null && document.Contains("currencies"))
            {
                return BsonSerializer.Deserialize<Dictionary<string, int>>(document.GetValue("currencies").AsBsonDocument);
            }
            return new Dictionary<string, int>();
        }

        public string GetCharacteristic(string userName, string characteristic)
        {
            var collection = Database.GetCollection<BsonDocument>("RegisteredProfiles");
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

        // Monster Stats Operations (global aggregate counts for breed/birth)
        public MonsterStats GetMonsterStats(string key)
        {
            var collection = Database.GetCollection<MonsterStats>("MonsterStats");
            var filter = Builders<MonsterStats>.Filter.Eq(s => s.Id, key);
            return collection.Find(filter).FirstOrDefault();
        }

        public List<MonsterStats> GetAllMonsterStats()
        {
            var collection = Database.GetCollection<MonsterStats>("MonsterStats");
            return collection.Find(Builders<MonsterStats>.Filter.Empty).ToList();
        }

        public void IncrementMonsterStats(string key, int pregnancyDelta, int offspringDelta)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (pregnancyDelta == 0 && offspringDelta == 0) return;

            var collection = Database.GetCollection<MonsterStats>("MonsterStats");
            var filter = Builders<MonsterStats>.Filter.Eq(s => s.Id, key);
            var update = Builders<MonsterStats>.Update
                .Inc(s => s.PregnancyCount, pregnancyDelta)
                .Inc(s => s.OffspringCount, offspringDelta);
            var options = new UpdateOptions { IsUpsert = true };
            collection.UpdateOne(filter, update, options);
        }

        public int GetSlotsJackpot(string key)
        {
            if (string.IsNullOrEmpty(key)) return -1;
            var collection = Database.GetCollection<SlotsJackpot>("SlotsJackpots");
            var filter = Builders<SlotsJackpot>.Filter.Eq(s => s.Id, key);
            var doc = collection.Find(filter).FirstOrDefault();
            return doc == null ? -1 : doc.Amount;
        }

        public void SetSlotsJackpot(string key, int amount)
        {
            if (string.IsNullOrEmpty(key)) return;
            var collection = Database.GetCollection<SlotsJackpot>("SlotsJackpots");
            var filter = Builders<SlotsJackpot>.Filter.Eq(s => s.Id, key);
            var update = Builders<SlotsJackpot>.Update.Set(s => s.Amount, amount);
            collection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
        }

        // Mod Message Operations
        public string GetModMessage(string messageLabel)
        {
            messageLabel = messageLabel.ToLower();
            var collection = Database.GetCollection<BsonDocument>("ModMessages");

            if (messageLabel == "all")
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

            return (string)message.GetElement("text").Value;
        }

        // Pledge Operations
        public void AddPledge(Pledge pledge)
        {
            var collection = Database.GetCollection<BsonDocument>("Pledges");
            collection.InsertOne(pledge.ToBsonDocument());
        }

        public Pledge GetPledge(ObjectId pledgeId)
        {
            var collection = Database.GetCollection<BsonDocument>("Pledges");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", pledgeId);
            var document = collection.Find(filter).FirstOrDefault();

            if (document != null)
            {
                return BsonSerializer.Deserialize<Pledge>(document);
            }
            return null;
        }

        public List<Pledge> GetPledgesByPledger(string pledger)
        {
            var collection = Database.GetCollection<BsonDocument>("Pledges");
            var filter = Builders<BsonDocument>.Filter.Eq("pledger", pledger);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Pledge>(doc)).ToList();
        }

        public List<Pledge> GetPledgesByPledgee(string pledgee)
        {
            var collection = Database.GetCollection<BsonDocument>("Pledges");
            var filter = Builders<BsonDocument>.Filter.Eq("pledgee", pledgee);
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Pledge>(doc)).ToList();
        }

        public List<Pledge> GetActivePledges(string pledger, string pledgee, string interactionType)
        {
            var collection = Database.GetCollection<BsonDocument>("Pledges");
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("pledger", pledger),
                Builders<BsonDocument>.Filter.Eq("pledgee", pledgee),
                Builders<BsonDocument>.Filter.Eq("interactionType", interactionType),
                Builders<BsonDocument>.Filter.Eq("status", "active")
            );
            var documents = collection.Find(filter).ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<Pledge>(doc)).ToList();
        }

        public void UpdatePledge(Pledge pledge)
        {
            var collection = Database.GetCollection<Pledge>("Pledges");
            var filter = Builders<Pledge>.Filter.Eq("_id", pledge.Id);
            collection.ReplaceOne(filter, pledge);
        }

        public void DeletePledge(ObjectId pledgeId)
        {
            var collection = Database.GetCollection<Pledge>("Pledges");
            var filter = Builders<Pledge>.Filter.Eq("_id", pledgeId);
            collection.DeleteOne(filter);
        }

        // Feedback Operations
        public void AddFeedback(FeedbackEntry entry)
        {
            var collection = Database.GetCollection<BsonDocument>("Feedback");
            collection.InsertOne(entry.ToBsonDocument());
        }

        public List<FeedbackEntry> GetRecentFeedback(int count)
        {
            var collection = Database.GetCollection<BsonDocument>("Feedback");
            var documents = collection.Find(Builders<BsonDocument>.Filter.Empty)
                .Sort(Builders<BsonDocument>.Sort.Descending("submittedAt"))
                .Limit(count)
                .ToList();

            return documents.Select(doc => BsonSerializer.Deserialize<FeedbackEntry>(doc)).ToList();
        }

        // Database Management
        public void ClearDatabase()
        {
            _client.DropDatabase(_databaseName);
        }

        public void ClearCollection(string collectionName)
        {
            Database.DropCollection(collectionName);
        }
    }
}