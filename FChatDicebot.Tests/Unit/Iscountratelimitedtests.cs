using FChatDicebot.Database;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for IsCountRateLimited (FIX_SPEC.md Phase 3 / H5). The comparison used to be
    /// inverted (DateTime.UtcNow &gt; timerEnd), meaning it reported "rate limited" only
    /// AFTER the cooldown had already expired — the opposite of every caller's intent.
    /// </summary>
    [Collection("Database")]
    public class IsCountRateLimitedTests : IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly IChateauDatabase _database;

        public IsCountRateLimitedTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reset();
            _database = _fixture.Database;
        }

        public void Dispose()
        {
            _fixture.Reset();
        }

        [Fact]
        public void IsCountRateLimited_TimerActive_ReturnsTrue()
        {
            var profile = new ProfileBuilder()
                .WithTimer("ratelimit_kiss", DateTime.UtcNow.AddMinutes(15))
                .BuildAndSave(_database);

            Assert.True(_database.IsCountRateLimited(profile.userName, "kiss"));
        }

        [Fact]
        public void IsCountRateLimited_TimerExpired_ReturnsFalse()
        {
            var profile = new ProfileBuilder()
                .WithTimer("ratelimit_kiss", DateTime.UtcNow.AddMinutes(-1))
                .BuildAndSave(_database);

            Assert.False(_database.IsCountRateLimited(profile.userName, "kiss"));
        }

        [Fact]
        public void IsCountRateLimited_NoTimer_ReturnsFalse()
        {
            var profile = new ProfileBuilder().BuildAndSave(_database);

            Assert.False(_database.IsCountRateLimited(profile.userName, "kiss"));
        }
    }
}
