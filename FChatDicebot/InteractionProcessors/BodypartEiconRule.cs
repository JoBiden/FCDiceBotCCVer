namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Where the bodypart a completion message decorates comes from.
    /// </summary>
    public enum BodypartEiconSource
    {
        /// <summary>No bodypart eicon for this interaction (the default for every processor).</summary>
        None,
        /// <summary>The identifier the resident typed (<c>!mark Bob ass</c> → ass).</summary>
        FromIdentifier,
        /// <summary>A part implied by the interaction itself (<c>!spank</c> → ass).</summary>
        Fixed,
    }

    /// <summary>
    /// Whose body the part belongs to, and therefore whose personal eicon renders.
    /// </summary>
    public enum BodypartEiconOwner
    {
        Initiator,
        Recipient,
        Both,
    }

    /// <summary>
    /// Declares that an interaction involves a specific bodypart, so the part-owner's personal
    /// <c>!seteicon {bodypart}</c> icon should join the completion message's trailing flourish
    /// (after the interaction eicons). Processors opt in by overriding
    /// <see cref="InteractionProcessorBase.BodypartEiconRule"/>; null — the default — means the
    /// interaction shows no bodypart eicon at all.
    ///
    /// A part with no stored eicon simply appends nothing: identifiers outside the
    /// <c>bodypart</c> category (<c>!break mind</c>) and residents who never set one both take
    /// the same graceful miss.
    /// </summary>
    public class BodypartEiconRule
    {
        /// <summary>Where the part comes from. <see cref="BodypartEiconSource.None"/> disables the rule.</summary>
        public BodypartEiconSource Source { get; set; }

        /// <summary>Whose part it is, and so whose eicon renders.</summary>
        public BodypartEiconOwner Owner { get; set; }

        /// <summary>The part, for <see cref="BodypartEiconSource.Fixed"/> rules only.</summary>
        public string FixedPart { get; set; }

        /// <summary>The part this rule decorates for a given typed identifier, or null when there isn't one.</summary>
        public string ResolvePart(string identifier)
        {
            switch (Source)
            {
                case BodypartEiconSource.Fixed:
                    return FixedPart;
                case BodypartEiconSource.FromIdentifier:
                    return string.IsNullOrEmpty(identifier) ? null : identifier;
                default:
                    return null;
            }
        }

        /// <summary>The part the resident typed (<c>!mark</c>, <c>!consume</c>, <c>!golden</c>, <c>!break</c>).</summary>
        public static BodypartEiconRule Typed(BodypartEiconOwner owner)
        {
            return new BodypartEiconRule { Source = BodypartEiconSource.FromIdentifier, Owner = owner };
        }

        /// <summary>A part the interaction always involves (<c>!spank</c> → ass, <c>!lick</c> → tongue).</summary>
        public static BodypartEiconRule Part(string bodypart, BodypartEiconOwner owner)
        {
            return new BodypartEiconRule { Source = BodypartEiconSource.Fixed, FixedPart = bodypart, Owner = owner };
        }
    }
}
