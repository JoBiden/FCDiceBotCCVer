using FChatDicebot.Model;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace FChatDicebot.Tests.Builders
{
    /// <summary>
    /// Builder for creating Profile test data with a fluent API.
    /// Makes test setup more readable and maintainable.
    /// </summary>
    public class ProfileBuilder
    {
        private Profile _profile;

        public ProfileBuilder()
        {
            // Initialize with sensible defaults
            _profile = new Profile
            {
                Id = ObjectId.GenerateNewId(),
                userName = TestConfiguration.GenerateTestUsername(),
                displayName = "Test User",
                counts = new Dictionary<string, int>(),
                characteristics = new Dictionary<string, string>(),
                lists = new Dictionary<string, List<string>>(),
                timers = new Dictionary<string, CoolDown>(),
                currencies = new Dictionary<string, int>(),
                jobExperience = new Dictionary<string, int>(),
                milkInventory = new List<MilkBottle>(),
                trainings = new Dictionary<string, int>(),
                dailyClimaxCounts = new Dictionary<string, int>()
            };
        }

        public ProfileBuilder WithUserName(string userName)
        {
            _profile.userName = userName;
            return this;
        }

        public ProfileBuilder WithDisplayName(string displayName)
        {
            _profile.displayName = displayName;
            return this;
        }

        public ProfileBuilder WithCount(string countLabel, int value)
        {
            _profile.counts[countLabel] = value;
            return this;
        }

        public ProfileBuilder WithCharacteristic(string label, string value)
        {
            _profile.characteristics[label] = value;
            return this;
        }

        public ProfileBuilder WithListItem(string listLabel, string value)
        {
            if (!_profile.lists.ContainsKey(listLabel))
            {
                _profile.lists[listLabel] = new List<string>();
            }
            _profile.lists[listLabel].Add(value);
            return this;
        }

        public ProfileBuilder WithTimer(string timerLabel, DateTime timerEnd)
        {
            _profile.timers[timerLabel] = new CoolDown
            {
                timerStart = DateTime.UtcNow,
                timerEnd = timerEnd
            };
            return this;
        }

        public ProfileBuilder WithCurrency(string currencyLabel, int amount)
        {
            _profile.currencies[currencyLabel] = amount;
            return this;
        }

        public ProfileBuilder WithJobExperience(string jobLabel, int experience)
        {
            _profile.jobExperience[jobLabel] = experience;
            return this;
        }

        public ProfileBuilder WithMilkBottle(MilkBottle bottle)
        {
            _profile.milkInventory.Add(bottle);
            return this;
        }

        public ProfileBuilder WithTrainingLevel(string trainingId, int level)
        {
            _profile.trainings[trainingId] = level;
            return this;
        }

        public ProfileBuilder WithDailyClimaxCount(string utcDayKey, int count)
        {
            _profile.dailyClimaxCounts[utcDayKey] = count;
            return this;
        }

        /// <summary>
        /// Record a MANOR kickback this profile has earned (as an employer) from a given
        /// employee in a given currency, mirroring how !work increments employeeEarnings.
        /// </summary>
        public ProfileBuilder WithEmployeeEarnings(string employeeUserName, string currency, int amount)
        {
            if (_profile.employeeEarnings == null)
            {
                _profile.employeeEarnings = new Dictionary<string, Dictionary<string, int>>();
            }
            if (!_profile.employeeEarnings.ContainsKey(employeeUserName))
            {
                _profile.employeeEarnings[employeeUserName] = new Dictionary<string, int>();
            }
            _profile.employeeEarnings[employeeUserName][currency] = amount;
            return this;
        }

        /// <summary>
        /// Returns the built profile without saving to database.
        /// Useful for testing validation logic.
        /// </summary>
        public Profile Build()
        {
            return _profile;
        }

        /// <summary>
        /// Builds and saves the profile to the test database.
        /// Returns the saved profile with correct _id from database.
        /// </summary>
        public Profile BuildAndSave(Database.IChateauDatabase database)
        {
            // First register the user (creates the profile in DB)
            database.RegisterUserChateau(_profile.userName);

            // Get the profile that was just created (has correct _id from DB)
            Profile registeredProfile = database.GetProfile(_profile.userName);

            // Copy our configured values to the registered profile
            registeredProfile.displayName = _profile.displayName;
            registeredProfile.counts = _profile.counts;
            registeredProfile.characteristics = _profile.characteristics;
            registeredProfile.lists = _profile.lists;
            registeredProfile.timers = _profile.timers;
            registeredProfile.currencies = _profile.currencies;
            registeredProfile.jobExperience = _profile.jobExperience;
            registeredProfile.milkInventory = _profile.milkInventory;
            registeredProfile.trainings = _profile.trainings;
            registeredProfile.dailyClimaxCounts = _profile.dailyClimaxCounts;
            registeredProfile.employeeEarnings = _profile.employeeEarnings;

            // Now save with the correct _id
            database.SetProfile(_profile.userName, registeredProfile);

            return registeredProfile;
        }
    }
}