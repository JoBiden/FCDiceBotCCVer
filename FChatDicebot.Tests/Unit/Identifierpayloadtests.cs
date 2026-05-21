using FChatDicebot.InteractionProcessors;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Unit tests for the shared <see cref="IdentifierPayload"/> encoder/decoder. The
    /// per-processor façades (Corruption/Milk/Climaxfor) have their own round-trip tests
    /// that also exercise this helper through delegation; these tests cover the primitive
    /// directly so the contract documented on the helper itself stays guaranteed.
    /// </summary>
    public class IdentifierPayloadTests
    {
        // -------------------------------------------------------------------
        // Compose
        // -------------------------------------------------------------------

        [Fact]
        public void Compose_StandardHeadAndTail_JoinsWithPipe()
        {
            Assert.Equal("corrupt|5", IdentifierPayload.Compose("corrupt", 5));
        }

        [Fact]
        public void Compose_NullHead_TreatedAsEmpty()
        {
            // Round-trip must still work — extract should return empty string for the head.
            string payload = IdentifierPayload.Compose(null, 3);
            Assert.Equal("|3", payload);
        }

        [Fact]
        public void Compose_NegativeTail_PreservedVerbatim()
        {
            // Negative tails are legal in the payload itself (e.g. corruption sign carriage);
            // it's the parsing helpers that apply per-processor sign rules.
            Assert.Equal("purify|-2", IdentifierPayload.Compose("purify", -2));
        }

        // -------------------------------------------------------------------
        // ExtractHead
        // -------------------------------------------------------------------

        [Fact]
        public void ExtractHead_NullInput_ReturnsNull()
        {
            Assert.Null(IdentifierPayload.ExtractHead(null));
        }

        [Fact]
        public void ExtractHead_EmptyInput_ReturnsNull()
        {
            Assert.Null(IdentifierPayload.ExtractHead(string.Empty));
        }

        [Fact]
        public void ExtractHead_NoSeparator_ReturnsWholeInput()
        {
            // Command-time shape: the processor hasn't stamped the tail yet, so the
            // identifier is just the head verb/substance.
            Assert.Equal("musk", IdentifierPayload.ExtractHead("musk"));
        }

        [Fact]
        public void ExtractHead_WithSeparator_ReturnsHeadPortion()
        {
            Assert.Equal("corrupt", IdentifierPayload.ExtractHead("corrupt|7"));
        }

        [Fact]
        public void ExtractHead_EmptyHead_ReturnsEmptyString()
        {
            // "|7" has a present-but-empty head — distinct from no-separator. Callers
            // generally coalesce to a default with ?? so the empty case is harmless.
            Assert.Equal(string.Empty, IdentifierPayload.ExtractHead("|7"));
        }

        // -------------------------------------------------------------------
        // TryExtractTail
        // -------------------------------------------------------------------

        [Fact]
        public void TryExtractTail_NullInput_ReturnsFalse()
        {
            Assert.False(IdentifierPayload.TryExtractTail(null, out int tail));
            Assert.Equal(0, tail);
        }

        [Fact]
        public void TryExtractTail_NoSeparator_ReturnsFalse()
        {
            Assert.False(IdentifierPayload.TryExtractTail("musk", out int tail));
            Assert.Equal(0, tail);
        }

        [Fact]
        public void TryExtractTail_TrailingSeparator_ReturnsFalse()
        {
            // "corrupt|" — separator is the last character, no integer to parse.
            Assert.False(IdentifierPayload.TryExtractTail("corrupt|", out int tail));
            Assert.Equal(0, tail);
        }

        [Fact]
        public void TryExtractTail_NonNumericTail_ReturnsFalse()
        {
            Assert.False(IdentifierPayload.TryExtractTail("corrupt|bogus", out int tail));
            Assert.Equal(0, tail);
        }

        [Fact]
        public void TryExtractTail_ValidPositiveInteger_Succeeds()
        {
            Assert.True(IdentifierPayload.TryExtractTail("corrupt|7", out int tail));
            Assert.Equal(7, tail);
        }

        [Fact]
        public void TryExtractTail_ValidNegativeInteger_Succeeds()
        {
            // The primitive accepts any integer; per-processor logic decides whether a
            // negative tail is meaningful.
            Assert.True(IdentifierPayload.TryExtractTail("purify|-3", out int tail));
            Assert.Equal(-3, tail);
        }

        [Fact]
        public void TryExtractTail_ZeroTail_Succeeds()
        {
            // Zero is a legitimate stamped value (e.g. milk's TOCTOU clamp-to-zero path).
            // Callers distinguish it from "no tail recorded" by checking the return bool.
            Assert.True(IdentifierPayload.TryExtractTail("cum|0", out int tail));
            Assert.Equal(0, tail);
        }

        // -------------------------------------------------------------------
        // ExtractTailOr
        // -------------------------------------------------------------------

        [Fact]
        public void ExtractTailOr_MissingTail_ReturnsDefault()
        {
            Assert.Equal(-1, IdentifierPayload.ExtractTailOr("cum", -1));
        }

        [Fact]
        public void ExtractTailOr_ValidTail_ReturnsParsed()
        {
            Assert.Equal(3, IdentifierPayload.ExtractTailOr("cum|3", -1));
        }

        // -------------------------------------------------------------------
        // Round-trip
        // -------------------------------------------------------------------

        [Fact]
        public void RoundTrip_PreservesBothPortions()
        {
            string composed = IdentifierPayload.Compose("monsterize", 42);
            Assert.Equal("monsterize", IdentifierPayload.ExtractHead(composed));
            Assert.True(IdentifierPayload.TryExtractTail(composed, out int tail));
            Assert.Equal(42, tail);
        }
    }
}
