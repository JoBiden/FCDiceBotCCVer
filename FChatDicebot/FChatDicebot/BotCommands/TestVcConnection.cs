using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    public class TestVcConnection : ChatBotCommand
    {
        public TestVcConnection()
        {
            Name = "testvcconnection";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            //perform authorization
            bool success = bot.VelvetcuffConnection.GetNewVelvetcuffOauthToken();
            bot.SendMessageInChannel("Authorization completed, token retrieved... " + (bot.VelvetcuffConnection.CurrentOAuthVcKey == null? "null" : bot.VelvetcuffConnection.CurrentOAuthVcKey.Take(10) ) + "... " + success, channel);
            
            //perform create transaction
            success = bot.VelvetcuffConnection.CreateNewVcTransaction(1000, "Platinum Luxury", DiceBot.DiceBotCharacter, true, "Cash out for 1000 from casino");
            bot.SendMessageInChannel("Transaction created... " +  bot.VelvetcuffConnection.CurrentPaymentId + "... " + success, channel);
            
            //perform check transaction
            int status = -1;
            success = bot.VelvetcuffConnection.CheckVcTransaction(bot.VelvetcuffConnection.CurrentPaymentId, out status);
            bot.SendMessageInChannel("Transaction checked... " + status + "... " + success, channel);

            //perform delete
            success = bot.VelvetcuffConnection.DeleteVcTransaction(bot.VelvetcuffConnection.CurrentPaymentId);
            bot.SendMessageInChannel("Transaction deleted... " + bot.VelvetcuffConnection.CurrentPaymentId + "... " + success, channel);
        }
    }
}
