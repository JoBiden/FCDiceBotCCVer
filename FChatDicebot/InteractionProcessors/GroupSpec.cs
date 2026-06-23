namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// How a group-capable casual interaction credits counts when its shared "moment"
    /// resolves (B4.3 / B7.12). Non-casual processors return no spec and are not
    /// group-capable (they fall back to independent 1:1 fan-out).
    /// </summary>
    public enum GroupCountKind
    {
        /// <summary>
        /// Symmetric (kiss / cuddle / handhold): every participant gets +(M-1) to a single
        /// shared key — i.e. +1 per other participant.
        /// </summary>
        Symmetric,

        /// <summary>
        /// Directional one-to-many (spank / bully / boobhat / lick): the initiator gets +R
        /// to the give key, each consenting recipient +1 to the take key.
        /// </summary>
        Directional,

        /// <summary>
        /// Lapsit's per-position rule: a person at stack position k gets +(M-1-k) lapsittake
        /// and +k lapsitgive. Handled entirely inside <c>LapsitProcessor</c>.
        /// </summary>
        Lapsit
    }

    /// <summary>
    /// Declares a casual processor's group count model so the shared resolution path can
    /// apply the right increments without each processor reimplementing the +N math.
    /// </summary>
    public class GroupSpec
    {
        public GroupCountKind Kind { get; private set; }

        // Symmetric: the single shared key both/all sides receive.
        public string SymmetricKey { get; private set; }

        // Directional: the initiator's "give" key and each recipient's "take" key.
        public string GiveKey { get; private set; }
        public string TakeKey { get; private set; }

        public static GroupSpec Symmetric(string key)
        {
            return new GroupSpec { Kind = GroupCountKind.Symmetric, SymmetricKey = key };
        }

        public static GroupSpec Directional(string giveKey, string takeKey)
        {
            return new GroupSpec { Kind = GroupCountKind.Directional, GiveKey = giveKey, TakeKey = takeKey };
        }

        public static GroupSpec LapStack()
        {
            return new GroupSpec { Kind = GroupCountKind.Lapsit };
        }
    }
}
