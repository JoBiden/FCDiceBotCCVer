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
            string location = string.Empty;
            if (terms.FirstOrDefault() != null)
            {
                location = commandController.GetIdentifierFromCommandTerms(rawTerms, "location");
            }
            string returnText = string.Empty;
            List<Interaction> petrifyInteractions = MonDB.getInteractionsByType("petrify");
            if (terms.Length < 1 || terms == null)
            {
                returnText = "Listed below are the number of 'statues' or otherwise petrified beings in every location. To see the individuals petrified at a given location, use !statues {location}.\n";
                
                Dictionary<string, int> statueNum = new Dictionary<string, int> { };
                foreach (Interaction interaction in petrifyInteractions)
                {
                    if (!statueNum.ContainsKey(interaction.identifier))
                    {
                        statueNum.Add(interaction.identifier, 1);
                    }
                    else
                    {
                        statueNum[interaction.identifier]++;
                    } 
                }
                foreach (KeyValuePair<string,int> count in statueNum)
                {
                    returnText += "\nStatues " + Utils.LocationToText(count.Key, "the initiator", "the recipient") + ": " + count.Value;
                }
            }
            
            else
            {
                if (location == null)
                {
                    returnText = ChateauInteractionHandler.typeNotFoundText("location");
                }
                else
                {
                    returnText = "Listed below is the identity of every resident that has been petrified " + Utils.LocationToText(location, "the initiator", "the recipient") + ", as well as their species (if known) to indicate their appearance.\n";
                    foreach (Interaction interaction in petrifyInteractions)
                    {
                        if (interaction.identifier == location)
                        {
                            Profile statue = MonDB.getProfile(interaction.recipient);
                            returnText += "\n" + statue.displayName;
                            if (statue.characteristics.ContainsKey("monster"))
                            {
                                returnText += ", " + statue.characteristics["monster"];
                            }
                            List<string> initiatorLocation = new List<string>() { "theirroom", "myoffice", "mycave", "myworkshop"};
                            if (initiatorLocation.Contains(location))
                            {
                                returnText += " " + Utils.LocationToText(location, interaction.initiator, interaction.recipient);
                            }
                        }
                    }
                }
            }

            bot.SendPrivateMessage(returnText, characterName);
        }
    }
}
