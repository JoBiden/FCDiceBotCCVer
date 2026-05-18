using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.StatusEffectContributors;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Unit tests for <see cref="CorruptionStatusContributor"/>. The contributor is a pure
    /// read of <c>profile.characteristics["corruption"]</c>; it doesn't need a database
    /// fixture because there's no state mutation to persist.
    /// </summary>
    public class CorruptionStatusContributorTests
    {
        private readonly CorruptionStatusContributor _contributor = new CorruptionStatusContributor();

        [Fact]
        public void NullProfile_ReturnsEmpty()
        {
            var result = _contributor.Contribute(null, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
            Assert.Empty(result.ConsentWarnings);
            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void ConsentCallSite_ReturnsEmpty()
        {
            // Per spec, the corruption descriptor is a completion-time flavor only.
            var bob = ProfileWithCorruption(-50);
            var result = _contributor.Contribute(bob, StatusEffectCallSite.Consent, "kiss", isInitiator: false);
            Assert.Empty(result.CompletionAppendix);
            Assert.Empty(result.ConsentWarnings);
        }

        [Fact]
        public void NeutralBand_ReturnsNoFragment()
        {
            foreach (int corruption in new[] { -9, -1, 0, 1, 9 })
            {
                var profile = ProfileWithCorruption(corruption);
                var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
                Assert.Empty(result.CompletionAppendix);
            }
        }

        [Theory]
        // Corruption side
        [InlineData(-100, "An absolute aura of corruption radiates from Bob.")]
        [InlineData(-200, "An absolute aura of corruption radiates from Bob.")]
        [InlineData(-99,  "A strong aura of corruption emanates from Bob.")]
        [InlineData(-50,  "A strong aura of corruption emanates from Bob.")]
        [InlineData(-49,  "A faint aura of corruption emanates from Bob.")]
        [InlineData(-10,  "A faint aura of corruption emanates from Bob.")]
        // Purity side
        [InlineData(10,   "A faint aura of purity emanates from Bob.")]
        [InlineData(49,   "A faint aura of purity emanates from Bob.")]
        [InlineData(50,   "A strong aura of purity emanates from Bob.")]
        [InlineData(99,   "A strong aura of purity emanates from Bob.")]
        [InlineData(100,  "An absolute aura of purity radiates from Bob.")]
        [InlineData(500,  "An absolute aura of purity radiates from Bob.")]
        public void Bands_MapToCorrectFragment(int corruption, string expectedFragment)
        {
            Assert.Equal(expectedFragment, CorruptionStatusContributor.DescribeBand(corruption, "Bob"));

            var profile = ProfileWithCorruption(corruption);
            profile.displayName = "Bob";
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
            Assert.Single(result.CompletionAppendix);
            Assert.Equal(expectedFragment, result.CompletionAppendix[0]);
        }

        [Fact]
        public void CorruptInteractionType_SkipsSelfReference()
        {
            // The !corrupt completion message already reports the new value, so the
            // contributor stays silent to avoid double-up.
            var bob = ProfileWithCorruption(-50);
            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion,
                CorruptionProcessor.CorruptType, isInitiator: false);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void PurifyInteractionType_SkipsSelfReference()
        {
            var bob = ProfileWithCorruption(50);
            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion,
                CorruptionProcessor.PurifyType, isInitiator: false);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void Fragment_HasNoLeadingWhitespace()
        {
            // Whitespace convention: AppendStatusFragments inserts the separator. Contributors
            // must not prepend their own.
            var bob = ProfileWithCorruption(-50);
            bob.displayName = "Bob";
            var result = _contributor.Contribute(bob, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
            Assert.NotEqual(" ", result.CompletionAppendix[0].Substring(0, 1));
        }

        [Fact]
        public void UnparseableCorruptionString_TreatedAsZero()
        {
            // Defends against legacy or hand-edited profiles where the field exists but
            // doesn't parse as an int.
            var profile = new ProfileBuilder().Build();
            profile.characteristics["corruption"] = "not-a-number";
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
            Assert.Empty(result.CompletionAppendix);
        }

        [Fact]
        public void MissingCorruptionField_ReturnsEmpty()
        {
            var profile = new ProfileBuilder().Build();
            var result = _contributor.Contribute(profile, StatusEffectCallSite.Completion, "kiss", isInitiator: false);
            Assert.Empty(result.CompletionAppendix);
        }

        private static Profile ProfileWithCorruption(int value)
        {
            return new ProfileBuilder()
                .WithUserName("Bob")
                .WithDisplayName("Bob")
                .WithCharacteristic(CorruptionProcessor.CorruptionCharacteristicKey, value.ToString())
                .Build();
        }
    }
}
