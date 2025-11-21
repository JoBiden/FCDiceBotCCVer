using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using System.ComponentModel;

namespace FChatDicebot.BotCommands
{
    public class ChateauAdminRename : ChatBotCommand
    {
        public ChateauAdminRename()
        {
            Name = "namechange";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string targetProfile = commandController.GetUserNameFromCommandTerms(rawTerms);
            string newUserName = commandController.GetQuotedTextFromCommandTerms(rawTerms);
            Profile oldProfile = MonDB.getProfile(targetProfile);
            Boolean valid = true;
            if (oldProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(targetProfile), characterName);
                valid = false;
            } 
            if (valid)
            {
                string message = string.Empty;

                List<Interaction> initiatedInteractionList = MonDB.getInteractionsByInitiator(targetProfile);
                List<Interaction> receivedInteractionList = MonDB.getInteractionsByRecipient(targetProfile);

                Profile partnerProfile = null;

                foreach (Interaction interaction in initiatedInteractionList)
                {
                    
                    interaction.initiator = newUserName;
                    MonDB.setInteraction(interaction);

                    partnerProfile = null;
                    switch (interaction.type) 
                    {
                        case "bond":
                            partnerProfile = profileListUpdate(interaction.recipient);
                            message = message + "Adjusting initiated bond with " + partnerProfile.userName + "... ";
                            break;
                        case "employ":
                            partnerProfile = MonDB.getProfile(interaction.recipient);
                            partnerProfile.characteristics["employer"] = newUserName;
                            message = message + "Adjusting initiated employ with " + partnerProfile.userName + "... ";
                            break;
                        case "mark":
                            partnerProfile = profileListUpdate(interaction.recipient);
                            message = message + "Adjusting initiated mark with " + partnerProfile.userName + "... ";
                            break;
                    }
                    if (partnerProfile != null)
                    {
                        MonDB.setProfile(partnerProfile.userName, partnerProfile);
                        message = message + "Done!\n";
                    }
                }

                foreach (Interaction interaction in receivedInteractionList)
                {
                    interaction.recipient = newUserName;
                    MonDB.setInteraction(interaction);

                    partnerProfile = null;
                    switch (interaction.type)
                    {
                        case "bond":
                            partnerProfile = profileListUpdate(interaction.initiator);
                            message = message + "Adjusting received bond with " + partnerProfile.userName + "... ";
                            break;
                        case "mark":
                            partnerProfile = profileListUpdate(interaction.initiator);
                            message = message + "Adjusting received mark with " + partnerProfile.userName + "... ";
                            break;
                    }
                    if (partnerProfile != null)
                    {
                        MonDB.setProfile(partnerProfile.userName, partnerProfile);
                        message = message + "Done!\n";
                    }
                }

                oldProfile.userName = newUserName;
                oldProfile.displayName = newUserName;
                message = message + targetProfile + " username has been changed to " + newUserName;
                bot.SendPrivateMessage(message, characterName);
            }

            Profile profileListUpdate(string profileName)
            {
                Profile partnerProfile = MonDB.getProfile(profileName);
                foreach (KeyValuePair<string, List<string>> list in partnerProfile.lists)
                {
                    if (list.Value.Contains(targetProfile))
                    {
                        list.Value.Remove(targetProfile);
                        list.Value.Add(newUserName);
                    }
                }

                return partnerProfile;
            }
        }
    }
}
