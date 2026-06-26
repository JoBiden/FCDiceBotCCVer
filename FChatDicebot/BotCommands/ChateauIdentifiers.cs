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
            Aliases = new string[] { "list", "identifiers" };
            Category = "General";
            ShortDescription = "List all identifiers in a category";
            LongDescription = "Display all available identifiers for a specific category (location, monster, object, plant etc.) an interaction might require, or subcategory (aquatic, bird, groundfloor, etc) that helps narrow your options. If used without a provided category, will list all available categories and subcategories. Knowledge is power!";
            Usage = "!category {category}\nor\n!category";
            RelatedCommands = new string[] { "identifier", "whatis", "volunteer" };
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
                { "bond", "Bonds" },
                { "break", "Breakable Bodyparts"}
            };
            Dictionary<string, string> subCategoryDisplay = new Dictionary<string, string>
            {
                { "groundfloor", "Ground Floor Locations" },
                { "secondfloor", "Second Floor Locations" },
                { "topfloor", "Top Floor Locations" },
                { "exterior", "Exterior Locations" },
                { "alraune", "Alraune-Like Monsters" },
                { "angel", "Angel-Like Monsters" },
                { "insect", "Insect-Like Monsters" },
                { "beast", "Beast-Like Monsters" },
                { "arachne", "Arachne-Like Monsters" },
                { "aquatic", "Aquatic Monsters" },
                { "cat", "Cat-Like Monsters" },
                { "dragon", "Dragon-Like Monsters" },
                { "wonderland", "Wonderland Monsters" },
                { "giant", "Giant-Like Monsters" },
                { "fox", "Fox-Like Monsters" },
                { "bird", "Bird-Like Monsters" },
                { "infernal", "Infernal Monsters" },
                { "snake", "Snake-Like Monsters" },
                { "mimic", "Mimic-Like Monsters" },
                { "cow", "Cow-Like Monsters" },
                { "slime", "Slime-Like Monsters" },
                { "elemental", "Elemental Monsters" },
                { "dog", "Dog-Like Monsters" },
                { "worm", "Worm-Like Monsters" },
                { "undead", "Undead Monsters" },
                { "construct", "Construct Monsters" },
                { "perfume", "Naturally Aromatic Monsters" },
                { "horned", "Horned Monsters" },
                { "flight", "Flying Monsters" },
                { "feathered", "Feathered Monsters" },
                { "corrupt", "Corrupt Monsters" },
                { "holy", "Holy Monsters" },
                { "carapace", "Naturally Armored Monsters" },
                { "poison", "Toxic Monsters" },
                { "mount", "Mountable Monsters" },
                { "nocturnal", "Monsters of the Night" },
                { "cutting", "Monsters with Natural Blades" },
                { "intangible", "Intangible Monsters" },
                { "electric", "Electric Monsters" },
                { "fiery", "Fiery Monsters" },

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
                returnText += Utils.sortedListDisplayText(categoryDisplay.Keys.ToList());
                returnText += "\n\nCurrently available subcategories are: \n";
                returnText += Utils.sortedListDisplayText(subCategoryDisplay.Keys.ToList());
            }
            else if (categoryDisplay.ContainsKey(lowerTerm))
            {
                returnText = "Currently known [b]" + categoryDisplay[lowerTerm] + "[/b] are: \n";
                List<string> categoryList = new List<string>();
                foreach (Identifier identifier in MonDB.getIdentifiers(lowerTerm))
                {
                    categoryList.Add(identifier.type);
                }
                returnText += Utils.sortedListDisplayText(categoryList);
            }
            else if (subCategoryDisplay.ContainsKey(lowerTerm))
            {
                returnText = "Currently known [b]" + subCategoryDisplay[lowerTerm] + "[/b] are: \n";
                List<string> subCategoryList = new List<string>();
                foreach (Identifier identifier in MonDB.getIdentifiers(lowerTerm))
                {
                    subCategoryList.Add(identifier.type);
                }
                returnText += Utils.sortedListDisplayText(subCategoryList);
            }
            else
            {
                returnText = "We didn't find that category in our records. Try \"!category\" with no additional arguments to get a list of all categories currently on record.";
            }

            bot.SendPrivateMessage(returnText, characterName);

            
        }
    }
}
