using System;

namespace FChatDicebot.Tests
{
    /// <summary>
    /// Configuration for test database connections and settings.
    /// </summary>
    public static class TestConfiguration
    {
        // Database Configuration
        public const string TestConnectionString = "mongodb://localhost:27017";
        public const string TestDatabaseName = "ChateauDb_Test";
        public const string ProductionConnectionString = "mongodb://localhost:27017";
        public const string ProductionDatabaseName = "ChateauDb";

        // Test Data Prefixes (to help identify test data)
        public const string TestUserPrefix = "TestUser_";
        public const string TestChannelPrefix = "test-channel-";

        // Test Settings
        public const int DefaultTestTimeout = 5000; // milliseconds

        /// <summary>
        /// Generates a unique test username to avoid collisions between tests.
        /// </summary>
        public static string GenerateTestUsername(string baseName = "User")
        {
            return $"{TestUserPrefix}{baseName}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        /// <summary>
        /// Generates a unique test channel ID.
        /// </summary>
        public static string GenerateTestChannelId(string baseName = "channel")
        {
            return $"{TestChannelPrefix}{baseName}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}