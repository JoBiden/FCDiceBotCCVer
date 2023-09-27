using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauStatues : ChatBotCommand
    {
        public ChateauStatues()
        {
            Name = "statues";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string location;
            if (terms.FirstOrDefault() != null)
            {
                location = commandController.GetIdentifierFromCommandTerms(rawTerms, "location");
            }
            string returnText = string.Empty;
            if (terms.Length < 1 || terms == null)
            {
                returnText = "Listed below are the number of 'statues' or otherwise petrified beings in every location. To see the individuals petrified at a given location, use !statues {location}. Note that only a character's most recent petrification will show here. Look at a character's full !history to see their whole story.\n\n[b]Location: # of statues[/n]\n";
                //List<Interaction> petrifyInteractions = MonDB.getInteractions("location"); do this over in monDb
            }
            
            else
            {
                returnText = "text";
            }

            bot.SendPrivateMessage(returnText, characterName);

            
        }
    }
}
