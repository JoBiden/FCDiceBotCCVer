using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.Model;

namespace FChatDicebot
{
    public class ChannelsAuditSession
    {
        List<MessageAddress> EntireJoinQueue;
        List<MessageAddress> Messages;
        List<MessageAddress> SuccessfulJoin;

        int JoinResults;

        public ChannelsAuditSession(List<MessageAddress> channelsList)
        {
            EntireJoinQueue = channelsList;
            JoinResults = 0;

            SuccessfulJoin = new List<MessageAddress>();

            Messages = new List<MessageAddress>();

        }

        public void AttemptToJoin(MessageAddress message)
        {
            Messages.Add(message);
        }

        public MessageAddress GetLastJoined()
        {
            if (Messages.Count > 0)
            {
                return Messages[Messages.Count -1];
            }
            else
                return null;
        }

        public List<MessageAddress> ChannelJoinResult(MessageAddress message, bool success)
        {
            List<MessageAddress> returnList = null;
            //if (success && SuccessfulJoin.Count(a => a.channel.ToLower() == message.channel.ToLower()) > 0)
            //    return returnList; //duplicate COL attempts don't need to count... or do they?

            JoinResults++;
            if(success)
                SuccessfulJoin.Add(message);
            //unsuccessful joins don't have a message, since the server ERR doesn't return channel id

            if (JoinResults >= EntireJoinQueue.Count)
            {
                //audit and remove old joins

                List<MessageAddress> channelDeletable = new List<MessageAddress>();
                foreach (MessageAddress joinAttempt in EntireJoinQueue)
                {
                    if (SuccessfulJoin.Count(a => a.channel.ToLower() == joinAttempt.channel.ToLower()) > 0)
                    {
                    }
                    else
                    {
                        //add to delete channels list
                        channelDeletable.Add(joinAttempt);
                    }
                }
                returnList = channelDeletable;
            }
            return returnList;
        }

        public bool HasMessages()
        {
            return Messages.Count > 0;
        }

        //TODO: AddUrgentMessage to move a message to the front of the queue on add
        //public void AddUrgentMessage(BotMessage message)
        //{
        //    Messages.Enqueue(message);
        //}
    }
}
