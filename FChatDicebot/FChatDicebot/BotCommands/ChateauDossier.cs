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
            Dictionary<string, string> countDisplay = new Dictionary<string, string>
            {
                { "kiss", "Kisses Shared" },
                { "handhold", "Hands Held" },
                { "cuddle", "Cuties Cuddled" },
                { "cum", "Cum Count" },
                { "spanktake", "Spanks Delivered" },
                { "spankgive", "Spanks Taken" },
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
                if (profile.userName != profile.displayName)
                {
                    dossierText += "[s]" + profile.userName + "[/s] ";
                }
                dossierText += "[b][u]" + profile.displayName + " ";
                if (profile.counts.Count > 0 ){
                    Dictionary<string, string> specialistText = new Dictionary<string, string>
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
                    dossierText += "the " + specialistText[maxValueKey] + " Specialist";
                }
                dossierText += "[/u][/b]\n\n";
                foreach ( KeyValuePair<string, int> count in profile.counts)
                {
                    dossierText += countDisplay[count.Key] + ": " + count.Value + "   ";
                }
            }
            

                
            bot.SendPrivateMessage(dossierText, characterName);
            
        }
    }
}
