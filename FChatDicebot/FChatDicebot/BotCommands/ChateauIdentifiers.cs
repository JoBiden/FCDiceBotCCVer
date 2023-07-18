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
    public class ChateauIdentifiers : ChatBotCommand
    {
        public ChateauIdentifiers()
        {
            Name = "category";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            Dictionary<string, string> categoryDisplay = new Dictionary<string, string>
            {
                { "training", "Trainings" },
                { "job", "Jobs" },
                { "currency", "Currencies" },
                { "parasite", "Parasites" },
                { "attire", "Attire" },
                { "scent", "Scents" },
                { "vice", "Vices" },
                { "substance", "Substances" },
                { "object", "Objects" },
                { "plant", "Plants" },
                { "location", "Locations" },
                { "monster", "Monsters" },
                { "bodypart", "Bodyparts" },

            };
            string lowerTerm = string.Empty;
            if (terms.FirstOrDefault() != null)
            {
                lowerTerm = terms.FirstOrDefault().ToLower();
            }
            string returnText = string.Empty;
            if (terms.Length < 1 || terms == null)
            {
                returnText = "Use this command to list out the currently available identifiers within a certain category. If you want to see the description of a specific identifier, use !identifier instead. Please note that only the first category provided will be listed out, and any additional terms will be ignored.\n\nThe currently available categories are: \n";
                foreach (string category in categoryDisplay.Keys)
                {
                    returnText += category + ", ";
                }
                returnText = returnText.Remove(returnText.Length - 2);
            } else if (categoryDisplay.ContainsKey(lowerTerm))
            {
                returnText = "Currently known [b]" + categoryDisplay[lowerTerm] + "[/b] are: \n";
                foreach (Identifier identifier in MonDB.getIdentifiers(lowerTerm))
                {
                    returnText += identifier.type + ", ";
                }
                returnText = returnText.Remove(returnText.Length - 2);
            } else
            {
                returnText = "We didn't find that category in our records. Try \"!category\" with no additional arguments to get a list of all categories currently on record.";
            }

            bot.SendPrivateMessage(returnText, characterName);

        }
    }
}
