using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class JoinChannel : ChatBotCommand
    {
        public JoinChannel()
        {
            Name = "joinchannel";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string errorString = "";

            int fetchedCount = rawTerms.Count(a => a.StartsWith("FETCHEDMODSfrom_"));

            string channelKey = fetchedCount > 0? address.GetChannelKey().ToLower() : Utils.GetChannelIdFromInputs(rawTerms, out errorString).ToLower();

            if(!string.IsNullOrEmpty(errorString))
            {
                if (!commandController.MessageCameFromChannel(address))
                {
                    bot.SendPrivateMessage(errorString, address);
                }
                else
                {
                    bot.SendMessageInChannel(errorString, address);
                }
                return;
            }

            //adding in the requirement of being a mod in the channel the bot is being added to
            if (fetchedCount == 0 && command.ops == null)
            {
                string[] newCommandTermsMarked = new string[command.rawTerms == null ? 2 : command.rawTerms.Length + 2];
                if(command.rawTerms != null && command.rawTerms.Length > 0)
                {
                    for(int i = 0; i < command.rawTerms.Length; i++)
                    {
                        newCommandTermsMarked[i] = command.rawTerms[i];
                    }

                }

                Console.WriteLine("Attempting fetch channel mods for " + channelKey);

                newCommandTermsMarked[newCommandTermsMarked.Length - 1] = "FETCHEDMODSfrom_" + channelKey;
                if(string.IsNullOrEmpty(address.GetChannelKey()))
                    newCommandTermsMarked[newCommandTermsMarked.Length - 2] = "fromCharacter";
                else
                    newCommandTermsMarked[newCommandTermsMarked.Length - 2] = "fromChannel_" + address.GetChannelKey().ToLower();

                command.rawTerms = newCommandTermsMarked;
                command.channel = channelKey;
                bot.RequestChannelOpListAndQueueFurtherRequest(command);
            }
            else if (fetchedCount > 0)
            {
                string sendMessage = "";

                string tackedOn = rawTerms.FirstOrDefault(a => a.StartsWith("FETCHEDMODSfrom_"));
                string joinedChannel = tackedOn.Replace("FETCHEDMODSfrom_", "");
                bool fromCharacter = rawTerms.Contains("fromCharacter");
                string originalChannel = rawTerms.FirstOrDefault(a => a.StartsWith("fromChannel_"));
                if(!string.IsNullOrEmpty(originalChannel))
                    originalChannel = originalChannel.Replace("fromChannel_", "");

                if (command.ops == null)
                {
                    sendMessage = "Error: Channel ops could not be requested for channel: " + channelKey;
                }
                else if(!command.ops.Contains(address.character) && !Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character))
                {
                    sendMessage = "Failed: You must be a channel mod in order to request I join: " + channelKey;
                }
                else
                {
                    sendMessage = "Attempting to join channel: " + channelKey + "\n[i]Remember, if you like having me in the channel use[/i] !setstartingchannel [i]so I'll reconnect whenever I come online![/i]";

                    bot.JoinChannel(new MessageAddress() { channel = channelKey, guild = null, character = null }, false);
                }

                MessageAddress originalAddress = new MessageAddress() { channel = originalChannel, character = address.character, guild = null };
                //send to character if there is no origin channel for this command
                if (fromCharacter)//!commandController.MessageCameFromChannel(originalAddress))
                {
                    bot.SendPrivateMessage(sendMessage, originalAddress);
                }
                else
                {
                    bot.SendMessageInChannel(sendMessage, originalAddress);
                }
            }
            else
            {
                bot.SendPrivateMessage("Error: fetch moderators failed.", address);
            }

        }
    }
}
