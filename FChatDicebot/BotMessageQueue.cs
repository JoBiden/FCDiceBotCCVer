using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.Model;

namespace FChatDicebot
{
    public class BotMessageQueue
    {
        Queue<BotMessage> Messages;

        public BotMessageQueue()
        {
            Messages = new Queue<BotMessage>();
        }

        public BotMessage GetNextMessage()
        {
            if (Messages.Count > 0)
            {
                return Messages.Dequeue();
            }
            else
                return null;
        }

        public void AddMessage(BotMessage message)
        {
            Messages.Enqueue(message);
        }

        public BotMessage Peek()
        {
            if (Messages.Peek() == null)
            {
                Console.WriteLine("Removed null message from queue");
                Utils.AddToLog("Removed null message from queue", null);
                Messages = (Queue<BotMessage>)Messages.Where(a => a != null);
            }

            return Messages.Peek();
        }

        public bool HasMessages()
        {
            return Messages.Count > 0;
        }

        public void RemoveAllChannelJoins()
        {
            List<BotMessage> newList = new List<BotMessage>();
            while (Messages.Count > 0)
            {
                BotMessage botMessage = Messages.Dequeue();
                if (botMessage.messageType != BotMessageFactory.JCH)
                    newList.Add(botMessage);
            }
            Messages = new Queue<BotMessage>();
            if (newList.Count > 0)
            {
                foreach (BotMessage msg in newList)
                {
                    Messages.Enqueue(msg);
                }
            }
        }

        //TODO: AddUrgentMessage to move a message to the front of the queue on add
        //public void AddUrgentMessage(BotMessage message)
        //{
        //    Messages.Enqueue(message);
        //}
    }
}
