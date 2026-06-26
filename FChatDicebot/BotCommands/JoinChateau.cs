using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class JoinChateau : ChatBotCommand
    {
        public JoinChateau()
        {
            Name = "joinchateau";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Register your character with the Chateau system";
            LongDescription = "Register yourself as a resident of the Chateau, creating your profile in the database. This is required before you can use most Chateau commands. You only need to do this once per character.";
            Usage = "!joinchateau";
            RelatedCommands = new string[] { "dossier", "bank", "work" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string confirmMessage = MonDB.registerUserChateau(characterName);
            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(confirmMessage, characterName);
            }
            else
            {
                if (confirmMessage.Substring(0,1) == "Y")
                {
                    confirmMessage += " Showing someone the ropes?";
                }
                bot.SendMessageInChannel(confirmMessage, channel);
            }
        }
    }
}
