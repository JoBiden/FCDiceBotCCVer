using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    public class ModMessage : ChatBotCommand
    {
        public ModMessage()
        {
            Name = "modmessage";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "View moderator announcements and messages";
            LongDescription = "Display moderator messages and announcements for the Chateau. Can view all messages or filter by category. Useful for staying informed about Chateau news, rule changes, or important updates.";
            Usage = "!modmessage\nor\n!modmessage [category]";
            RelatedCommands = new string[] { "help", "botinfo" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            if (terms.Length < 1)
            {
                terms =  new string[1];
                terms[0] = "all";
            }
            string messageText = MonDB.modMessage(terms[0]);

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(messageText, characterName);
            }
            else
            {
                bot.SendMessageInChannel(messageText, channel);
            }
        }
    }
}
