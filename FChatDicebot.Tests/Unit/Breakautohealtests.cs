using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for the lazy daily auto-heal tick on <see cref="BreakInstance.LoadAllWithTick"/>.
    /// Pure logic — no database fixture required.
    /// </summary>
    public class BreakAutoHealTests
    {
        private static Profile MakeProfileWithBreak(string part, int severity, DateTime lastTickedAt)
        {
            var profile = new ProfileBuilder().WithUserName("Bob").Build();
            BreakInstance.SaveAll(profile, new List<BreakInstance>
            {
                new BreakInstance
                {
                    Part = part,
                    Severity = severity,
                    BrokenBy = "Alice",
                    BrokenAt = lastTickedAt,
                    LastTickedAt = lastTickedAt,
                }
            });
            return profile;
        }

        [Fact]
        public void LoadAllWithTick_SameDay_NoDecrement()
        {
            var profile = MakeProfileWithBreak("mouth", severity: 3, lastTickedAt: DateTime.UtcNow.Date);
            var entries = BreakInstance.LoadAllWithTick(profile);
            Assert.Single(entries);
            Assert.Equal(3, entries[0].Severity);
        }

        [Fact]
        public void LoadAllWithTick_OneDayElapsed_DecrementsByOne()
        {
            var profile = MakeProfileWithBreak("mouth", severity: 3, lastTickedAt: DateTime.UtcNow.Date.AddDays(-1));
            var entries = BreakInstance.LoadAllWithTick(profile);
            Assert.Single(entries);
            Assert.Equal(2, entries[0].Severity);
            Assert.Equal(DateTime.UtcNow.Date, entries[0].LastTickedAt);
        }

        [Fact]
        public void LoadAllWithTick_MultiDayGap_CollapsesIntoOneTick()
        {
            var profile = MakeProfileWithBreak("mouth", severity: 10, lastTickedAt: DateTime.UtcNow.Date.AddDays(-3));
            var entries = BreakInstance.LoadAllWithTick(profile);
            Assert.Single(entries);
            Assert.Equal(7, entries[0].Severity);
        }

        [Fact]
        public void LoadAllWithTick_SeverityHitsZero_RemovesEntry()
        {
            var profile = MakeProfileWithBreak("mouth", severity: 1, lastTickedAt: DateTime.UtcNow.Date.AddDays(-1));
            var entries = BreakInstance.LoadAllWithTick(profile);
            Assert.Empty(entries);
            // List key cleared entirely from the profile when the last entry exhausts.
            Assert.False(profile.lists.ContainsKey(BreakInstance.BreaksListKey));
        }

        [Fact]
        public void LoadAllWithTick_OvershootGap_RemovesEntry()
        {
            var profile = MakeProfileWithBreak("mouth", severity: 2, lastTickedAt: DateTime.UtcNow.Date.AddDays(-5));
            var entries = BreakInstance.LoadAllWithTick(profile);
            Assert.Empty(entries);
        }

        [Fact]
        public void LoadAllWithTick_PersistsTickToProfile()
        {
            var profile = MakeProfileWithBreak("mouth", severity: 3, lastTickedAt: DateTime.UtcNow.Date.AddDays(-1));
            BreakInstance.LoadAllWithTick(profile);
            // Subsequent load (without re-ticking) should reflect the decremented severity.
            var followup = BreakInstance.LoadAll(profile);
            Assert.Single(followup);
            Assert.Equal(2, followup[0].Severity);
        }

        [Fact]
        public void LoadAllWithTick_NoBreaks_ReturnsEmpty()
        {
            var profile = new ProfileBuilder().WithUserName("Bob").Build();
            var entries = BreakInstance.LoadAllWithTick(profile);
            Assert.Empty(entries);
        }
    }
}
