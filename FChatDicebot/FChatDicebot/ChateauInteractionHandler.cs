using FChatDicebot.Model;
using System;
using System.Collections.Generic;
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
            
            // NEW: Try to use the new processor system first
            var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor(toPlay.pendingInteraction.type);
            if (processor != null)
            {
                return processor.ProcessInteraction(toPlay);
            }
            
            // FALLBACK: Use old switch statement for interactions not yet migrated
            // All current interactions have been migrated to the new processor system!
            // This switch is kept as a fallback for any future legacy interactions
            switch (toPlay.pendingInteraction.type)
            {
                // All interactions have been migrated to processors:
                // - Casual: kiss, cuddle, handhold, spank, bully
                // - Involved: feed, golden, dressup
                // - Commitment: mark, entitle
                // - Consequence: rename, monsterize, petrify, plant, objectify, consume, employ, bond
                // - Transaction: paymentGive, paymentReceive

                default:
                    returnString = "NoInteraction";
                    break;
            };
            if (returnString != "NoInteraction")
            {
                MonDB.removePendingInteraction(toPlay.Id);
            }
            return returnString;
        }

        public static string noJobText()
        {
            return "It doesn't look like you have a job to work yet! Feel free to ask around and see who is hiring, or self employ by using !employ and targeting yourself.";
        }

        public static string notFoundText(string targetUser)
        {
            return "The user " + targetUser + " could not be found. Either they're not registered, or you're looking for someone else (check your case sensitive spelling, and make sure you have a [user] tag! Pro tip: you can start typing the name, then use Tab to auto complete with the [user] tag.)";
        }

        public static string notRegisteredText()
        {
            return "It appears as if you aren't registered with the Chateau yet! We have a firm policy of consent, and that means you should know what you're signing up for. If you are registered, then others will be allowed to [i]Interact[/i] with you using commands (so long as you explicitly [i]!consent[/i]), read a [i]!dossier[/i] of your character's current state in the Chateau, and see stored [i]Interactions[/i] that involve your character. Registering also means that you will do your best to follow the rules, be a respectful member of the community, and will abide by our Moderators decisions in times of dispute.\n\nIf that all sounds reasonable to you, then use [b]!joinchateau[/b] to register. Thank you for your interest either way!";
        }

        internal static string dutyFromPreviousDayText()
        {
            return "Looks like we didn't hear back from you about your work the other day. Our policy is to assume you got bored and wandered off, forfeiting any potential rewards. Make sure you complete the entirety of your work before the end of the day!";
        }

        internal static string markNotSetText()
        {
            return "It doesn't appear as if you have set a mark yet! Please make sure that you have used [b]!setmark[/b] so we know what to display when you !mark someone. \n\nYou can use any eicon you like, and don't worry - you can always change it later. We'll update our records to always have your most recently !setmark on display. If you don't yet have an eicon for your mark, but would like to have a mark interaction now, we suggest using the [noparse][eicon]blank[/eicon][/noparse] eicon.";
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
