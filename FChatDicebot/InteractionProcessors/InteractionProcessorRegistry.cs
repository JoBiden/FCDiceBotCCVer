using FChatDicebot.InteractionProcessors.Casual;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Consequence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Registry that manages all interaction processors.
    /// Automatically discovers and registers all processor implementations.
    /// </summary>
    public class InteractionProcessorRegistry
    {
        private static Dictionary<string, IInteractionProcessor> _processors = new Dictionary<string, IInteractionProcessor>();
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the registry by discovering all interaction processors.
        /// This is called automatically on first use.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            //casual interactions
            RegisterProcessor(new KissProcessor());
            RegisterProcessor(new CuddleProcessor());
            RegisterProcessor(new HandholdProcessor());
            RegisterProcessor(new SpankProcessor());
            RegisterProcessor(new BullyProcessor());

            //commitment interactions
            RegisterProcessor(new MarkProcessor());
            RegisterProcessor(new EntitleProcessor());
            RegisterProcessor(new PetrifyProcessor());
            RegisterProcessor(new PlantProcessor());
            RegisterProcessor(new ObjectifyProcessor());
            RegisterProcessor(new ConsumeProcessor());
            RegisterProcessor(new EmployProcessor());
            RegisterProcessor(new BondProcessor());

            //involved interactions
            RegisterProcessor(new FeedProcessor());
            RegisterProcessor(new GoldenProcessor());
            RegisterProcessor(new DressupProcessor());
            RegisterProcessor(new PaymentGiveProcessor());
            RegisterProcessor(new PaymentReceiveProcessor());

            //consequence interactions
            RegisterProcessor(new MonsterizeProcessor());
            RegisterProcessor(new RenameProcessor());

            _initialized = true;
        }

        /// <summary>
        /// Register a processor for a specific interaction type
        /// </summary>
        private static void RegisterProcessor(IInteractionProcessor processor)
        {
            _processors[processor.InteractionType.ToLower()] = processor;
        }

        /// <summary>
        /// Get the processor for a specific interaction type
        /// </summary>
        public static IInteractionProcessor GetProcessor(string interactionType)
        {
            Initialize(); // Ensure registry is initialized

            string key = interactionType.ToLower();
            if (_processors.ContainsKey(key))
            {
                return _processors[key];
            }

            return null;
        }

        /// <summary>
        /// Check if a processor exists for an interaction type
        /// </summary>
        public static bool HasProcessor(string interactionType)
        {
            Initialize();
            return _processors.ContainsKey(interactionType.ToLower());
        }

        /// <summary>
        /// Get all registered interaction types
        /// </summary>
        public static List<string> GetAllInteractionTypes()
        {
            Initialize();
            return _processors.Keys.ToList();
        }

        /// <summary>
        /// Get all processors for a specific investment level
        /// </summary>
        public static List<IInteractionProcessor> GetProcessorsByInvestmentLevel(string investmentLevel)
        {
            Initialize();
            return _processors.Values
                .Where(p => p.InvestmentLevel.Equals(investmentLevel, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
