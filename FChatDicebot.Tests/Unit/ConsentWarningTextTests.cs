using FChatDicebot.InteractionProcessors;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Unit coverage for the shared consent-warning composer and the CooldownSpec help
    /// formatter — the single source of truth behind every "[b]This should not be taken
    /// lightly...[/b]" block and the derived !help cooldown strings.
    /// </summary>
    public class ConsentWarningTextTests
    {
        [Theory]
        [InlineData(1, "day")]
        [InlineData(7, "week")]
        public void PeriodWord_RendersDayAndWeek(int days, string expected)
        {
            Assert.Equal(expected, ConsentWarningText.PeriodWord(days));
        }

        [Fact]
        public void Block_WrapsOpenerAndClauses_SkippingEmpties()
        {
            string block = ConsentWarningText.Block("First clause.", "", null, "Second clause.");

            Assert.Equal("[b]This should not be taken lightly. First clause. Second clause.[/b]", block);
        }

        [Fact]
        public void Block_WithNoClauses_IsJustTheOpener()
        {
            Assert.Equal("[b]This should not be taken lightly.[/b]", ConsentWarningText.Block());
        }

        // Shape A — both parties.
        [Fact]
        public void FrequencyBoth_RendersBetweenTheTwoOfYou()
        {
            Assert.Equal(
                "You can only declare a bond between the two of you once per day.",
                ConsentWarningText.FrequencyBoth("declare a bond", 1));
        }

        // Shape B — recipient, globally.
        [Theory]
        [InlineData("employed", 1, "You can only be employed once per day.")]
        [InlineData("monsterized", 7, "You can only be monsterized once per week.")]
        public void FrequencyRecipient_RendersRecipientFramedParticiple(string participle, int days, string expected)
        {
            Assert.Equal(expected, ConsentWarningText.FrequencyRecipient(participle, days));
        }

        // Shape C — per-axis on the initiator, named.
        [Fact]
        public void FrequencyPerAxis_NamesInitiatorAndAxis()
        {
            Assert.Equal(
                "Alice can only dose you with a given vice once per week.",
                ConsentWarningText.FrequencyPerAxis("Alice", "dose you with a given vice", 7));
        }

        // Shape D — per-day magnitude quota on the initiator, named.
        [Fact]
        public void FrequencyQuota_NamesInitiatorAndMagnitude()
        {
            Assert.Equal(
                "Alice can only corrupt you by 10 per day.",
                ConsentWarningText.FrequencyQuota("Alice", "corrupt", 10, 1));
        }

        [Fact]
        public void ConsumedClause_ShowsSpentMagnitudeWhenPositive()
        {
            Assert.Equal(
                "Alice has already corrupted you by 2 today.",
                ConsentWarningText.ConsumedClause("Alice", "corrupted", 2));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ConsumedClause_SuppressedWhenNothingSpent(int used)
        {
            Assert.Equal("", ConsentWarningText.ConsumedClause("Alice", "corrupted", used));
        }

        // -------------------------------------------------------------------
        // CooldownSpec — derived help / !whatis strings
        // -------------------------------------------------------------------

        [Fact]
        public void FormatDuration_OneDayCooldown()
        {
            var spec = new CooldownSpec { Kind = CooldownKind.Cooldown, Binds = CooldownBinds.Recipient, PeriodDays = 1 };
            Assert.Equal("1 Day", spec.FormatDuration());
        }

        [Fact]
        public void FormatDuration_SevenDayCooldown()
        {
            var spec = new CooldownSpec { Kind = CooldownKind.Cooldown, Binds = CooldownBinds.Initiator, PeriodDays = 7, Scope = "vice" };
            Assert.Equal("7 Days", spec.FormatDuration());
        }

        [Fact]
        public void FormatDuration_MagnitudeQuota()
        {
            var spec = new CooldownSpec { Kind = CooldownKind.MagnitudeQuota, Binds = CooldownBinds.Initiator, PeriodDays = 1, QuotaMagnitude = 10 };
            Assert.Equal("Daily quota (10 magnitude per recipient)", spec.FormatDuration());
        }

        [Theory]
        [InlineData(CooldownBinds.Both, null, "both initiator and recipient")]
        [InlineData(CooldownBinds.Recipient, null, "recipient")]
        [InlineData(CooldownBinds.Initiator, "vice", "initiator (per vice per recipient)")]
        [InlineData(CooldownBinds.Initiator, null, "initiator")]
        [InlineData(CooldownBinds.Pair, null, "both initiator and recipient (per pair)")]
        public void FormatAppliesTo_Cooldown(CooldownBinds binds, string scope, string expected)
        {
            var spec = new CooldownSpec { Kind = CooldownKind.Cooldown, Binds = binds, PeriodDays = 1, Scope = scope };
            Assert.Equal(expected, spec.FormatAppliesTo());
        }

        [Fact]
        public void FormatAppliesTo_MagnitudeQuota_IsInitiator()
        {
            var spec = new CooldownSpec { Kind = CooldownKind.MagnitudeQuota, Binds = CooldownBinds.Initiator, QuotaMagnitude = 10 };
            Assert.Equal("initiator", spec.FormatAppliesTo());
        }
    }
}
