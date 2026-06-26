using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Database;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !familytree — the family-only sibling of !bondtree. Same read-only bond-graph walk, but
    /// restricted to the family bond subset (marriage, offspring, sibling, family/kin). Public,
    /// consent-free; result is PM'd to the invoker. A real command rather than an alias, because
    /// it differs from !bondtree in the type filter it passes to <see cref="BondTreeSupport"/>.
    /// </summary>
    public class ChateauFamilytree : ChatBotCommand
    {
        private readonly IChateauDatabase _database;

        public ChateauFamilytree(IChateauDatabase database)
        {
            _database = database;
            Name = "familytree";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "Map a resident's family bonds, N degrees out";
            LongDescription = "Walks only the family bonds (marriage, offspring, sibling, kin) and lists everyone connected to a resident within N degrees of separation, grouped by degree. " +
                "Bonds are public, so no consent is needed. The result is sent to you in a private message.\n" +
                "Defaults to yourself and 2 degrees. Pass a name to root the tree on someone else, and/or a number (1-3) to set the depth.";
            Usage = "!familytree\nor\n!familytree [noparse][user]CharacterName[/user][/noparse]\nor\n!familytree [noparse][user]CharacterName[/user][/noparse] 3";
            RelatedCommands = new string[] { "bond", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        /// <summary>
        /// Legacy constructor for the reflection-based command loader (uses MonDB).
        /// </summary>
        public ChateauFamilytree() : this(MonDB.GetDatabase())
        {
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string targetUser = commandController.GetUserNameFromCommandTerms(rawTerms);
            if (string.IsNullOrWhiteSpace(targetUser))
            {
                targetUser = characterName;
            }

            Profile rootProfile = _database.GetProfile(targetUser);
            if (rootProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(targetUser), characterName);
                return;
            }

            int degrees = BondTreeSupport.ParseDegrees(terms);
            string message = BondTreeSupport.BuildBondTree(targetUser, degrees, familyOnly: true, _database.GetProfile);
            bot.SendPrivateMessage(message, characterName);
        }
    }
}
