using FChatDicebot.Model;
using System;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Regression test for L1: PrintedCommand's CDS-message length-limiting branch cast
    /// MessageDataFormat to MSGclient after confirming it was actually a CDSclient,
    /// throwing InvalidCastException for any channel description long enough to reach the
    /// truncation branch (silently dropping the whole update — no other caller catches it).
    /// </summary>
    public class BotMessageTests
    {
        [Fact]
        public void PrintedCommand_LongChannelDescription_TruncatesWithoutThrowing()
        {
            string longDescription = new string('x', 60000); // exceeds MaximumCharsInMessagePrivate (49050)
            var botMessage = new BotMessage
            {
                messageType = "CDS",
                MessageDataFormat = new CDSclient { channel = "ADH-1234", description = longDescription }
            };

            string printed = botMessage.PrintedCommand();

            Assert.NotNull(printed);
            var cds = (CDSclient)botMessage.MessageDataFormat;
            Assert.True(cds.description.Length < longDescription.Length);
            Assert.Contains("(maximum message length reached)", cds.description);
        }

        [Fact]
        public void PrintedCommand_ShortChannelDescription_LeavesDescriptionUnchanged()
        {
            var botMessage = new BotMessage
            {
                messageType = "CDS",
                MessageDataFormat = new CDSclient { channel = "ADH-1234", description = "A short description." }
            };

            botMessage.PrintedCommand();

            var cds = (CDSclient)botMessage.MessageDataFormat;
            Assert.Equal("A short description.", cds.description);
        }
    }
}
