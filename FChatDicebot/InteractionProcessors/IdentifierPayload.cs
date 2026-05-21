namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Shared encoder/decoder for the <c>"{head}|{tail}"</c> payload pattern that several
    /// processors stamp onto <see cref="Model.Interaction.identifier"/>.
    ///
    /// The <see cref="Model.Interaction"/> model has no dedicated per-call slot for things
    /// like "applied magnitude" / "rolled quantity" / "post-bump daily count" — but a
    /// processor needs to carry that number from <c>ProcessInteraction</c> into the
    /// follow-up <c>GetCompletionMessage</c> call (which receives only the in-memory
    /// PendingCommand). Piggy-backing on the identifier as a compact pipe-separated
    /// payload solves that. The command-time shape is the head alone (no pipe); the
    /// processor overwrites with the full composite before the completion message runs
    /// so the channel text reports the truthful clamped/rolled value.
    ///
    /// <para>
    /// Three processors currently use this pattern (Corrupt, Milk, Climaxfor). New
    /// processors that need to ferry a per-call integer alongside a verb/substance/type
    /// should call <see cref="Compose"/>, <see cref="ExtractHead"/>, and
    /// <see cref="TryExtractTail"/> here instead of inventing a fourth pipe encoder.
    /// </para>
    ///
    /// <para>
    /// Per-processor defaults (e.g. "if the verb is missing fall back to <c>corrupt</c>",
    /// "if the magnitude is unparseable fall back to 1") are the processor's business, so
    /// each processor keeps a small façade method that calls into here and applies its own
    /// default. See <see cref="Commitment.CorruptionProcessor.ParseVerbFromIdentifier"/>,
    /// <see cref="Involved.MilkProcessor.ParseSubstanceFromIdentifier"/>, and
    /// <see cref="Involved.ClimaxforProcessor.ParseTypeFromIdentifier"/> for examples.
    /// </para>
    /// </summary>
    public static class IdentifierPayload
    {
        /// <summary>Pipe character separating the head and tail portions.</summary>
        public const char Separator = '|';

        /// <summary>
        /// Encode a payload as <c>"{head}|{tail}"</c>. A null head is treated as the empty
        /// string so the round-trip can still be parsed.
        /// </summary>
        public static string Compose(string head, int tail)
        {
            return (head ?? string.Empty) + Separator + tail.ToString();
        }

        /// <summary>
        /// Pull the head portion out of a payload. Returns:
        /// <list type="bullet">
        ///   <item><c>null</c> when the input is null or empty.</item>
        ///   <item>The entire input when no separator is present (command-time shape — the
        ///         processor hasn't stamped the tail yet).</item>
        ///   <item>The substring before the separator otherwise.</item>
        /// </list>
        /// Callers wanting a non-null default should coalesce: <c>ExtractHead(x) ?? "default"</c>.
        /// </summary>
        public static string ExtractHead(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return null;
            int pipe = identifier.IndexOf(Separator);
            return pipe < 0 ? identifier : identifier.Substring(0, pipe);
        }

        /// <summary>
        /// Try to parse the integer tail from a payload. Returns false (with
        /// <paramref name="tail"/>=0) when the input is null/empty, when the separator is
        /// missing, when the separator is the last character, or when the tail portion
        /// isn't a valid integer.
        ///
        /// Use this directly when callers need to distinguish "no tail recorded" from
        /// "tail recorded as 0" (e.g. milk's TOCTOU clamp path). When a numeric fallback
        /// is acceptable, prefer the convenience overload that accepts a default.
        /// </summary>
        public static bool TryExtractTail(string identifier, out int tail)
        {
            tail = 0;
            if (string.IsNullOrEmpty(identifier)) return false;
            int pipe = identifier.IndexOf(Separator);
            if (pipe < 0 || pipe >= identifier.Length - 1) return false;
            string tailPart = identifier.Substring(pipe + 1);
            return int.TryParse(tailPart, out tail);
        }

        /// <summary>
        /// Convenience overload: returns the parsed tail, or <paramref name="defaultValue"/>
        /// when <see cref="TryExtractTail"/> would have returned false.
        /// </summary>
        public static int ExtractTailOr(string identifier, int defaultValue)
        {
            return TryExtractTail(identifier, out int value) ? value : defaultValue;
        }
    }
}
