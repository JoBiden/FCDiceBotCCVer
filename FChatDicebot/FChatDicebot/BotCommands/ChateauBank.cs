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
using System.Windows.Markup;
using ZstdSharp;

namespace FChatDicebot.BotCommands
{
    public class ChateauBank : ChatBotCommand
    {
        public ChateauBank()
        {
            Name = "bank";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Check your or another character's currency balance";
            LongDescription = "View the currency balance stored in the Chateau's vault for yourself or another character. Shows employment status and encourages earning through !work or !volunteer if no currencies are present.";
            Usage = "!bank\nor\n!bank [user]CharacterName[/user]";
            RelatedCommands = new string[] { "work", "volunteer", "employ", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string targetUser;
            bool ownAccount = false;
            bool selfEmployed = false;

            if (terms.Length < 1)
            {
                targetUser = characterName;
                ownAccount = true;
            }
            else
            {
                targetUser = commandController.GetUserNameFromCommandTerms(rawTerms);
            }
            string bankText = string.Empty;
            Profile profile = MonDB.getProfile(targetUser);
            if (profile == null)
            {
                bankText = "Resident not found. Either they aren't registered, or you're looking for the wrong person (check your spelling, and be sure to use a [user] tag!)";
            }
            else
            {
                string employerDisplayName = string.Empty;
                if (profile.characteristics.ContainsKey("employer"))
                {
                    employerDisplayName = MonDB.getDisplayName(profile.characteristics["employer"]);
                    if (profile.characteristics["employer"] == targetUser)
                    {
                        selfEmployed = true;
                    }
                }
                if (profile.currencies == null || profile.currencies.Count == 0)
                {
                    // No currencies - build appropriate message based on situation
                    bankText = ownAccount
                        ? "It doesn't look like you're storing any currency in our vault yet."
                        : $"It doesn't look like {profile.displayName} is storing any currency in our vault.";

                    bankText += " That doesn't mean destitution, but we can only facilitate transactions and report account balances of currencies stored here. Our records show ";

                    // Employment status guidance
                    if (profile.characteristics.ContainsKey("job"))
                    {
                        string jobText = Utils.JobToText(profile.characteristics["job"]);
                        string anOrAJobText = Utils.AnOrA(jobText) + " " + jobText;
                        // Has a job - encourage working
                        if (selfEmployed)
                        {
                            bankText += ownAccount
                                ? $"you're self-employed as {anOrAJobText}, so get out there and !work to start earning currency!"
                                : $"{profile.displayName} is self-employed as {anOrAJobText}. They can use !work to start earning currency!";
                        }
                        else
                        {
                            bankText += ownAccount
                                ? $"you're currently employed by {employerDisplayName} as {anOrAJobText}. Go !work for them and earn some currency!"
                                : $"{profile.displayName} is employed by {employerDisplayName} as {anOrAJobText}. Encourage them to !work for their boss and earn some currency!";
                        }
                    }
                    else
                    {
                        // No job - encourage employment
                        bankText += ownAccount
                            ? "you don't currently have a job. Use !employ to self-employ, or find someone to !employ you, so you can go !work and earn some currency!"
                            : $"{profile.displayName} doesn't currently have a job. Maybe you could !employ them so they can !work and earn some currency! [sub](Don't worry, you won't be on the hook for payment! Most jobs involve work that pays for itself, and any additional resident payroll is covered by the Chateau's 'Money Allocation for Necessary Operational Roles', or the MANOR for short!)[/sub]";
                    }

                }
                else
                {
                    // Has currencies

                    if (profile.characteristics.ContainsKey("job")) //has currencies and a job
                    {
                        bankText = "Our records show ";
                        bankText += selfEmployed
                            ? (ownAccount ? "you are self-employed" : $"{profile.displayName} is self-employed")
                            : (ownAccount ? $"you are employed by {employerDisplayName}" : $"{profile.displayName} is employed by {employerDisplayName}");
                        bankText += " as " + Utils.AnOrA(Utils.JobToText(profile.characteristics["job"])) + " " + Utils.JobToText(profile.characteristics["job"]) + ". ";
                    }
                    bankText += ownAccount
                        ? "Through your hard !work and !volunteer efforts, as well as other exploits, you have amassed:\n"
                        : $"{profile.displayName}'s account contains:\n";

                    var alphabatizedCurrencyList = profile.currencies.OrderBy(kv => kv.Key).ToList();
                    foreach (var currency in alphabatizedCurrencyList)
                    {
                        bankText += $"[b]{currency.Value} {currency.Key}[/b] | ";
                    }
                    bankText = bankText.TrimEnd(' ', '|');
                }

            }
            bot.SendPrivateMessage(bankText, characterName);
        }
    }
}
