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
using System.Windows.Markup;

namespace FChatDicebot.BotCommands
{
    public class ChateauDossier : ChatBotCommand
    {
        public ChateauDossier()
        {
            Name = "dossier";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string targetUser;
            bool specialist = false;
            Dictionary<string, string> countDisplay = new Dictionary<string, string>
            {
                { "kiss", "Kisses Shared" },
                { "handhold", "Hands Held" },
                { "cuddle", "Cuties Cuddled" },
                { "cum", "Cum Count" },
                { "spanktake", "Spanks Taken" },
                { "spankgive", "Spanks Delivered" },
                { "bullygive", "Big Bullies" },
                { "bullytake", "Boolied" }
            };


            if (terms.Length < 1)
            {
                targetUser = characterName;
            }
            else
            {
                targetUser = commandController.GetUserNameFromCommandTerms(rawTerms);
            }
            string dossierText = "";
            Profile profile = MonDB.getProfile(targetUser);
            if (profile == null)
            {
                dossierText = "Dossier not found. Either they aren't registered, or you're looking for the wrong person (check your spelling!)";
            }
            else
            {
                dossierText += "[b][u]" + profile.displayName + " the ";
                List<string> specialistTextList = new List<string>();
                if (profile.characteristics.ContainsKey("monster"))
                {
                    dossierText += profile.characteristics["monster"].Substring(0, 1).ToUpper() + profile.characteristics["monster"].Substring(1) + ", ";
                }
                if (profile.counts.Count > 0)
                {
                    Dictionary<string, string> countSpecialistText = new Dictionary<string, string>
                    {
                        { "kiss", "Kissing" },
                        { "cuddle", "Cuddling" },
                        { "handhold", "Handholding" },
                        { "spanktake", "Spankbaiting" },
                        { "spankgive", "Spanking" },
                        { "bullygive", "Bullying" },
                        { "bullytake", "Bullybaiting" }
                    };
                    var maxValueKey = profile.counts.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                    specialistTextList.Add(countSpecialistText[maxValueKey]);
                }
                Dictionary<string, string> involvedSpecialistText = new Dictionary<string, string>
                    {
                        { "milkgive", "Livestock" },
                        { "milktake", "Milking" },
                        { "paygive", "Piggybank" },
                        { "paytake", "Currency Claiming" },
                        { "feedgive", "Feeding" },
                        { "feedtake", "Eating" },
                        { "goldengive", "Golden Flow" },
                        { "goldentake", "Golden Receptacle" },
                        { "pledge", "Pledging" },
                        { "dressupgive", "Beautifying" },
                        { "dressuptake", "Dressup" },
                        { "climaxgive", "Climax Claiming" },
                        { "climaxtake", "Climaxing" },
                        { "bond", "Bondbuilding" },
                    };
                string involvedSpecialistKey = getSpecialistKey(involvedSpecialistText);
                if (involvedSpecialistKey != null)
                {
                    dossierCommaListAddition(involvedSpecialistText[involvedSpecialistKey]);
                }
                Dictionary<string, string> commitmentSpecialistText = new Dictionary<string, string>
                    {
                        { "markgive", "Marking" },
                        { "marktake", "Mark Collecting" },
                        { "consumegive", "Devouring" },
                        { "consumetake", "Snack" },
                        { "petrifygive", "Petrifying" },
                        { "petrifytake", "Statuesque" },
                        { "plantgive", "Gardening" },
                        { "planttake", "Greenery" },
                        { "objectifygive", "Objectifying" },
                        { "objectifytake", "Objectified" },
                        { "entitlegive", "Title Bestowing" },
                        { "entitletake", "Title Claiming" },
                        { "breedgive", "Impregnation" },
                        { "breedtake", "Breeding" },
                        { "employgive", "Hiring" },
                        { "employtake", "Job Hopping" },
                        { "traingive", "Teaching" },
                        { "traintake", "Learning" },
                        { "corruptgive", "Corruptive" },
                        { "corrupttake", "Corrupted" }
                    };
                string commitmentSpecialistKey = getSpecialistKey(commitmentSpecialistText);
                if (commitmentSpecialistKey != null)
                {
                    specialistTextList.Add(commitmentSpecialistText[commitmentSpecialistKey]);
                }
                Dictionary<string, string> consequenceSpecialistText = new Dictionary<string, string>
                    {
                        { "monsterizegive", "Monster Making" },
                        { "monsterizetake", "Shapeshifting" },
                        { "infestgive", "Infesting" },
                        { "infesttake", "Infested" },
                        { "renamegive", "Naming" },
                        { "renametake", "Identity Hopping" },
                        { "odorizegive", "Perfuming" },
                        { "odorizetake", "Stench" },
                        { "cursegive", "Cursing" },
                        { "cursetake", "Cursebearing" },
                        { "breakgive", "Breaking" },
                        { "breaktake", "Broken" },
                        { "dosegive", "Addictive Substance" },
                        { "dosetake", "Addicted" }
                    };
                string consequenceSpecialistKey = getSpecialistKey(consequenceSpecialistText);
                if (consequenceSpecialistKey != null)
                {
                    specialistTextList.Add(consequenceSpecialistText[consequenceSpecialistKey]);
                }
                if (specialistTextList.Count > 0)
                {
                    for (int i = 0; i < specialistTextList.Count; i++) 
                    {
                        dossierText += specialistTextList[i];
                        switch (specialistTextList.Count - i) {
                            case 1:
                                dossierText += " Specialist";
                                break;
                            case 2:
                                dossierText += " and ";
                                break;
                            default:
                                dossierText += ", ";
                                break;
                        }
                        
                    }
                    
                }
                else //hanging 'the' to remove
                {
                    dossierText = dossierText.Substring(0, dossierText.Length - 5);
                }

                dossierText += "[/u][/b]\n\n";
                foreach (KeyValuePair<string, int> count in profile.counts)
                {
                    dossierText += countDisplay[count.Key] + ": " + count.Value + "   ";
                }
            }

            bot.SendPrivateMessage(dossierText, characterName);

            string getSpecialistKey(Dictionary<string, string> specialistDict)
            {
                long currentCount;
                long largestCount = 0;
                string largestKey = string.Empty;
                foreach (string involvedKey in specialistDict.Keys)
                {
                    if (involvedKey.EndsWith("give"))
                    {
                        currentCount = MonDB.getTypeCount(targetUser, involvedKey.Substring(0, involvedKey.Length - 4), "initiator");
                    }
                    else if (involvedKey.EndsWith("take"))
                    {
                        currentCount = MonDB.getTypeCount(targetUser, involvedKey.Substring(0, involvedKey.Length - 4), "recipient");
                    }
                    else
                    {
                        currentCount = MonDB.getTypeCount(targetUser, involvedKey, "both");
                    }
                    if (currentCount > largestCount)
                    {
                        largestCount = currentCount;
                        largestKey = involvedKey;
                    }
                }
                if (largestCount > 1)
                {
                    return largestKey;
                }
                return null;
            }

            void dossierCommaListAddition(string newText)
            {
                if (dossierText.EndsWith(" and "))
                {
                    dossierText = dossierText.Substring(0, dossierText.Length - 5) + ", ";
                }
                dossierText += newText + " and ";
                return;
            }
        }
    }
}
