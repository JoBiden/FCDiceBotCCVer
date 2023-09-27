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
            string recipientDisplayName = string.Empty;
            string initiatorDisplayName = string.Empty;
            string privateMessage = string.Empty;
            string channelMessage = string.Empty;
            int maxNoSpoilerLength = 300;
            int pendingMinutesKeep = 10;
            bool all = false;
            foreach (PendingCommand pendingCommand in pendingList)
            {
                if (pendingCommand.startTime.CompareTo(DateTime.UtcNow.AddMinutes(-pendingMinutesKeep)) < 0) //it has been more than the allotted time to keep the pending Interaction
                {
                    MonDB.removePending(pendingCommand.Id);
                }
            }
            pendingList = MonDB.getPending(characterName);

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
                    if (channelMessage != string.Empty)
                    {
                        channelMessage += "\n\n";
                    }
                    channelMessage += getInteractionMessage(toConsent.pendingInteraction.type, toConsent.pendingInteraction.identifier, toConsent.pendingInteraction.initiator, toConsent.pendingInteraction.recipient);
                    //channelMessage += getInteractionMessageWithDisplayNames(toConsent);
                }
                if (channelMessage.Length > maxNoSpoilerLength)
                {
                    channelMessage = "Consent is such a wonderful thing~ This was a long one though, so the details are in the spoiler below. \n[spoiler]" + channelMessage + "[/spoiler]";
                }
            } 
            else if ((numToConsent >= 0) && (numToConsent <= pendingList.Count))
            {
                PendingCommand toConsent = pendingList.ElementAt(numToConsent - 1);
                ChateauInteractionHandler.addInteraction(pendingList.ElementAt(numToConsent - 1));
                channelMessage += getInteractionMessage(toConsent.pendingInteraction.type, toConsent.pendingInteraction.identifier, toConsent.pendingInteraction.initiator, toConsent.pendingInteraction.recipient);
                //channelMessage += getInteractionMessageWithDisplayNames(toConsent);
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

            //string getInteractionMessageWithDisplayNames(PendingCommand pending)
            //{
            //    recipientDisplayName = MonDB.getProfile(pending.pendingInteraction.recipient).displayName;
            //    initiatorDisplayName = MonDB.getProfile(pending.pendingInteraction.initiator).displayName;
            //    string returnString = getInteractionMessage(pending.pendingInteraction.type, initiatorDisplayName, recipientDisplayName); //this is feeding display names to Rename which then uses display name to try and pull a profile
            //    return returnString;
            //}
        }

        public string getInteractionMessage(string interactionType, string identifier, string initiator, string recipient)
        {
            string returnString = string.Empty;
            var random = new Random();
            switch (interactionType)
            {
                case "kiss":
                    var kissDescriptors = new List<String> { "cute.", "that's kind of lewd...", "so salatious.", "hot!", "it sounded quite wet.", "short and sweet.", "slow and sensual.", "just a casual peck.", "doki..." };
                    returnString = "Mwah! "+ initiator + " and " + recipient + " share a kiss, " + kissDescriptors[random.Next(kissDescriptors.Count)];
                    if (initiator == "Queen Contract")
                    {
                        returnString = returnString + "[eicon]qckiss[eicon]";
                    }
                    break;

                case "handhold":
                    var handholdDescriptors = new List<String> { "Cute.", "That's kind of lewd...", "So salatious.", "Hot!", "When's the wedding?", "The forbidden act, out in the open..."};
                    returnString = "Ooh, " + initiator + " and " + recipient + " hold hands! " + handholdDescriptors[random.Next(handholdDescriptors.Count)];
                    break;
                case "cuddle":
                    var cuddleDescriptors = new List<String> { "Cute.", "That's kind of lewd...", "So salatious.", "Hot!", "Looks cozy!", "Is there room for one more?", initiator + " is definitely the big spoon.", recipient + " is definitely the little spoon." };
                    returnString = initiator + " and " + recipient + " cuddle up together. " + cuddleDescriptors[random.Next(cuddleDescriptors.Count)];
                    if (initiator == "Queen Contract")
                    {
                        if (recipient == "The Corrupted Rin")
                        {
                            returnString = returnString + "[eicon]rin_lap[/eicon]";
                        }
                        else
                        {
                            returnString = returnString + "[eicon]qchug[/eicon]";
                        }
                    }
                    break;
                case "spank":
                    var spankDescriptors = new List<String> { "a sharp spank!", "a love tap to the ass.", "a smack that will leave a mark.", "a red imprint on their derriere.", "a surprisingly loving booty grope.", "an impact with enough force to cook a lesser being."};
                    returnString = initiator + " winds up and gives " + recipient + " " + spankDescriptors[random.Next(spankDescriptors.Count)];
                    break;
                case "bully":
                    var bullyDescriptors = new List<String> { "boolies them into submission!", "shows them whose boss!", "applies excessive force to their victim!", "spins them right round!", "establishes the pecking order!", "instills fear..." };
                    returnString = initiator + " takes " + recipient + " by the collar and " + bullyDescriptors[random.Next(bullyDescriptors.Count)];
                    break;
                case "rename":
                    string newName = MonDB.getProfile(recipient).displayName;
                    returnString = initiator + " has made it known that " + recipient + " is to be known as " + newName + " henceforth! All occurences of their old name in our records will be changed to reflect their new identity.";
                    break;
                case "monsterize":
                    identifier = Utils.AnOrA(identifier) + " " + identifier;
                    returnString = initiator + " has bolstered monsterkind by turning " + recipient + " into " + identifier + "! We welcome all monsters to our Chateau, no matter what your origins. Enjoy your new life as " + identifier + "~";
                    break;
                case "petrify":                                      
                    returnString = initiator + " has petrified " + recipient + " " + Utils.LocationToText(identifier, initiator, recipient) + "! They might be stuck there for quite awhile... hopefully visitors enjoy the pose they're stuck in.";
                    break;
                case "plant":
                    identifier = Utils.AnOrA(identifier) + " " + identifier;
                    returnString = initiator + " has grown the garden by turning " + recipient + " into " + identifier + "! They might stay planted for for quite awhile... surely the gardeners will take good care of them.";
                    break;
                case "objectify":
                    identifier = Utils.AnOrA(identifier) + " " + identifier;
                    returnString = initiator + " has made " + recipient + " into some sort of " + identifier + "! Who knows what's in store for them, but they'll be stuck with their fate for quite awhile...";
                    break;
                case "dressup":
                    returnString = initiator + " has dressed up " + recipient + " in " + Utils.AttireToText(identifier) + "! Do a spin for everyone, let them admire your new garb!";
                    break;
                case "feed":
                    returnString = initiator + " has fed " + recipient + " some " + Utils.SubstanceToText(identifier) + "! Was it yummy? I bet it was.";
                    break;
                case "golden":
                    returnString = initiator + " breathes a sigh of relief as a golden fluid pours over " + recipient + "'s " + Utils.BodypartToText(identifier) + ".";
                    break;
                case "consume":
                    returnString = initiator + " consumes " + recipient + ", and they were never heard from again... or at least, it will be quite some time before they manage to escape, reform, or otherwise recover their strength.";
                    break;
                default:
                    returnString = "What type of interaction was that? " + interactionType + "? For some reason, I don't recognize it... tell [user]Queen Contract[/user] to check the 'ChateauConsent' code for me if you get a chance.";
                    break;

            }
            return returnString;
        }
    }


}
