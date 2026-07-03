using FChatDicebot.Model;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    public class ChateauInteractionHandler
    {



        /// <summary>
        /// If <paramref name="toPlay"/> is tagged as a pledge fulfillment (ChateauFulfill
        /// stamps a <c>{ "pledgeId": ... }</c> BsonDocument as the first extraParameter),
        /// mark that pledge fulfilled — and honored, incrementing the pledger's
        /// <c>pledgesfulfilled</c> count, when it landed a day or more after the pledge was
        /// made. Called from the real success path in ChateauConsent.ProcessOneToOneSeat
        /// (H3) as well as this class's own processor branch above.
        /// </summary>
        public static void TryMarkPledgeFulfilled(PendingCommand toPlay)
        {
            if (toPlay.pendingInteraction.extraParameters == null || toPlay.pendingInteraction.extraParameters.Count == 0)
            {
                return;
            }

            try
            {
                var firstParam = toPlay.pendingInteraction.extraParameters[0].AsBsonDocument;
                if (!firstParam.Contains("pledgeId")) return;

                string pledgeIdStr = firstParam["pledgeId"].AsString;
                ObjectId pledgeId = ObjectId.Parse(pledgeIdStr);

                var pledge = MonDB.getPledge(pledgeId);
                if (pledge == null || !pledge.IsActive) return;

                // Mark pledge as fulfilled
                pledge.status = "fulfilled";
                pledge.fulfilledTime = DateTime.UtcNow;

                // Check if pledge was honored (fulfilled 1+ days after creation)
                TimeSpan timeSincePledge = pledge.fulfilledTime.Value - pledge.pledgeTime;
                if (timeSincePledge.TotalDays >= 1)
                {
                    pledge.pledgeHonored = true;
                    MonDB.incrementCount(pledge.pledger, "pledgesfulfilled");
                }

                MonDB.updatePledge(pledge);
            }
            catch
            {
                // extraParameters[0] wasn't a pledge-shaped BsonDocument (e.g. it's a plain
                // int payload like payment's amount) — not a pledge fulfillment, ignore.
            }
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
