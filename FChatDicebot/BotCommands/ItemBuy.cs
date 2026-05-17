using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

//NOTE: this command was used for a short time to buy items off the VC shop and credit people with chips in the casino from scraping chatlogs.
//replaced because it was confusing for people, and the !buychips command is a vast improvement
namespace FChatDicebot.BotCommands
{
    //public class ItemBuy : ChatBotCommand
    //{
        //public ItemBuy()
        //{
        //    Name = "itembuy";
        //    RequireBotAdmin = false;
        //    RequireChannelAdmin = false;
        //    RequireChannel = true;
        //    LockCategory = CommandLockCategory.ChannelScores;
        //}

        //public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        //{
        //    //special command: just listens for people referencing itembuy with velvetcuff. if it comes back successful, credit them the chips

        //    string buystring = Utils.GetFullStringOfInputs(rawTerms);

        //    if(buystring.Contains("Chips"))
        //    {
        //        bot.WaitingBuyCommands.Add(new BuyCommand() { 
        //            CharacterName = characterName,
        //            ChannelName = channel,
        //            Confirmed = false,
        //            Remove = false,
        //            ItemString = buystring,
        //            PingCount = bot.PinMessageSent
        //        });
        //        Console.WriteLine("added waitingbuycommand");
        //    }
        //}
    //}
}
