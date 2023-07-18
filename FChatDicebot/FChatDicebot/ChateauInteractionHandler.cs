using FChatDicebot.BotCommands;
using FChatDicebot.Model;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        
    }
}
