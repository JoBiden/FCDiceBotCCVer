using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauPledges : ChatBotCommand
    {
        public ChateauPledges()
        {
            Name = "pledges";
            Aliases = new string[] { };
            Category = "Interaction Support";
            ShortDescription = "View active pledges";
            LongDescription = "View all your currently active pledges, or the active pledges of a specified user, including who the interaction was pledged to and what interaction type was promised. You can later !fulfill a pledged interaction.";
            Usage = "!pledges\nor\n!pledges [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "pledge", "fulfill", "abandonpledge" };
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

            string targetUser = characterName;
            bool viewingSelf = true;

            if (rawTerms.Length > 1)
            {
                targetUser = commandController.GetUserNameFromCommandTerms(rawTerms);
                if (!string.Equals(targetUser, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    viewingSelf = false;
                }
            }

            Profile userProfile = MonDB.getProfile(targetUser);

            if (userProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notRegisteredText(), targetUser);
                return;
            }

            

            // Get all pledges where this user is the pledger or pledgee
            List<Pledge> pledgesGiven = MonDB.getPledgesByPledger(targetUser);
            List<Pledge> pledgesReceived = MonDB.getPledgesByPledgee(targetUser);

            // Filter to only active pledges
            pledgesGiven = pledgesGiven.Where(p => p.IsActive).ToList();
            pledgesReceived = pledgesReceived.Where(p => p.IsActive).ToList();

            StringBuilder message = new StringBuilder($"[b]{userProfile.displayName}'s Pledges:[/b]\n\n");
            

            // Show pledges this user has made
            if (pledgesGiven.Count > 0)
            {
                message.AppendLine (viewingSelf ? "[b]Pledges You Have Made:[/b]" : $"[b]Pledges {userProfile.displayName} Has Made:[/b]");
                foreach (var pledge in pledgesGiven)
                {
                    string pledgeeName = MonDB.getDisplayName(pledge.pledgee);
                    string timeAgo = Utils.TimeDifferenceText(pledge.pledgeTime, DateTime.UtcNow);
                    message.Append( $"  • Pledged to {Utils.interactionToVerb(pledge.interactionType, false)} {pledgeeName} ({timeAgo} ago)\n");
                }
                message.AppendLine();
            }
            else
            {
                message.AppendLine(viewingSelf ? "[b]Pledges You Have Made:[/b] None\n" : $"[b]Pledges {userProfile.displayName} Has Made:[/b] None\n");
            }

            // Show pledges others have made to this user
            if (pledgesReceived.Count > 0)
            {
                message.AppendLine(viewingSelf ? "[b]Pledges Made to You:[/b]" : $"[b]Pledges Others Have Made to {userProfile.displayName}:[/b]");
                foreach (var pledge in pledgesReceived)
                {
                    string pledgerName = MonDB.getDisplayName(pledge.pledger);
                    string timeAgo = Utils.TimeDifferenceText(pledge.pledgeTime, DateTime.UtcNow);
                    message.AppendLine( viewingSelf ? $"  • {pledgerName} pledged to {Utils.interactionToVerb(pledge.interactionType, false)} you ({timeAgo} ago)" : $"  • {pledgerName} pledged to {Utils.interactionToVerb(pledge.interactionType, false)} {userProfile.displayName} ({timeAgo} ago)");
                }
            }
            else
            {
                message.Append(viewingSelf ? "[b]Pledges Made to You:[/b] None" : $"[b]Pledges Others Have Made to {userProfile.displayName}:[/b] None (Maybe you could be the first?)");
            }
            bot.SendPrivateMessage(message.ToString(), characterName);
        }
    }
}
