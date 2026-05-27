using System.Collections.Generic;
using FChatDicebot.InteractionProcessors.StatusEffectContributors;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Registry of <see cref="IPostInteractionEffect"/> instances. Parallels
    /// <see cref="StatusEffectRegistry"/> but for cross-party side effects that mutate both
    /// initiator and recipient (currently parasite spread; future contagion mechanics).
    ///
    /// Order of invocation is registration order. Tests can call <see cref="Clear"/> and
    /// <see cref="RegisterEffect"/> to inject fakes in isolation.
    /// </summary>
    public static class PostInteractionEffectRegistry
    {
        private static readonly List<IPostInteractionEffect> _effects = new List<IPostInteractionEffect>();
        private static bool _initialized = false;

        /// <summary>
        /// Seed the registry with all built-in effects. Idempotent. Called automatically
        /// the first time the registry is queried.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            RegisterEffect(new ParasiteSpreadEffect());

            _initialized = true;
        }

        /// <summary>
        /// Append an effect. Public so consequence-interaction wiring and tests can both
        /// register; production callers should prefer adding their entry to
        /// <see cref="Initialize"/> so the effect is on for every run.
        /// </summary>
        public static void RegisterEffect(IPostInteractionEffect effect)
        {
            if (effect == null) return;
            _effects.Add(effect);
        }

        /// <summary>
        /// Drop all effects and reset the initialized flag. Intended for test isolation only —
        /// production code never needs this.
        /// </summary>
        public static void Clear()
        {
            _effects.Clear();
            _initialized = false;
        }

        /// <summary>
        /// Snapshot of currently-registered effects, in registration order. Triggers
        /// <see cref="Initialize"/> if it hasn't run yet.
        /// </summary>
        public static IReadOnlyList<IPostInteractionEffect> GetAllEffects()
        {
            Initialize();
            return _effects.AsReadOnly();
        }
    }
}
