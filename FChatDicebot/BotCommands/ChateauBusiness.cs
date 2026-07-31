using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !business: an employer's read-only view of the MANOR kickbacks their employees have
    /// earned them, broken down by employee and by currency (from Profile.employeeEarnings,
    /// which !work increments). Not an interaction — no consent, cooldown, or DB-layer change;
    /// it reads only the invoker's own profile, in the mold of !payroll / !economics.
    /// </summary>
    public class ChateauBusiness : ChatBotCommand
    {
        public ChateauBusiness()
        {
            Name = "business";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "See what your employees have earned you";
            LongDescription = "View the profits you've earned from the hard work of your employees, broken down by employee and by currency. You earn approximately 25% of what your employee makes, on top of what they would make if self employed.";
            Usage = "!business";
            RelatedCommands = new string[] { "employ", "work", "bank", "payroll" };
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
            Profile userProfile = MonDB.getProfile(characterName);
            string message = BuildBusiness(userProfile, MonDB.getDisplayName);
            bot.SendPrivateMessage(message, characterName);
        }

        public const string EmptyStateMessage =
            "None of your employees have earned you anything yet. Use !employ to put someone on the payroll, and they'll start sending MANOR kickbacks your way every time they !work.";

        /// <summary>
        /// Renders an employer's employeeEarnings ledger. <paramref name="resolveDisplayName"/>
        /// maps an employee userName to a current display name (e.g. MonDB.getDisplayName);
        /// when it returns null/empty (profile gone) the stored userName key is shown instead.
        /// Pure aside from the resolver, so it can be unit-tested without a bot or DB.
        /// </summary>
        public static string BuildBusiness(Profile profile, Func<string, string> resolveDisplayName)
        {
            Dictionary<string, Dictionary<string, int>> ledger = profile?.employeeEarnings;

            // Aggregate per employee, pruning any non-positive currency entries.
            var rows = new List<EmployeeRow>();
            if (ledger != null)
            {
                foreach (var employeeEntry in ledger)
                {
                    if (employeeEntry.Value == null)
                        continue;

                    var positive = employeeEntry.Value
                        .Where(c => c.Value > 0)
                        .OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (positive.Count == 0)
                        continue;

                    string displayName = resolveDisplayName != null ? resolveDisplayName(employeeEntry.Key) : null;
                    if (string.IsNullOrEmpty(displayName))
                        displayName = employeeEntry.Key;

                    rows.Add(new EmployeeRow
                    {
                        DisplayName = displayName,
                        Currencies = positive,
                        Total = positive.Sum(c => c.Value)
                    });
                }
            }

            if (rows.Count == 0)
                return EmptyStateMessage;

            // Highest-earning employees first, then alphabetical for stable ties.
            rows = rows
                .OrderByDescending(r => r.Total)
                .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var grandTotals = new Dictionary<string, int>();
            var sb = new StringBuilder();
            sb.Append(ReadoutText.Title("Business of " + profile.displayName)).Append('\n');
            sb.Append("Here's what your employees have earned you:\n");
            sb.Append(ReadoutText.Section("Earnings by employee", ReadoutDomain.Economy)).Append('\n');
            foreach (EmployeeRow row in rows)
            {
                sb.Append(ReadoutText.RowIndent)
                  .Append(ReadoutText.Row(row.DisplayName, string.Join(ReadoutText.InlineSeparator,
                      row.Currencies.Select(c => Utils.Capitalize(c.Key) + ": " + ReadoutText.Num(c.Value)))))
                  .Append('\n');

                foreach (var c in row.Currencies)
                {
                    if (grandTotals.ContainsKey(c.Key))
                        grandTotals[c.Key] += c.Value;
                    else
                        grandTotals[c.Key] = c.Value;
                }
            }

            string totalChips = string.Join(ReadoutText.InlineSeparator, grandTotals
                .OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .Select(c => Utils.Capitalize(c.Key) + ": " + ReadoutText.Num(c.Value)));
            sb.Append(ReadoutText.Section("Total from all employees", ReadoutDomain.Economy))
              .Append(' ').Append(totalChips).Append('\n');
            sb.Append(ReadoutText.Footer("See !payroll for the Chateau-wide workforce, or !bank for your own account."));

            return sb.ToString();
        }

        private class EmployeeRow
        {
            public string DisplayName { get; set; }
            public List<KeyValuePair<string, int>> Currencies { get; set; }
            public int Total { get; set; }
        }
    }
}
