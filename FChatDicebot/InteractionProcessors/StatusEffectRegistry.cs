using FChatDicebot.InteractionProcessors.StatusEffectContributors;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Registry of <see cref="IStatusEffectContributor"/> instances. Parallels
    /// <see cref="InteractionProcessorRegistry"/>. Status-effect-producing interactions
    /// (odorize, dose, break, infest, corrupt, curse) add their contributor here in
    /// <see cref="Initialize"/>; <see cref="InteractionProcessorBase.GetActiveStatusEffects"/>
    /// walks the list and merges what each one returns.
    ///
    /// Order of contribution is registration order. Tests can call <see cref="Clear"/> and
    /// <see cref="RegisterContributor"/> to inject fakes in isolation.
    /// </summary>
    public static class StatusEffectRegistry
    {
        private static readonly List<IStatusEffectContributor> _contributors = new List<IStatusEffectContributor>();
        private static bool _initialized = false;

        /// <summary>
        /// Seed the registry with all built-in contributors. Idempotent. Called automatically
        /// the first time the registry is queried.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // Contributors are added here as their feature lands.
            RegisterContributor(new OdorizeStatusContributor(MonDB.GetDatabase()));
            RegisterContributor(new CorruptionStatusContributor());

            _initialized = true;
        }

        /// <summary>
        /// Append a contributor. Public so consequence-interaction wiring and tests can both
        /// register; production callers should prefer adding their entry to
        /// <see cref="Initialize"/> so the contributor is on for every run.
        /// </summary>
        public static void RegisterContributor(IStatusEffectContributor contributor)
        {
            if (contributor == null) return;
            _contributors.Add(contributor);
        }

        /// <summary>
        /// Drop all contributors and reset the initialized flag. Intended for test isolation
        /// only — production code never needs this.
        /// </summary>
        public static void Clear()
        {
            _contributors.Clear();
            _initialized = false;
        }

        /// <summary>
        /// Snapshot of currently-registered contributors, in registration order. Triggers
        /// <see cref="Initialize"/> if it hasn't run yet.
        /// </summary>
        public static IReadOnlyList<IStatusEffectContributor> GetAllContributors()
        {
            Initialize();
            return _contributors.AsReadOnly();
        }
    }
}
