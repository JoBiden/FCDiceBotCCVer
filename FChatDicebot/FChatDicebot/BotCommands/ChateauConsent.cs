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
    public class ChateauConsent : ChatBotCommand
    {
        public ChateauConsent()
        {
            Name = "consent";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            List<PendingCommand> pendingList = MonDB.getPending(characterName);
            int numToConsent = Utils.GetNumberFromInputs(terms);
            string firstTerm = terms.FirstOrDefault();
            string privateMessage = string.Empty;
            string channelMessage = string.Empty;
            int maxNoSpoilerLength = 200;
            bool all = false;
            if (firstTerm != null)
            {
                if (firstTerm == "all"){
                    all = true;
                }
            }
            if (pendingList.Count == 0)
            {
                privateMessage = "No one is awaiting your consent. Maybe you can make the first move?~";
            }
            else if ((pendingList.Count == 1) || all)
            {
                foreach (PendingCommand toConsent in pendingList)
                {
                    ChateauInteractionHandler.addInteraction(toConsent);
                    if (channelMessage!= string.Empty){
                        channelMessage += "\n\n";
                    }
                    channelMessage += getInteractionMessage(toConsent.pendingInteraction.type, toConsent.pendingInteraction.initiator, toConsent.pendingInteraction.recipient);
                }
                if (channelMessage.Length > maxNoSpoilerLength)
                {
                    channelMessage = "Consent is such a wonderful thing~ This was a long one though, so the details are in the spoiler below. \n[spoiler]" + channelMessage + "[/spoiler]";
                }
            } 
            else if ((numToConsent >= 0) && (numToConsent <= pendingList.Count))
            {
                ChateauInteractionHandler.addInteraction(pendingList.ElementAt(numToConsent - 1));
            }

            else
            {
                privateMessage = "Use '!consent #' to consent to the correct interaction. You have the following interactions awaiting your consent:";
                int n = 1;
                foreach(PendingCommand toConsent in pendingList)
                {
                    privateMessage += "\n" + n + ": [b]" + toConsent.pendingInteraction.type + "[/b] initiated by [user]" + toConsent.pendingInteraction.initiator + "[/user]";
                    n++;
                }
                channelMessage = "There's multiple people awaiting your consent, " + characterName + ". Either '!consent all', or refer to the list we just messaged you.";
               
            }
            if (channelMessage != string.Empty)
            {
                bot.SendMessageInChannel(channelMessage, channel);
            }
            if (privateMessage != string.Empty)
            {
                bot.SendPrivateMessage(privateMessage, characterName);
            }
        }

        public string getInteractionMessage(string interactionType, string initiator, string recipient)
        {
            string returnString = string.Empty;
            var random = new Random();
            switch (interactionType)
            {
                case "kiss":
                    var kissDescriptors = new List<String> { "cute.", "that's kind of lewd...", "so salatious.", "hot!", "it sounded quite wet.", "short and sweet.", "slow and sensual.", "just a casual peck.", "doki..." };
                    returnString = "Mwah! "+ initiator + " and " + recipient + " share a kiss, " + kissDescriptors[random.Next(kissDescriptors.Count)];
                    break;

                case "handhold":
                    var handholdDescriptors = new List<String> { "Cute.", "That's kind of lewd...", "So salatious.", "Hot!", "When's the wedding?", "The forbidden act, out in the open..."};
                    returnString = "Ooh, " + initiator + " and " + recipient + " hold hands! " + handholdDescriptors[random.Next(handholdDescriptors.Count)];
                    break;
                case "cuddle":
                    var cuddleDescriptors = new List<String> { "Cute.", "That's kind of lewd...", "So salatious.", "Hot!", "Looks cozy!", "Is there room for one more?", initiator + " is definitely the big spoon.", recipient + " is definitely the little spoon." };
                    returnString = initiator + " and " + recipient + " cuddle up together. " + cuddleDescriptors[random.Next(cuddleDescriptors.Count)];
                    break;
                case "spank":
                    var spankDescriptors = new List<String> { "a sharp spank!", "a love tap to the ass.", "a smack that will leave a mark.", "a red imprint on their derriere.", "a surprisingly loving booty grope.", "an impact with enough force to cook a lesser being."};
                    returnString = initiator + " winds up and gives " + recipient + " " + spankDescriptors[random.Next(spankDescriptors.Count)];
                    break;
                case "bully":
                    var bullyDescriptors = new List<String> { "boolies them into submission!", "shows them whose boss!", "applies excessive force to their victim!", "spins them right round!", "establishes the pecking order!", "instills fear..." };
                    returnString = initiator + " takes " + recipient + " by the collar and " + bullyDescriptors[random.Next(bullyDescriptors.Count)];
                    break;
                default:
                    returnString = "What type of interaction was that? " + interactionType + "? For some reason, I don't recognize it...";
                    break;

            }
            return returnString;
        }
    }


}
