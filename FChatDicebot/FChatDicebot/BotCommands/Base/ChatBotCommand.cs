using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.BotCommands.Base
{
    public abstract class ChatBotCommand
    {
        public string Name;
        public bool RequireBotAdmin;
        public bool RequireChannelAdmin;
        public bool RequireChannel;

        public CommandLockCategory LockCategory;

        public virtual void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {

        }
    }
}
