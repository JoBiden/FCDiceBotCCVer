using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string errorString = "";

            int fetchedCount = rawTerms.Count(a => a.StartsWith("FETCHEDMODSfrom_"));

            string channelId = fetchedCount > 0? channel : Utils.GetChannelIdFromInputs(rawTerms, out errorString);

            if(!string.IsNullOrEmpty(errorString))
            {
                if (!commandController.MessageCameFromChannel(channel))
                {
                    bot.SendPrivateMessage(errorString, characterName);
                }
                else
                {
                    bot.SendMessageInChannel(errorString, channel);
                }
                return;
            }

            //adding in the requirement of being a mod in the channel the bot is being added to
            if (fetchedCount == 0 &&command.ops == null)
            {
                string[] newCommandTermsMarked = new string[command.rawTerms == null ? 1 : command.rawTerms.Length + 1];
                if(command.rawTerms != null && command.rawTerms.Length > 0)
                {
                    for(int i = 0; i < command.rawTerms.Length; i++)
                    {
                        newCommandTermsMarked[i] = command.rawTerms[i];
                    }

                }

                Console.WriteLine("Attempting fetch channel mods for " + channelId);

                newCommandTermsMarked[newCommandTermsMarked.Length - 1] = "FETCHEDMODSfrom_" + channel;

                command.rawTerms = newCommandTermsMarked;
                command.channel = channelId;
                bot.RequestChannelOpListAndQueueFurtherRequest(command);
            }
            else if (fetchedCount > 0)
            {
                string sendMessage = "";

                string tackedOn = rawTerms.FirstOrDefault(a => a.StartsWith("FETCHEDMODSfrom_"));
                string originalChannel = tackedOn.Replace("FETCHEDMODSfrom_", "");
            
                if(command.ops == null)
                {
                    sendMessage = "Error: Channel ops could not be requested for channel: " + channelId;
                }
                else if(!command.ops.Contains(characterName) && !Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, characterName))
                {
                    sendMessage = "Failed: You must be a channel mod in order to request I join: " + channelId;
                }
                else
                {
                    sendMessage = "Attempting to join channel: " + channelId + "\n[i]Remember, if you like having me in the channel use[/i] !setstartingchannel [i]so I'll reconnect whenever I come online![/i]";

                    bot.JoinChannel(channelId);
                }

                //send to character if there is no origin channel for this command
                if (!commandController.MessageCameFromChannel(originalChannel))
                {
                    bot.SendPrivateMessage(sendMessage, characterName);
                }
                else
                {
                    bot.SendMessageInChannel(sendMessage, originalChannel);
                }
            }
            else
            {
                bot.SendPrivateMessage("Error: fetch moderators failed.", characterName);
            }
            
        }
    }
}
