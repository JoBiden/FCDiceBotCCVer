using FChatDicebot;
using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for Utils.cs number parsing and game-related functions
    /// </summary>
    public class UtilsNumberParsingTests
    {
        #region GetNumberFromInputs Tests

        [Fact]
        public void GetNumberFromInputs_FindsFirstPositiveNumber()
        {
            string[] input = { "hello", "42", "world" };

            int result = Utils.GetNumberFromInputs(input);

            Assert.Equal(42, result);
        }

        [Fact]
        public void GetNumberFromInputs_MultipleNumbers_ReturnsFirst()
        {
            string[] input = { "10", "20", "30" };

            int result = Utils.GetNumberFromInputs(input);

            Assert.Equal(10, result);
        }

        [Fact]
        public void GetNumberFromInputs_NoNumbers_ReturnsZero()
        {
            string[] input = { "no", "numbers", "here" };

            int result = Utils.GetNumberFromInputs(input);

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetNumberFromInputs_EmptyArray_ReturnsZero()
        {
            string[] input = new string[0];

            int result = Utils.GetNumberFromInputs(input);

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetNumberFromInputs_Null_ReturnsZero()
        {
            string[] input = null;

            int result = Utils.GetNumberFromInputs(input);

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetNumberFromInputs_ZeroAndNegative_IgnoresZeroReturnsNegative()
        {
            string[] input = { "0", "-5", "text" };

            int result = Utils.GetNumberFromInputs(input);

            Assert.Equal(-5, result);
        }

        [Fact]
        public void GetNumberFromInputs_MixedContent_FindsNumber()
        {
            string[] input = { "text", "0", "100", "more" };

            int result = Utils.GetNumberFromInputs(input);

            Assert.Equal(100, result);
        }

        #endregion

        #region GetAllNumbersFromInputs Tests

        [Fact]
        public void GetAllNumbersFromInputs_FindsAllPositiveNumbers()
        {
            string[] input = { "10", "hello", "20", "world", "30" };

            List<int> result = Utils.GetAllNumbersFromInputs(input);

            Assert.Equal(new List<int> { 10, 20, 30 }, result);
        }

        [Fact]
        public void GetAllNumbersFromInputs_NoNumbers_ReturnsEmpty()
        {
            string[] input = { "no", "numbers" };

            List<int> result = Utils.GetAllNumbersFromInputs(input);

            Assert.Empty(result);
        }

        [Fact]
        public void GetAllNumbersFromInputs_EmptyArray_ReturnsEmpty()
        {
            string[] input = new string[0];

            List<int> result = Utils.GetAllNumbersFromInputs(input);

            Assert.Empty(result);
        }

        [Fact]
        public void GetAllNumbersFromInputs_Null_ReturnsEmpty()
        {
            string[] input = null;

            List<int> result = Utils.GetAllNumbersFromInputs(input);

            Assert.Empty(result);
        }

        [Fact]
        public void GetAllNumbersFromInputs_ZeroAndNegative_IgnoresThem()
        {
            string[] input = { "0", "-5", "10", "-20", "5" };

            List<int> result = Utils.GetAllNumbersFromInputs(input);

            Assert.Equal(new List<int> { 10, 5 }, result);
        }

        #endregion

        #region Percentile Tests

        [Fact]
        public void Percentile_Threshold100_AlwaysTrue()
        {
            Random rnd = new Random(12345);

            for (int i = 0; i < 10; i++)
            {
                bool result = Utils.Percentile(rnd, 100.0);
                Assert.True(result);
            }
        }

        [Fact]
        public void Percentile_Threshold0_AlwaysFalse()
        {
            Random rnd = new Random(12345);

            for (int i = 0; i < 10; i++)
            {
                bool result = Utils.Percentile(rnd, 0.0);
                Assert.False(result);
            }
        }

        [Fact]
        public void Percentile_ThresholdNegative_AlwaysFalse()
        {
            Random rnd = new Random(12345);

            for (int i = 0; i < 10; i++)
            {
                bool result = Utils.Percentile(rnd, -10.0);
                Assert.False(result);
            }
        }

        [Fact]
        public void Percentile_Threshold50_ReturnsVariedResults()
        {
            Random rnd = new Random(12345);
            int trueCount = 0;
            int falseCount = 0;

            for (int i = 0; i < 100; i++)
            {
                bool result = Utils.Percentile(rnd, 50.0);
                if (result) trueCount++;
                else falseCount++;
            }

            // With 50% threshold, we should get some of each (not exact due to randomness)
            Assert.True(trueCount > 0);
            Assert.True(falseCount > 0);
        }

        #endregion

        #region GetRollModifierString Tests

        [Fact]
        public void GetRollModifierString_Positive_ReturnsWithPlus()
        {
            string result = Utils.GetRollModifierString(5);

            Assert.Equal(" +5", result);
        }

        [Fact]
        public void GetRollModifierString_Negative_ReturnsWithMinus()
        {
            string result = Utils.GetRollModifierString(-3);

            Assert.Equal(" -3", result);
        }

        [Fact]
        public void GetRollModifierString_Zero_ReturnsEmpty()
        {
            string result = Utils.GetRollModifierString(0);

            Assert.Equal("", result);
        }

        #endregion

        #region GetDeckTypeString Tests

        [Fact]
        public void GetDeckTypeString_Playing_ReturnsPlaying()
        {
            string result = Utils.GetDeckTypeString(DeckType.Playing, "custom");

            Assert.Equal("Playing", result);
        }

        [Fact]
        public void GetDeckTypeString_Tarot_ReturnsTarot()
        {
            string result = Utils.GetDeckTypeString(DeckType.Tarot, "custom");

            Assert.Equal("Tarot", result);
        }

        [Fact]
        public void GetDeckTypeString_ManyThings_ReturnsManyThings()
        {
            string result = Utils.GetDeckTypeString(DeckType.ManyThings, "custom");

            Assert.Equal("Many Things", result);
        }

        [Fact]
        public void GetDeckTypeString_Uno_ReturnsUno()
        {
            string result = Utils.GetDeckTypeString(DeckType.Uno, "custom");

            Assert.Equal("Uno", result);
        }

        [Fact]
        public void GetDeckTypeString_Custom_ReturnsCustomName()
        {
            string result = Utils.GetDeckTypeString(DeckType.Custom, "MySpecialDeck");

            Assert.Equal("MySpecialDeck", result);
        }

        #endregion

        #region GetDeckTypeStringHidePlaying Tests

        [Fact]
        public void GetDeckTypeStringHidePlaying_Playing_ReturnsEmpty()
        {
            string result = Utils.GetDeckTypeStringHidePlaying(DeckType.Playing, "custom");

            Assert.Equal("", result);
        }

        [Fact]
        public void GetDeckTypeStringHidePlaying_Tarot_ReturnsInParentheses()
        {
            string result = Utils.GetDeckTypeStringHidePlaying(DeckType.Tarot, "custom");

            Assert.Equal("(Tarot) ", result);
        }

        [Fact]
        public void GetDeckTypeStringHidePlaying_Custom_ReturnsCustomInParentheses()
        {
            string result = Utils.GetDeckTypeStringHidePlaying(DeckType.Custom, "MyDeck");

            Assert.Equal("(MyDeck) ", result);
        }

        #endregion

        #region GetCardMoveTypeString Tests

        [Fact]
        public void GetCardMoveTypeString_Enum_DiscardCard_ReturnsDiscarded()
        {
            string result = Utils.GetCardMoveTypeString(CardMoveType.DiscardCard);

            Assert.Equal("discarded", result);
        }

        [Fact]
        public void GetCardMoveTypeString_Enum_PlayCard_ReturnsPlayed()
        {
            string result = Utils.GetCardMoveTypeString(CardMoveType.PlayCard);

            Assert.Equal("played", result);
        }

        [Fact]
        public void GetCardMoveTypeString_Enum_ToHandFromDiscard_ReturnsCorrect()
        {
            string result = Utils.GetCardMoveTypeString(CardMoveType.ToHandFromDiscard);

            Assert.Equal("moved to hand from discard", result);
        }

        [Fact]
        public void GetCardMoveTypeString_Piles_ReturnsFromTo()
        {
            string result = Utils.GetCardMoveTypeString(CardPileId.Hand, CardPileId.Discard);

            Assert.Equal("from hand to discard", result);
        }

        #endregion

        #region GetPileName Tests

        [Fact]
        public void GetPileName_Hand_ReturnsHand()
        {
            string result = Utils.GetPileName(CardPileId.Hand);

            Assert.Equal("hand", result);
        }

        [Fact]
        public void GetPileName_Deck_ReturnsDeck()
        {
            string result = Utils.GetPileName(CardPileId.Deck);

            Assert.Equal("deck", result);
        }

        [Fact]
        public void GetPileName_Discard_ReturnsDiscard()
        {
            string result = Utils.GetPileName(CardPileId.Discard);

            Assert.Equal("discard", result);
        }

        [Fact]
        public void GetPileName_Play_ReturnsPlay()
        {
            string result = Utils.GetPileName(CardPileId.Play);

            Assert.Equal("play", result);
        }

        [Fact]
        public void GetPileName_None_ReturnsNoPile()
        {
            string result = Utils.GetPileName(CardPileId.NONE);

            Assert.Equal("noPile", result);
        }

        #endregion

        #region GetRouletteBetString Tests

        [Fact]
        public void GetRouletteBetString_Black_ReturnsBlack()
        {
            string result = Utils.GetRouletteBetString(RouletteBet.Black, 0);

            Assert.Equal("Black", result);
        }

        [Fact]
        public void GetRouletteBetString_Red_ReturnsRed()
        {
            string result = Utils.GetRouletteBetString(RouletteBet.Red, 0);

            Assert.Equal("Red", result);
        }

        [Fact]
        public void GetRouletteBetString_Even_ReturnsEven()
        {
            string result = Utils.GetRouletteBetString(RouletteBet.Even, 0);

            Assert.Equal("Even", result);
        }

        [Fact]
        public void GetRouletteBetString_Odd_ReturnsOdd()
        {
            string result = Utils.GetRouletteBetString(RouletteBet.Odd, 0);

            Assert.Equal("Odd", result);
        }

        [Fact]
        public void GetRouletteBetString_First12_ReturnsFirst12()
        {
            string result = Utils.GetRouletteBetString(RouletteBet.First12, 0);

            Assert.Equal("First 12", result);
        }

        [Fact]
        public void GetRouletteBetString_SpecificNumber_ReturnsNumberWithValue()
        {
            string result = Utils.GetRouletteBetString(RouletteBet.SpecificNumber, 17);

            Assert.Equal("Number 17", result);
        }

        [Fact]
        public void GetRouletteBetString_None_ReturnsNone()
        {
            string result = Utils.GetRouletteBetString(RouletteBet.NONE, 0);

            Assert.Equal("NONE", result);
        }

        #endregion
    }
}
