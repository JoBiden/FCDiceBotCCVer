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
            Aliases = new string[] { "c" };
            Category = "General";
            ShortDescription = "Consent to a pending interaction";
            LongDescription = "Give your consent to a pending interaction request from another character. When someone requests an interaction with you, you must !consent for it to be completed and recorded in both dossiers.\n\nPending requests expire after 10 minutes. If more than one interaction is awaiting your consent, you will be messaged a numbered list of the interactions, and can choose which interaction to consent to, or '!consent all' to consent to every interaction awaiting your consent.";
            Usage = "!consent\nor\n!consent all\nor\n!consent {number}\nor\n!c\nor\n!c all\nor\n!c {number}";
            RelatedCommands = new string[] { "kiss", "cuddle", "bully" };
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
            List<PendingCommand> pendingList = MonDB.getPending(characterName);
            int numToConsent = Utils.GetNumberFromInputs(terms);
            string firstTerm = terms.FirstOrDefault();
            string recipientDisplayName = string.Empty;
            string initiatorDisplayName = string.Empty;
            string privateMessage = string.Empty;
            string channelMessage = string.Empty;
            int maxNoSpoilerLength = 500;
            int pendingMinutesKeep = 10;
            bool all = false;
            foreach (PendingCommand pendingCommand in pendingList)
            {
                if (pendingCommand.startTime.CompareTo(DateTime.UtcNow.AddMinutes(-pendingMinutesKeep)) < 0) //it has been more than the allotted time to keep the pending Interaction
                {
                    MonDB.removePendingInteraction(pendingCommand.Id);
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
                        // Try new processor system first
                        var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor(toConsent.pendingInteraction.type);
                        if (processor != null)
                        {
                            // Process the interaction (saves to DB, updates profiles)
                            processor.ProcessInteraction(toConsent);

                            //get completion message
                            Profile initProfile = MonDB.getProfile(toConsent.pendingInteraction.initiator);
                            Profile recipProfile = MonDB.getProfile(toConsent.pendingInteraction.recipient);
                            channelMessage += processor.GetCompletionMessage(initProfile, recipProfile, toConsent.pendingInteraction.identifier);
                            channelMessage += CheckRateLimitsAndGetMessage(toConsent.pendingInteraction);
                        }
                        else
                        {
                            // Fallback for non-migrated interactions
                            ChateauInteractionHandler.addInteraction(toConsent);
                            channelMessage += getInteractionMessage(toConsent.pendingInteraction.type, toConsent.pendingInteraction.identifier, toConsent.pendingInteraction.initiator, toConsent.pendingInteraction.recipient);
                            channelMessage += CheckRateLimitsAndGetMessage(toConsent.pendingInteraction);
                        }
                    }
                    channelMessage += getInteractionMessage(toConsent.pendingInteraction.type, toConsent.pendingInteraction.identifier, toConsent.pendingInteraction.initiator, toConsent.pendingInteraction.recipient);

                    channelMessage = CheckAchievementsAndAppendToMessage(channelMessage, toConsent.pendingInteraction.initiator);
                    channelMessage = CheckAchievementsAndAppendToMessage(channelMessage, toConsent.pendingInteraction.recipient);
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
                // Try new processor system first
                var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor(toConsent.pendingInteraction.type);
                if (processor != null)
                {
                    // Process the interaction (saves to DB, updates profiles)
                    processor.ProcessInteraction(toConsent);

                    // Get the completion message
                    Profile initProfile = MonDB.getProfile(toConsent.pendingInteraction.initiator);
                    Profile recipProfile = MonDB.getProfile(toConsent.pendingInteraction.recipient);
                    channelMessage += processor.GetCompletionMessage(initProfile, recipProfile, toConsent.pendingInteraction.identifier);
                }
                else
                {
                    // Fallback for non-migrated interactions
                    ChateauInteractionHandler.addInteraction(toConsent);
                    channelMessage += getInteractionMessage(toConsent.pendingInteraction.type, toConsent.pendingInteraction.identifier, toConsent.pendingInteraction.initiator, toConsent.pendingInteraction.recipient);
                }
                channelMessage = CheckAchievementsAndAppendToMessage(channelMessage, toConsent.pendingInteraction.initiator);
                channelMessage = CheckAchievementsAndAppendToMessage(channelMessage, toConsent.pendingInteraction.recipient);
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

            string CheckAchievementsAndAppendToMessage(string message, string profile)
            {
                // Check for system title achievements
                string titlesText = ChateauSystemTitles.CheckAndGrantTitles(profile);

                // Append title notifications to the message
                if (!string.IsNullOrEmpty(titlesText))
                {
                    message += titlesText;
                }

                return message;
            }
        }

        private string CheckRateLimitsAndGetMessage(Interaction interaction)
        {
            string initiator = interaction.initiator;
            string recipient = interaction.recipient;
            string type = interaction.type;

            // Determine count keys based on interaction type
            (string initKey, string recKey) = GetCountKeys(type);

            bool initLimited = MonDB.IsCountRateLimited(initiator, initKey);
            bool recLimited = MonDB.IsCountRateLimited(recipient, recKey);

            if (!initLimited && !recLimited) return string.Empty;

            Profile initProfile = MonDB.getProfile(initiator);
            Profile recProfile = MonDB.getProfile(recipient);

            if (initLimited && recLimited)
            {
                return $"\n\n[sub]Looks like that didn't make it into either dossier though... The clerks were probably still busy processing their last {type}(s).[/sub]";
            }
            else if (initLimited)
            {
                return $"\n\n[sub]Looks like that didn't make it into {initProfile.displayName}'s dossier though... The clerks were probably still busy processing their last {type}.[/sub]";
            }
            else
            {
                return $"\n\n[sub]Looks like that didn't make it into {recProfile.displayName}'s dossier though... The clerks were probably still busy processing their last {type}.[/sub]";
            }
        }

        private (string, string) GetCountKeys(string interactionType)
        {
            switch (interactionType.ToLower())
            {
                case "kiss": return ("kiss", "kiss");
                case "cuddle": return ("cuddle", "cuddle");
                case "handhold": return ("handhold", "handhold");
                case "spank": return ("spankgive", "spanktake");
                case "bully": return ("bullygive", "bullytake");
                case "feed": return ("feedgive", "feedtake");
                case "golden": return ("goldengive", "goldentake");
                case "dressup": return ("dressupgive", "dressuptake");
                default: return (string.Empty, string.Empty);
            }
        }


        //theoretically now defunct, but keeping around for fallback safety
        public string getInteractionMessage(string interactionType, string identifier, string initiator, string recipient)
        {
            Profile initiatorProfile = MonDB.getProfile(initiator);
            Profile recipientProfile = MonDB.getProfile(recipient);
            string returnString = string.Empty;
            var random = new Random();
            switch (interactionType)
            {
                case "kiss":
                    var kissDescriptors = new List<String> { "cute.", "that's kind of lewd...", "so salatious.", "hot!", "it sounded quite wet.", "short and sweet.", "slow and sensual.", "just a casual peck.", "doki..." };
                    returnString = "Mwah! "+ initiatorProfile.displayName + " and " + recipientProfile.displayName + " share a kiss, " + kissDescriptors[random.Next(kissDescriptors.Count)];
                    if (initiator == "Queen Contract")
                    {
                        returnString += "[eicon]qckiss[eicon]";
                    }
                    break;

                case "handhold":
                    var handholdDescriptors = new List<String> { "Cute.", "That's kind of lewd...", "So salatious.", "Hot!", "When's the wedding?", "The forbidden act, out in the open..."};
                    returnString = "Ooh, " + initiatorProfile.displayName + " and " + recipientProfile.displayName + " hold hands! " + handholdDescriptors[random.Next(handholdDescriptors.Count)];
                    break;
                case "cuddle":
                    var cuddleDescriptors = new List<String> { "Cute.", "That's kind of lewd...", "So salatious.", "Hot!", "Looks cozy!", "Is there room for one more?", initiatorProfile.displayName + " is definitely the big spoon.", recipientProfile.displayName + " is definitely the little spoon." };
                    returnString = initiatorProfile.displayName + " and " + recipientProfile.displayName + " cuddle up together. " + cuddleDescriptors[random.Next(cuddleDescriptors.Count)];
                    if (initiator == "Queen Contract" || recipient == "Queen Contract")
                    {
                        if (initiator == "The Corrupted Rin" || recipient == "The Corrupted Rin")
                        {
                            returnString += " [eicon]rin_lap[/eicon]";
                        }
                        else
                        {
                            returnString += " [eicon]qchug[/eicon]";
                        }
                    }
                    break;
                case "spank":
                    var spankDescriptors = new List<String> { "a sharp spank!", "a love tap to the ass.", "a smack that will leave a mark.", "a red imprint on their derriere.", "a surprisingly loving booty grope.", "an impact with enough force to cook a lesser being."};
                    returnString = initiatorProfile.displayName + " winds up and gives " + recipientProfile.displayName + " " + spankDescriptors[random.Next(spankDescriptors.Count)];
                    if (recipient == "Queen Contract")
                    {
                        returnString += " [eicon]qcass[eicon]";
                    }
                    break;
                case "bully":
                    var bullyDescriptors = new List<String> { "boolies them into submission!", "shows them whose boss!", "applies excessive force to their victim!", "spins them right round!", "establishes the pecking order!", "instills fear..." };
                    returnString = initiatorProfile.displayName + " takes " + recipientProfile.displayName + " by the collar and " + bullyDescriptors[random.Next(bullyDescriptors.Count)];
                    break;
                case "rename":
                    returnString = initiatorProfile.displayName + " has made it known that " + recipientProfile.displayName + " is to be known as " + recipientProfile.displayName + " henceforth! All occurences of their name in our records will be changed to reflect their new identity.";
                    break;
                case "monsterize":
                    identifier = Utils.AnOrA(identifier) + " " + identifier;
                    returnString = initiatorProfile.displayName + " has bolstered monsterkind by turning " + recipientProfile.displayName + " into " + identifier + "! We welcome all monsters to our Chateau, no matter what your origins. Enjoy your new life as " + identifier + "~";
                    break;
                case "petrify":                                      
                    returnString = initiatorProfile.displayName + " has petrified " + recipientProfile.displayName + " " + Utils.LocationToText(identifier, initiator, recipient) + "! They might be stuck there for quite awhile... hopefully visitors enjoy the pose they're stuck in.";
                    break;
                case "plant":
                    identifier = Utils.AnOrA(identifier) + " " + identifier;
                    returnString = initiatorProfile.displayName + " has grown the garden by turning " + recipientProfile.displayName + " into " + identifier + "! They might stay planted for quite awhile... surely the gardeners will take good care of them.";
                    break;
                case "objectify":
                    identifier = Utils.AnOrA(identifier) + " " + identifier;
                    returnString = initiatorProfile.displayName + " has made " + recipientProfile.displayName + " into some sort of " + identifier + "! Who knows what's in store for them, but they'll be stuck with their fate for quite awhile...";
                    break;
                case "dressup":
                    returnString = initiatorProfile.displayName + " has dressed up " + recipientProfile.displayName + " in " + Utils.AttireToText(identifier) + "! Do a spin for everyone, let them admire your new garb!";
                    break;
                case "feed":
                    returnString = initiatorProfile.displayName + " has fed " + recipientProfile.displayName + " some " + Utils.SubstanceToText(identifier) + "! Was it yummy? I bet it was.";
                    break;
                case "golden":
                    returnString = initiatorProfile.displayName + " breathes a sigh of relief as a golden fluid pours over " + recipientProfile.displayName + "'s " + Utils.BodypartToText(identifier) + ".";
                    break;
                case "consume":
                    returnString = initiatorProfile.displayName + " consumes " + recipientProfile.displayName + ", and they were never heard from again... or at least, it will be quite some time before they manage to escape, reform, or otherwise recover their strength.";
                    break;
                case "mark":
                    returnString = initiatorProfile.displayName + " emblazons their mark upon " + recipientProfile.displayName + "'s " + Utils.BodypartToText(identifier) + ". Wear it with pride~ " + initiatorProfile.characteristics["mark"];
                    break;
                case "employ":
                    returnString = initiatorProfile.displayName + " has given " + recipientProfile.displayName + " the esteemed position of " + Utils.JobToText(identifier) + "! Enjoy your new job everytime you !work (and don't forget you can still !volunteer to see what other jobs are like.)";
                    break;
                case "bond":
                    returnString = initiatorProfile.displayName + " is now " + recipientProfile.displayName + "'s " + Utils.BondToText(identifier, false) + ", and " + recipientProfile.displayName + " is now their " + Utils.BondToText(identifier, true) + "! May you enjoy a bright future together.";
                    break;
                case "paymentGive":
                    returnString = initiatorProfile.displayName + " has paid " + recipientProfile.displayName + " in " + identifier + "! How generous!";
                    break;
                case "paymentReceive":
                    returnString = initiatorProfile.displayName + " has received a payment from " + recipientProfile.displayName + " in " + identifier + "! How generous!";
                    break;
                default:
                    returnString = "What type of interaction was that? " + interactionType + "? For some reason, I don't recognize it... tell [user]Queen Contract[/user] to check the 'ChateauConsent' code for me if you get a chance.";
                    break;

            }
            return returnString;
        }
    }


}
