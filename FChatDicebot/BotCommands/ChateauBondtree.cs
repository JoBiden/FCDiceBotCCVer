using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Database;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !bondtree — read-only walk of the bond graph that lists everyone connected to a resident
    /// within N degrees of separation, across every bond type. Public, consent-free (bonds are
    /// mutually declared, public facts); result is PM'd to the invoker. Shares its traversal with
    /// !familytree via <see cref="BondTreeSupport"/>, differing only in the type filter.
    /// </summary>
    public class ChateauBondtree : ChatBotCommand
    {
        private readonly IChateauDatabase _database;

        public ChateauBondtree(IChateauDatabase database)
        {
            _database = database;
            Name = "bondtree";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "Map everyone connected to a resident by bonds, N degrees out";
            LongDescription = "Walks the bond graph and lists everyone connected to a resident within N degrees of separation, across every bond type, grouped by degree. " +
                "Bonds are public, so no consent is needed. The result is sent to you in a private message.\n" +
                "Defaults to yourself and 2 degrees. Pass a name to root the tree on someone else, and/or a number (1-3) to set the depth.";
            Usage = "!bondtree\nor\n!bondtree [noparse][user]CharacterName[/user][/noparse]\nor\n!bondtree [noparse][user]CharacterName[/user][/noparse] 3";
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
        public ChateauBondtree() : this(MonDB.GetDatabase())
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
            string message = BondTreeSupport.BuildBondTree(targetUser, degrees, familyOnly: false, _database.GetProfile);
            bot.SendPrivateMessage(message, characterName);
        }
    }
}
