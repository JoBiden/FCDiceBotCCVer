using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauViewPledges : ChatBotCommand
    {
        public ChateauViewPledges()
        {
            Name = "viewpledges";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            Profile userProfile = MonDB.getProfile(characterName);

            if (userProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notRegisteredText(), characterName);
                return;
            }

            // Get all pledges where this user is the pledger or pledgee
            List<Pledge> pledgesGiven = MonDB.getPledgesByPledger(characterName);
            List<Pledge> pledgesReceived = MonDB.getPledgesByPledgee(characterName);

            // Filter to only active pledges
            pledgesGiven = pledgesGiven.Where(p => p.IsActive).ToList();
            pledgesReceived = pledgesReceived.Where(p => p.IsActive).ToList();

            string message = $"[b]Pledges for {userProfile.displayName}:[/b]\n\n";

            // Show pledges this user has made
            if (pledgesGiven.Count > 0)
            {
                message += "[b]Pledges You Have Made:[/b]\n";
                foreach (var pledge in pledgesGiven)
                {
                    string pledgeeName = MonDB.getDisplayName(pledge.pledgee);
                    string timeAgo = Utils.TimeDifferenceText(pledge.pledgeTime, DateTime.UtcNow);
                    message += $"  • Pledged to {pledge.interactionType} {pledgeeName} ({timeAgo} ago)\n";
                }
                message += "\n";
            }
            else
            {
                message += "[b]Pledges You Have Made:[/b] None\n\n";
            }

            // Show pledges others have made to this user
            if (pledgesReceived.Count > 0)
            {
                message += "[b]Pledges Made To You:[/b]\n";
                foreach (var pledge in pledgesReceived)
                {
                    string pledgerName = MonDB.getDisplayName(pledge.pledger);
                    string timeAgo = Utils.TimeDifferenceText(pledge.pledgeTime, DateTime.UtcNow);
                    message += $"  • {pledgerName} pledged to {pledge.interactionType} you ({timeAgo} ago)\n";
                }
            }
            else
            {
                message += "[b]Pledges Made To You:[/b] None";
            }

            bot.SendPrivateMessage(message, characterName);
        }
    }
}
