using FChatDicebot.Model;
using System;
using System.Linq;

namespace FChatDicebot
{
    public class ChateauInteractionHandler 
    {

        public static string addInteraction(PendingCommand toPlay)
        {
            string initiator = toPlay.pendingInteraction.initiator;
            string recipient = toPlay.pendingInteraction.recipient;
            string returnString = "NoInteraction";
            switch (toPlay.pendingInteraction.type)
            {
                case "kiss":
                    MonDB.incrementCount(initiator, "kiss");
                    MonDB.incrementCount(recipient, "kiss");
                    returnString = "kiss";
                    break;
                case "cuddle":
                    MonDB.incrementCount(initiator, "cuddle");
                    MonDB.incrementCount(recipient, "cuddle");
                    returnString = "cuddle";
                    break;
                case "handhold":
                    MonDB.incrementCount(initiator, "handhold");
                    MonDB.incrementCount(recipient, "handhold");
                    returnString = "handhold";
                    break;
                case "spank":
                    MonDB.incrementCount(initiator, "spankgive");
                    MonDB.incrementCount(recipient, "spanktake");
                    returnString = "spank";
                    break;
                case "bully":
                    MonDB.incrementCount(initiator, "bullygive");
                    MonDB.incrementCount(recipient, "bullytake");
                    returnString = "spank";
                    break;
                case "rename":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile renameProfile = MonDB.getProfile(recipient);
                    Timer renameTimer = new Timer();
                    renameTimer.timerEnd = DateTime.UtcNow.AddDays(7); 
                    renameProfile.displayName = "[s]" + recipient + "[/s] " + toPlay.pendingInteraction.extraParameters.FirstOrDefault().AsString;
                    renameProfile.timers["rename"] = renameTimer;
                    MonDB.setProfile(recipient, renameProfile);
                    returnString = "rename";
                    break;
                case "monsterize":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile monsterizeProfile = MonDB.getProfile(recipient);
                    Timer monsterizeTimer = new Timer();
                    monsterizeTimer.timerEnd = DateTime.UtcNow.AddDays(7);
                    monsterizeProfile.characteristics["monster"] = toPlay.pendingInteraction.identifier;
                    monsterizeProfile.timers["monsterize"] = monsterizeTimer;
                    MonDB.setProfile(recipient, monsterizeProfile);
                    returnString = "monsterize";
                    break;
                case "petrify":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile petrifyProfile = MonDB.getProfile(recipient);
                    Timer petrifyTimer = new Timer();
                    petrifyTimer.timerEnd = DateTime.UtcNow.AddDays(1);
                    petrifyProfile.characteristics["petrifylocation"] = toPlay.pendingInteraction.identifier;
                    petrifyProfile.timers["petrify"] = petrifyTimer;
                    MonDB.setProfile(recipient, petrifyProfile);
                    returnString = "petrify";
                    break;
                case "plant":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile plantProfile = MonDB.getProfile(recipient);
                    Timer plantTimer = new Timer();
                    plantTimer.timerEnd = DateTime.UtcNow.AddDays(1);
                    plantProfile.characteristics["plantType"] = toPlay.pendingInteraction.identifier;
                    plantProfile.timers["plant"] = plantTimer;
                    MonDB.setProfile(recipient, plantProfile);
                    returnString = "plant";
                    break;
                case "objectify":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile objectifyProfile = MonDB.getProfile(recipient);
                    Timer objectifyTimer = new Timer();
                    objectifyTimer.timerEnd = DateTime.UtcNow.AddDays(1);
                    objectifyProfile.characteristics["objectType"] = toPlay.pendingInteraction.identifier;
                    objectifyProfile.timers["objectify"] = objectifyTimer;
                    MonDB.setProfile(recipient, objectifyProfile);
                    returnString = "objectify";
                    break;
                case "consume":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile consumeProfile = MonDB.getProfile(recipient);
                    Timer consumeTimer = new Timer();
                    consumeTimer.timerEnd = DateTime.UtcNow.AddDays(1);
                    consumeProfile.timers["consume"] = consumeTimer;
                    MonDB.setProfile(recipient, consumeProfile);
                    returnString = "consume";
                    break;
                case "dressup":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile dressupProfile = MonDB.getProfile(recipient);
                    dressupProfile.characteristics["attire"] = toPlay.pendingInteraction.identifier;
                    MonDB.setProfile(recipient, dressupProfile);
                    returnString = "dressup";
                    break;
                case "feed":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile feedProfile = MonDB.getProfile(recipient);
                    MonDB.setProfile(recipient, feedProfile);
                    returnString = "feed";
                    break;
                case "golden":
                    MonDB.addInteraction(toPlay.pendingInteraction);
                    Profile goldenProfile = MonDB.getProfile(recipient);
                    MonDB.setProfile(recipient, goldenProfile);
                    returnString = "golden";
                    break;
                default:
                    returnString = "NoInteraction";
                    break;
            };
            if (returnString != "NoInteraction")
            {
                MonDB.removePending(toPlay.Id);
            }
            return returnString;
        }

        public static string notFoundText(string targetUser)
        {
            return "The user " + targetUser + " could not be found. Either they're not registered, or you're looking for someone else (check your spelling, and make sure you have a [user] tag! Pro tip: you can start typing the name, then use Tab to auto complete with the [user] tag.)";
        }

        public static string notRegisteredText()
        {
            return "It appears as if you aren't registered with the Chateau yet! We have a firm policy of consent, and that means you should know what you're signing up for. If you are registered, then others will be allowed to [i]Interact[/i] with you using commands (so long as you explicitly [i]!consent[/i]), read a [i]!dossier[/i] of your character's current state in the Chateau, and see stored [i]Interactions[/i] that involve your character. Registering also means that you will do your best to follow the rules, be a respectful member of the community, and will abide by our Moderators decisions in times of dispute.\n\nIf that all sounds reasonable to you, then use [b]!joinchateau[/b] to register. Thank you for your interest either way!";
        }

        internal static string needsQuotedText()
        {
            return "That command supports spaces in the input, so for the sake of safety, please put it in quotes. For instance, [noparse]!command [user]Target User[/user] \"Input with spaces\" otherInput[/noparse]";
        }

        internal static string typeNotFoundText(string category)
        {
            return "That command requires an identifier of type [b]" + category + "[/b], which we couldn't find in your input. You can use \"!category " + category + "\" to get a full list of identifiers that will work for that command. Mind your spelling!";
        }
    }
}
