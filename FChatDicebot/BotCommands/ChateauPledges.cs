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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;

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

            StringBuilder message = new StringBuilder(
                ReadoutText.Title("Pledges of " + userProfile.displayName) + "\n");

            // Show pledges this user has made
            string givenHeader = viewingSelf ? "Pledges made" : "Pledges " + userProfile.displayName + " has made";
            if (pledgesGiven.Count > 0)
            {
                message.AppendLine(ReadoutText.Section(givenHeader, ReadoutDomain.Relationship));
                foreach (var pledge in pledgesGiven)
                {
                    string pledgeeName = MonDB.getDisplayName(pledge.pledgee);
                    string timeAgo = Utils.TimeDifferenceText(pledge.pledgeTime, DateTime.UtcNow);
                    message.Append(ReadoutText.RowIndent)
                        .Append($"Pledged to {Utils.interactionToVerb(pledge.interactionType, false)} {pledgeeName} ")
                        .AppendLine(ReadoutText.Small($"({timeAgo} ago)"));
                }
            }
            else
            {
                // Small print rather than body weight: an empty slot shouldn't read as loudly
                // as a real pledge.
                message.AppendLine(ReadoutText.Section(givenHeader, ReadoutDomain.Relationship) + " " + ReadoutText.None());
            }

            // Show pledges others have made to this user
            string receivedHeader = viewingSelf ? "Pledges made to you" : "Pledges made to " + userProfile.displayName;
            if (pledgesReceived.Count > 0)
            {
                message.AppendLine(ReadoutText.Section(receivedHeader, ReadoutDomain.Relationship));
                foreach (var pledge in pledgesReceived)
                {
                    string pledgerName = MonDB.getDisplayName(pledge.pledger);
                    string timeAgo = Utils.TimeDifferenceText(pledge.pledgeTime, DateTime.UtcNow);
                    string who = viewingSelf ? "you" : userProfile.displayName;
                    message.Append(ReadoutText.RowIndent)
                        .Append($"{pledgerName} pledged to {Utils.interactionToVerb(pledge.interactionType, false)} {who} ")
                        .AppendLine(ReadoutText.Small($"({timeAgo} ago)"));
                }
            }
            else
            {
                message.AppendLine(ReadoutText.Section(receivedHeader, ReadoutDomain.Relationship) + " " + ReadoutText.None()
                    + (viewingSelf ? "" : " " + ReadoutText.Small("(Maybe you could be the first?)")));
            }

            message.Append(ReadoutText.Footer("Use !fulfill to make good on a pledge, or !abandonpledge to let one go."));
            bot.SendPrivateMessage(message.ToString(), characterName);
        }
    }
}
