using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands.Support
{
    /// <summary>
    /// Shared by-name targeting for the pending-lifecycle verbs (B5.8). Given the typed terms
    /// and a way to pull the relevant counterparty off a seat (the initiator for <c>!refuse</c>,
    /// the recipient for <c>!withdraw</c>), returns the first matching seat — by login name or
    /// display name, case-insensitively. Returns null when no name was supplied (so the caller
    /// can fall back to its bare / all / # handling).
    ///
    /// Lives in the <c>.Support</c> namespace (not <c>FChatDicebot.BotCommands</c>) so the
    /// reflection-based command loader — which <c>Activator.CreateInstance</c>s every type in
    /// that namespace — doesn't choke on this static class.
    /// </summary>
    public static class LifecycleTargeting
    {
        public static PendingCommand FindByCounterparty(
            BotCommandController commandController, string[] rawTerms, string[] terms,
            IEnumerable<PendingCommand> seats, Func<PendingCommand, string> counterpartySelector)
        {
            string tagged = commandController.GetUserNameFromCommandTerms(rawTerms);
            string candidate = !string.IsNullOrEmpty(tagged) ? tagged : (terms != null ? terms.FirstOrDefault() : null);
            if (string.IsNullOrEmpty(candidate)) return null;
            if (string.Equals(candidate, "all", StringComparison.OrdinalIgnoreCase)) return null;
            int ignored;
            if (int.TryParse(candidate, out ignored)) return null;

            return seats.FirstOrDefault(s =>
            {
                string counterparty = counterpartySelector(s);
                if (string.IsNullOrEmpty(counterparty)) return false;
                return string.Equals(counterparty, candidate, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(MonDB.getDisplayName(counterparty) ?? string.Empty, candidate, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
