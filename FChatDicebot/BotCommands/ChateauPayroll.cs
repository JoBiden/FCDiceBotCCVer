using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Drill-down for the !statistics workforce block. Two sections: current headcount per
    /// job (snapshot from <c>characteristics["job"]</c>) and lifetime duties completed per
    /// job (sum of <c>profile.jobExperience</c> across all profiles — each !work completion
    /// increments one of those entries by 1).
    /// </summary>
    public class ChateauPayroll : ChatBotCommand
    {
        public ChateauPayroll()
        {
            Name = "payroll";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "View the Chateau's current workforce and lifetime duties completed";
            LongDescription = "List the Chateau's current workforce by job, plus the lifetime tally of duties completed in each job (from every resident's !work history).";
            Usage = "!payroll";
            RelatedCommands = new string[] { "statistics", "work", "employ", "bank" };
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
            string message = BuildPayroll(MonDB.getAllProfiles());
            bot.SendPrivateMessage(message, characterName);
        }

        public static string BuildPayroll(List<Profile> profiles)
        {
            if (profiles == null) profiles = new List<Profile>();
            var employed = ChateauStatisticsSupport.CountEmployedByJob(profiles);
            var duties = ChateauStatisticsSupport.SumDutiesByJob(profiles);

            if (employed.Count == 0 && duties.Count == 0)
            {
                return "Nobody is currently employed at the Chateau, and no duties have ever been completed. The vacancies are open — use !employ to take a post.";
            }

            var sb = new System.Text.StringBuilder();
            sb.Append(ReadoutText.Title("The Chateau Payroll")).Append('\n');

            sb.Append(ReadoutText.Section("Current workforce", ReadoutDomain.Economy)).Append('\n');
            if (employed.Count == 0)
            {
                sb.Append(ReadoutText.RowIndent).Append(ReadoutText.Small("Nobody is currently employed.")).Append('\n');
            }
            else
            {
                foreach (var entry in employed.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
                {
                    string label = entry.Value == 1 ? Utils.JobToText(entry.Key) : Utils.JobToPlural(entry.Key);
                    sb.Append(ReadoutText.RowIndent)
                      .Append(ReadoutText.Row(label, ReadoutText.Num(entry.Value.ToString("N0", CultureInfo.InvariantCulture))))
                      .Append('\n');
                }
            }

            sb.Append(ReadoutText.Section("Duties completed by job", ReadoutDomain.Economy)).Append('\n');
            if (duties.Count == 0 || duties.Values.Sum() == 0)
            {
                sb.Append(ReadoutText.RowIndent).Append(ReadoutText.Small("No duties have been completed yet.")).Append('\n');
            }
            else
            {
                foreach (var entry in duties.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
                {
                    if (entry.Value <= 0) continue;
                    sb.Append(ReadoutText.RowIndent)
                      .Append(ReadoutText.Row(Utils.JobToText(entry.Key),
                          ReadoutText.Num(entry.Value.ToString("N0", CultureInfo.InvariantCulture))))
                      .Append('\n');
                }
            }
            sb.Append(ReadoutText.Footer("Employers can see their own kickbacks with !business."));
            return sb.ToString();
        }
    }
}
