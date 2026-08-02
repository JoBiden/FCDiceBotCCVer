using FChatDicebot.BotCommands;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.SavedData;

namespace FChatDicebot
{
    public class BotCommandController
    {
        private BotMain Bot;

        public List<ChatBotCommand> BotCommands;

        // alias -> the command that owns it, built once at load from every command's Aliases
        // array. Aliases are a fallback, never an override: FindCommandByName tries real Names
        // first, so adding an alias can't shadow a command that already exists.
        private Dictionary<string, ChatBotCommand> AliasIndex;

        private object SavedTablesFileLock = new object();
        private object SavedChannelsFileLock = new object();
        private object ChannelDecksLock = new object();
        private object ChannelScoresLock = new object();

        public object ModeratorRequestLock = new object();

        public BotCommandController(BotMain sourceBot)
        {
            Bot = sourceBot;
            BotCommands = new List<ChatBotCommand>();

            LoadChatBotCommands();
            BuildAliasIndex();
        }

        private void LoadChatBotCommands()
        {
            Type[] allTypes;
            try
            {
                allTypes = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // If some types can't be loaded (e.g. a dependency that isn't present in
                // the current host, such as a unit-test runner), keep the ones that did load.
                allTypes = ex.Types.Where(t => t != null).ToArray();
            }

            foreach(Type thisType in allTypes)
            {
                if (thisType != null && thisType.Namespace == "FChatDicebot.BotCommands")
                {
                    try
                    {
                        object obj = Activator.CreateInstance(thisType);

                        //dynamic is assumed to be any type you tell it, not validated at compile time. can crash runtime if methods not found.
                        dynamic changedObj = Convert.ChangeType(obj, thisType);

                        if (changedObj.GetType().BaseType == typeof(ChatBotCommand))
                            BotCommands.Add(changedObj);
                    }
                    catch (Exception ex)
                    {
                        // A single command type that can't be instantiated here (for example a
                        // DI command whose parameterless constructor expects a live database
                        // during unit tests) should not stop every other command from loading.
                        Console.WriteLine("LoadChatBotCommands: skipped " + thisType.FullName + " - " + ex.Message);
                    }
                }
            }
        }

        // Index every command's Aliases array so aliases dispatch without a delegating class of
        // their own. Two rules keep an added alias from breaking anything: a real command Name
        // always wins over an alias, and the first claim on an alias wins over a later one. Both
        // are logged, because a silently ignored alias reads as "my alias doesn't work".
        private void BuildAliasIndex()
        {
            AliasIndex = new Dictionary<string, ChatBotCommand>(StringComparer.OrdinalIgnoreCase);

            var commandNames = new HashSet<string>(
                BotCommands.Where(a => !string.IsNullOrEmpty(a.Name)).Select(a => a.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (ChatBotCommand c in BotCommands)
            {
                if (c.Aliases == null)
                    continue;

                foreach (string rawAlias in c.Aliases)
                {
                    if (string.IsNullOrWhiteSpace(rawAlias))
                        continue;

                    string alias = rawAlias.Trim();

                    if (commandNames.Contains(alias))
                    {
                        Console.WriteLine("BuildAliasIndex: alias '" + alias + "' declared on !" + c.Name
                            + " is already a command name; the command wins and the alias is ignored.");
                        continue;
                    }

                    ChatBotCommand owner;
                    if (AliasIndex.TryGetValue(alias, out owner))
                    {
                        if (owner != c)
                        {
                            // Reflection order decides which command got here first, so name both
                            // rather than implying the winner was chosen deliberately.
                            Console.WriteLine("BuildAliasIndex: alias '" + alias + "' is claimed by both !"
                                + owner.Name + " and !" + c.Name + "; keeping !" + owner.Name + ".");
                        }
                        continue;
                    }

                    AliasIndex[alias] = c;
                }
            }
        }

        // Resolve a command by name deterministically. Commands are loaded by reflection
        // (LoadChatBotCommands), so when a legacy DiceBot command and a Chateau command share a
        // Name (historically "work" = Work/ChateauWork, "help" = DiceHelp/ChateauHelp), a plain
        // FirstOrDefault picked a non-deterministic winner by metadata-token order. The Chateau
        // command is always the intended one in this deployment, so on a collision we prefer the
        // class whose type name is the Chateau implementation.
        //
        // Falls back to the alias index, so "!hug" resolves to the ChateauCuddle instance itself
        // rather than to a stub class that forwarded to it.
        public ChatBotCommand FindCommandByName(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
                return null;

            var matches = BotCommands.Where(a => a.Name == commandName).ToList();
            if (matches.Count == 1)
                return matches[0];

            if (matches.Count > 1)
            {
                return matches.FirstOrDefault(a => a.GetType().Name.StartsWith("Chateau", StringComparison.Ordinal))
                    ?? matches[0];
            }

            ChatBotCommand aliased;
            if (AliasIndex != null && AliasIndex.TryGetValue(commandName, out aliased))
                return aliased;

            return null;
        }

        public void RunChatBotCommand(UserGeneratedCommand command)
        {
            MessageAddress address = new MessageAddress() { character = command.characterName, channel = command.channel, guild = command.guild };

            ChannelSettings settings = Bot.GetChannelSettings(address);

            ChatBotCommand c = FindCommandByName(command.commandName);

            if (c == null && settings != null)
            {
                string currencyName = settings.CurrencyName;
                string currencyNamePlural = settings.CurrencyNamePlural;

                // Map altered command names to original command objects
                var alteredCommandMap = BotCommands
                    .Where(a => a.Name.Contains("chip"))
                    .ToDictionary(
                        a => a.Name.Replace("chip", currencyName), // Altered command name
                        a => a // Original command object
                    );

                if (!string.IsNullOrEmpty(settings.WorkCommandAlias))
                {
                    // Indexer assignment (last-wins), not Dictionary.Add: a channel admin's
                    // configured alias (currency name, work alias, potion alias) can collide
                    // with another alias in this same map (e.g. a currency named "potion"
                    // colliding with the derived "showpotions" alias). Add() throws
                    // ArgumentException on a colliding key, which previously disabled all
                    // alias dispatch in that channel until the config was fixed.
                    if (alteredCommandMap.ContainsKey(settings.WorkCommandAlias))
                    {
                        Console.WriteLine("Alias collision: WorkCommandAlias '" + settings.WorkCommandAlias + "' already maps to another command in channel " + address.channel + ".");
                    }
                    alteredCommandMap[settings.WorkCommandAlias] = BotCommands.FirstOrDefault(a => a.Name == ("work"));
                }

                if (!string.IsNullOrEmpty(settings.PotionCommandsAlias))
                {
                    string potionName = settings.PotionCommandsAlias;
                    var alteredCommandMapPotions = BotCommands
                        .Where(a => a.Name.Contains("potion"))
                        .ToDictionary(
                            a => a.Name.Replace("potion", potionName), // Altered command name
                            a => a // Original command object
                        );
                    foreach (var pot in alteredCommandMapPotions)
                    {
                        if (alteredCommandMap.ContainsKey(pot.Key))
                        {
                            Console.WriteLine("Alias collision: PotionCommandsAlias '" + pot.Key + "' already maps to another command in channel " + address.channel + ".");
                        }
                        alteredCommandMap[pot.Key] = pot.Value;
                    }
                }

                // Map altered command names to original command objects
                var alteredCommandMapPlural = BotCommands
                    .Where(a => a.Name.Contains("chips"))
                    .ToDictionary(
                        a => a.Name.Replace("chips", currencyNamePlural), // Altered command name
                        a => a // Original command object
                    );

                // Check if the altered command name matches the user's command name
                if (c == null && alteredCommandMapPlural.TryGetValue(command.commandName, out var matchedCommand2))
                {
                    c = matchedCommand2;
                }
                if (alteredCommandMap.TryGetValue(command.commandName, out var matchedCommand))
                {
                    c = matchedCommand;
                }
            }

            if (c == null)
            {
                if(string.IsNullOrEmpty(command.channel))
                {
                    Bot.SendPrivateMessage("Failed: That is not a valid command. Use !help for a list of commands.", address);
                }
                return;
            }

            // Hoisted out of the registration check below so bare-name targeting can be gated
            // on it too — an unregistered resident should hear about registering, not about
            // which of three residents they might have meant.
            bool callerIsRegistered = MonDB.getProfile(command.characterName) != null;

            if (callerIsRegistered && c.ResolvesRecipient())
            {
                if (!TryResolveBareRecipient(c, command, address))
                    return;
            }

            string[] terms = Utils.LowercaseStrings(command.rawTerms);
            terms = Utils.TrimStringsAndRemoveEmpty(terms);
            terms = Utils.FixComparators(terms);
            command.terms = terms;

            object lockObject = GetObjectToLock(c.LockCategory);
            bool characterIsAdmin = Utils.IsCharacterAdmin(Bot.AccountSettings.AdminCharacters, command.characterName);

            bool fromChannel = MessageCameFromChannel(address);
            if (!callerIsRegistered && command.commandName != "joinchateau")
            {
                Bot.SendPrivateMessage(ChateauInteractionHandler.notRegisteredText(), address);
            }
            else if (fromChannel || !c.RequireChannel)
            {
                if (characterIsAdmin || !c.RequireBotAdmin)
                {
                    CharacterData characterData = null;
                    if (fromChannel)
                    {
                        characterData = Bot.DiceBot.GetCharacterData(address, true);

                        if (characterData == null)
                        {
                            Bot.SendMessageInChannel("Error: Character Data not found for " + address.character + ".", address);
                            return;
                        }
                        else
                        {
                            characterData.LastCommand = DoubleTime.GetCurrentTimestampSeconds();
                            SaveCharacterDataToDisk();
                        }
                    }

                    bool authorizedDiscordAdmin = AuthorizedDiscordAdmin(command);

                    if (characterData != null && characterData.CharacterIsTimedOut() && !characterIsAdmin)
                    {
                        Bot.SendMessageInChannel("Failed: You are currently timed out for " + DoubleTime.PrintTimeFromSeconds(characterData.TimeoutDuration - (DoubleTime.GetCurrentTimestampSeconds() - characterData.TimeoutStartTime)) + ".", address);
                    }
                    else if (command.ops == null && ((c.RequireChannelAdmin && !characterIsAdmin) || c.RequireBotIsChannelAdmin) && !Utils.IsDiscordMessage(command) && fromChannel)
                    {
                        Bot.RequestChannelOpListAndQueueFurtherRequest(command);
                    }
                    else if ((!c.RequireChannelAdmin) || characterIsAdmin || authorizedDiscordAdmin ||
                        (!fromChannel && !c.RequireChannel) ||
                        (command.ops != null && command.ops.Contains(command.characterName)))
                    {
                        bool botIsAdmin = false;
                        if (command.ops != null && Bot.AccountSettings != null)
                            botIsAdmin = command.ops.Contains(Bot.AccountSettings.CharacterName);
                        if (!c.RequireBotIsChannelAdmin || botIsAdmin || !fromChannel)
                        {
                            if (lockObject != null)
                            {
                                lock (lockObject)
                                {
                                    c.Run(Bot, this, command.rawTerms, terms, address, command);
                                }
                            }
                            else
                            {
                                c.Run(Bot, this, command.rawTerms, terms, address, command);
                            }
                        }
                        else
                        {
                            if (fromChannel)
                                Bot.SendMessageInChannel("Failed: " + TextFormat.GetCharacterUserTags(Bot.AccountSettings.CharacterName) + " needs to be a channel/ guild op to use this command (" + command.commandName + ").", address);
                            else
                                Bot.SendPrivateMessage("Failed: " + TextFormat.GetCharacterUserTags(Bot.AccountSettings.CharacterName) + " needs to be a channel/ guild op to use this command (" + command.commandName + ").", address);
                        }
                    }
                    else
                    {
                        if (fromChannel)
                            Bot.SendMessageInChannel("Failed: " + TextFormat.GetCharacterUserTags(command.characterName) + ", you need to be a channel/ guild op to use this command (" + command.commandName + ").", address);
                        else
                            Bot.SendPrivateMessage("Failed: " + TextFormat.GetCharacterUserTags(command.characterName) + ", you need to be a channel/ guild op to use this command (" + command.commandName + ").", address);
                    }
                }
                else
                {
                    if (fromChannel)
                        Bot.SendMessageInChannel("Failed: You do not have authorization to complete this command.", address);
                    else
                        Bot.SendPrivateMessage("Failed: You do not have authorization to complete this command.", address);
                }
            }
            else if(!fromChannel && c.RequireChannel)
            {
                Bot.SendPrivateMessage("Failed: This command requires a channel to use.", address);
            }
        }

        public bool MessageCameFromChannel(MessageAddress address)
        {
            return !string.IsNullOrEmpty(address.GetChannelKey());
        }

        public bool AuthorizedDiscordAdmin(UserGeneratedCommand command)
        {
            bool authorizedDiscordAdmin = false;
            if (Utils.IsDiscordMessage(command))
            {
                //get discord user
                var user = BotMain.GetDiscordUser(command.characterName);
                if (typeof(Discord.WebSocket.SocketGuildUser) == user.GetType())
                {
                    authorizedDiscordAdmin = Utils.IsDiscordAdmin((Discord.WebSocket.SocketGuildUser)user);
                }
            }
            return authorizedDiscordAdmin;
        }

        public string GetChannelFromInputs(string[] rawTerms, out string error)
        {
            error = "";
            if (rawTerms == null || rawTerms.Length < 1)
            {
                error = "Error: requires parameters to designate channel id.";
                return "";
            }

            string channelIdToSend = rawTerms[0];
            string channelLower = channelIdToSend.ToLower();

            string samplechannel = BotMain.CasinoChannelId;

            switch (channelLower)
            {
                case "casino":
                case "vccasino":
                    channelIdToSend = BotMain.CasinoChannelId;// "adh-3fe0682b9b6bbe0acb62";
                    break;
                case "breakerworld":
                case "breaker":
                    channelIdToSend = BotMain.BreakerWorldChannelId;
                    break;
                case "kowloon":
                case "kcc":
                    channelIdToSend = BotMain.KowloonChannelId;
                    break;
                case "fateroom":
                case "fate":
                    channelIdToSend = BotMain.SevenMinutesFateRoomId;
                    break;
                case "fategu":
                case "suitengu":
                    channelIdToSend = BotMain.SuitenGuFateRoomId;
                    break;
                case "chessclub":
                case "chess":
                    channelIdToSend = BotMain.ChessClubChannelId;
                    break;
                case "testroom":
                case "testdicebot":
                case "test":
                    channelIdToSend = BotMain.TestDicebotChannelId;
                    break;
                case "sonic":
                case "sonicheroes":
                case "sonicdarkesthour":
                case "darkesthour":
                case "sonicheroesdarkesthour":
                    channelIdToSend = BotMain.SonicHeroesDarkestHourRoomId;
                    break;
            }

            if (channelIdToSend == null || channelIdToSend.Length != samplechannel.Length)
            {
                error = "Error: invalid channel id. paste only the [channel id] from /code in a room (ex: adh-3fe0682b9b6bbe0acb62)";
                channelIdToSend = "";
            }

            return channelIdToSend;
        }

        public object GetObjectToLock(CommandLockCategory lockType)
        {
            switch(lockType)
            {
                case CommandLockCategory.SavedChannels:
                    return SavedChannelsFileLock;
                case CommandLockCategory.SavedTables:
                    return SavedTablesFileLock;
                case CommandLockCategory.ChannelDecks:
                    return ChannelDecksLock;
                case CommandLockCategory.ChannelScores:
                    return ChannelScoresLock;
            }
            return null;
        }

        public string GetNonNumberWordFromCommandTerms(string [] terms)
        {
            var leftovers = terms.Where(a => !Char.IsDigit(a[0]));

            string wordFound = leftovers.FirstOrDefault();
            if (wordFound != null)
                wordFound = wordFound.ToLower();

            return wordFound;
        }

        public SlotsSetting GetDefaultSlotsSetting(SlotsType defaultSlots)
        {
            switch(defaultSlots)
            {
                case SlotsType.Default:
                case SlotsType.Bondage:
                    return Bot.SavedSlots.FirstOrDefault(a => a.SlotsId == "Bondage").SlotsSetting;
                case SlotsType.Fruit:
                    return Bot.SavedSlots.FirstOrDefault(a => a.SlotsId == "Fruits").SlotsSetting;
                case SlotsType.Melty:
                    return Bot.SavedSlots.FirstOrDefault(a => a.SlotsId == "Melty").SlotsSetting;
            }

            return Bot.SavedSlots.FirstOrDefault(a => a.SlotsId == "Bondage").SlotsSetting;
        }

        public string GetTableNameFromCommandTerms(string[] terms)
        {
            var leftovers = terms.Where(a => a != "nolabel" && !a.Contains('+') && !a.Contains('-'));

            string tableName = leftovers.FirstOrDefault();
            if (tableName != null)
                tableName = tableName.ToLower();

            return tableName;
        }

        public int GetRollModifierFromCommandTerms(string[] terms)
        {
            string modifierTerm = terms.FirstOrDefault(a => a.StartsWith("+") || a.StartsWith("-"));

            int mod = 0;

            if (modifierTerm != null && modifierTerm.Length > 1)
            {
                bool negative = modifierTerm.StartsWith("-");

                modifierTerm = modifierTerm.Substring(1);

                int.TryParse(modifierTerm, out mod);

                if (negative)
                    mod = -1 * mod;
            }

            return mod;
        }

        public DeckType GetDeckTypeFromCommandTerms(string[] terms, out string deckId)
        {
            deckId = "";
            DeckType deckType = DeckType.Playing;
            if (terms != null && terms.Length >= 1 && terms.Contains("tarot"))
            {
                deckType = DeckType.Tarot;
            }
            if (terms != null && terms.Length >= 1 && terms.Contains("manythings"))
            {
                deckType = DeckType.ManyThings;
            }
            if (terms != null && terms.Length >= 1 && terms.Contains("uno"))
            {
                deckType = DeckType.Uno;
            }
            if (terms != null && terms.Length >= 1 && terms.Contains("rumble"))
            {
                deckType = DeckType.BreakerRumble;
            }
            if (terms != null && terms.Length >= 1 && terms.Contains("rumbleclassic"))
            {
                deckType = DeckType.BreakerRumbleClassic;
            }
            if (terms != null && terms.Length >= 1 && terms.Contains("rumbleextra"))
            {
                deckType = DeckType.BreakerRumbleExtra;
            }

            for(int i = 0; i < terms.Length; i++)
            {
                if(terms[i].StartsWith("deck:"))
                {
                    string mod = terms[i].Replace("deck:", "");
                    deckId = mod;
                    deckType = DeckType.Custom;
                }
            }
            //if (terms != null && terms.Length >= 1 && terms.Contains("skipbo")) //TODO: add skipbo deck
            //    deckType = DeckType.Skipbo;
            //if (terms != null && terms.Length >= 1 && terms.Contains("custom"))
            //    deckType = DeckType.Custom;

            return deckType;
        }

        public DeckType GetExtraDeckTypeFromCommandTerms(string[] terms, out string deckId)
        {
            deckId = "";
            DeckType rtn = DeckType.NONE;
            for(int i = 0; i < terms.Length; i++)
            {
                if(terms[i].StartsWith("from:"))
                {
                    string newTerm = terms[i].Replace("from:", "");

                    string[] newCommands = new string[] { newTerm };

                    rtn = GetDeckTypeFromCommandTerms(newCommands, out deckId);
                }
            }
            return rtn;
        }

        public IGame GetGameTypeForCommand(DiceBot diceBot, MessageAddress address, string[] terms, out string errorString)
        {
            errorString = "";
            IGame gametype = GetGameTypeFromCommandTerms(diceBot, terms);

            if (gametype == null)
            {
                string channelKey = address.GetChannelKey();
                //check game sessions and see if this channel has a session for anything
                var gamesPresent = diceBot.GameSessions.Where(a => a.GetChannelKey() == channelKey);
                if (gamesPresent.Count() == 0)
                {
                    errorString = "Error: No game type specified. [i]You must create a game session by specifying the game type as the first player.[/i]";
                }
                else if (gamesPresent.Count() > 1)
                {
                    errorString = "Error: You must specify a game type if more than one game session exists in the channel.";
                }
                else if (gamesPresent.Count() == 1)
                {
                    GameSession sesh = gamesPresent.First();
                    gametype = sesh.CurrentGame;
                }
            }

            return gametype;
        }

        public IGame GetGameTypeFromCommandTerms(DiceBot diceBot, string[] terms)
        {
            if (terms == null || terms.Length == 0)
                return null;

            IGame returnGame = null;

            foreach (IGame game in diceBot.PossibleGames)
            {
                if(terms.Contains(game.GetGameName().ToLower()))
                {
                    returnGame = game;
                    break;
                }
            }
            return returnGame;
        }

        public string GetCharacterDrawNameFromCommandTerms(string characterName, string[] terms)
        {
            string characterDrawName = characterName;
            if (terms != null && terms.Length >= 1 && terms.Contains("dealer"))
                characterDrawName = DiceBot.DealerPlayerAlias;
            if (terms != null && terms.Length >= 1 && terms.Contains("burn"))
                characterDrawName = DiceBot.BurnCardsPlayerAlias;
            if (terms != null && terms.Length >= 1 && terms.Contains("discard"))
                characterDrawName = DiceBot.DiscardPlayerAlias;
            if (terms != null && terms.Length >= 1 && terms.Contains("inplay"))
                characterDrawName = characterName + DiceBot.PlaySuffix;

            return characterDrawName;
        }

        public string GetChannelPotionName(ChannelSettings settings, bool capitalize)
        {
            string potionAlias = "potion";
            if (settings != null)
                potionAlias = settings.PotionCommandsAlias;
            if (capitalize)
                potionAlias = TextFormat.CapitalizeFirst(potionAlias);

            return potionAlias;
        }

        /// <summary>
        /// Lets a resident type a bare name where a <c>[user]</c> tag is expected
        /// ("!cuddle bob smith", "!dose queen contract lust") by rewriting
        /// <c>command.rawTerms</c> to carry the tag they didn't type. Every parser downstream
        /// then sees the shape it already understands, which is why this is a normalization
        /// pass here rather than a change to each of the ~55 targeting commands.
        ///
        /// Returns false when the resident clearly named someone but we couldn't place them —
        /// either it matched more than one resident or it matched none. They've been told who
        /// they might have meant; the command must not run on a guess or on an empty target.
        /// </summary>
        private bool TryResolveBareRecipient(ChatBotCommand c, UserGeneratedCommand command, MessageAddress address)
        {
            try
            {
                List<ProfileName> knownNames = ProfileNameCache.GetNames();

                RecipientResolution resolution = RecipientResolver.Resolve(
                    command.rawTerms,
                    GetNonNameArgumentTerms(c),
                    knownNames);

                if (resolution.IsAmbiguous)
                {
                    Bot.SendPrivateMessage(
                        ChateauInteractionHandler.ambiguousNameText(
                            resolution.TypedText,
                            DisplayNamesFor(resolution.Candidates, knownNames)),
                        address);
                    return false;
                }

                if (!string.IsNullOrEmpty(resolution.UnresolvedText))
                {
                    // Words that nothing else in the command accounts for and that match no
                    // resident. The command's own recipient lookup is about to fail on this,
                    // and it would report an empty name — say who we didn't recognize instead.
                    List<string> suggestions = RecipientResolver.SuggestSimilarNames(
                        resolution.UnresolvedText, knownNames, 3);

                    Bot.SendPrivateMessage(
                        ChateauInteractionHandler.nameNotRecognizedText(
                            resolution.UnresolvedText,
                            DisplayNamesFor(suggestions, knownNames)),
                        address);
                    return false;
                }

                if (!string.IsNullOrEmpty(resolution.ResolvedName))
                {
                    command.rawTerms = resolution.Terms;
                }
            }
            catch (Exception ex)
            {
                // Inferring a name is a convenience laid over a parse that already works with
                // an explicit tag, so a database hiccup here must not take the command down —
                // fall through and let it run on exactly what was typed.
                Console.WriteLine("TryResolveBareRecipient: skipped for " + command.commandName + " - " + ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Every term this command could be parsing as something other than a name — the
        /// identifiers of its declared category, plus interaction types where it takes one.
        /// What's left over after these is a name attempt.
        /// </summary>
        private IEnumerable<string> GetNonNameArgumentTerms(ChatBotCommand c)
        {
            var argumentTerms = new List<string>();

            if (!string.IsNullOrEmpty(c.IdentifierCategory))
            {
                List<Identifier> identifiers = MonDB.getIdentifiers(c.IdentifierCategory);
                if (identifiers != null)
                {
                    argumentTerms.AddRange(identifiers
                        .Where(a => a != null && !string.IsNullOrEmpty(a.type))
                        .Select(a => a.type));
                }
            }

            if (c.TakesInteractionType)
            {
                List<string> interactionTypes = InteractionProcessors.InteractionProcessorRegistry.GetAllInteractionTypes();
                if (interactionTypes != null)
                    argumentTerms.AddRange(interactionTypes.Where(a => !string.IsNullOrEmpty(a)));
            }

            return argumentTerms;
        }

        /// <summary>
        /// Residents are always shown their display names, never F-Chat handles (style guide
        /// §2), so map the resolver's canonical userNames back before putting them in a message.
        /// </summary>
        private List<string> DisplayNamesFor(List<string> userNames, List<ProfileName> knownNames)
        {
            return userNames.Select(userName =>
            {
                ProfileName match = knownNames.FirstOrDefault(
                    a => string.Equals(a.userName, userName, StringComparison.OrdinalIgnoreCase));
                string display = match == null ? null : RecipientResolver.StripDisplayName(match.displayName);
                return string.IsNullOrWhiteSpace(display) ? userName : display;
            }).ToList();
        }

        public string GetUserNameFromCommandTerms(string[] terms)
        {
            bool isName = false;
            string userName = "";
            if (terms != null)
            {
                foreach (string term in terms)
                {
                    if (isName)
                    {
                        userName += " " + term;
                    }

                    if (term.StartsWith("[user]"))
                    {
                        isName = true;
                        userName = term;
                    }

                    if (term.EndsWith("[/user]"))
                    {
                        isName = false;
                        userName = userName.Remove(0, 6);
                        userName = userName.Remove(userName.Length-7);
                        Utils.SanitizeInput(userName);
                        return userName;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Multi-target counterpart of <see cref="GetUserNameFromCommandTerms"/>: returns every
        /// <c>[user]…[/user]</c> tag in the command, in order, deduplicated (case-insensitive)
        /// while preserving first-seen order. Used by the casual group commands so
        /// <c>!cuddle A B C</c> can spin up one shared moment. Never returns null — an empty
        /// list means no user tags were present.
        /// </summary>
        public List<string> GetUserNamesFromCommandTerms(string[] terms)
        {
            var names = new List<string>();
            if (terms == null)
                return names;

            bool isName = false;
            string userName = "";
            foreach (string term in terms)
            {
                if (isName)
                {
                    userName += " " + term;
                }

                if (term.StartsWith("[user]"))
                {
                    isName = true;
                    userName = term;
                }

                if (term.EndsWith("[/user]"))
                {
                    isName = false;
                    userName = userName.Remove(0, 6);
                    userName = userName.Remove(userName.Length - 7);
                    Utils.SanitizeInput(userName);
                    if (!string.IsNullOrWhiteSpace(userName) &&
                        !names.Any(n => string.Equals(n, userName, StringComparison.OrdinalIgnoreCase)))
                    {
                        names.Add(userName);
                    }
                }
            }
            return names;
        }

        public string GetEIconFromCommandTerms(string[] terms)
        {
            bool isEIcon = false;
            string EIcon = "";
            if (terms != null)
            {
                foreach (string term in terms)
                {
                    if (isEIcon)
                    {
                        EIcon += " " + term;
                    }

                    if (term.StartsWith("[eicon]"))
                    {
                        isEIcon = true;
                        EIcon = term;
                    }

                    if (term.EndsWith("[/eicon]"))
                    {
                        isEIcon = false;
                        Utils.SanitizeInput(EIcon);
                        return EIcon;
                    }
                }
            }
            return null;
        }

        public string GetQuotedTextFromCommandTerms(string[] terms)
        {
            bool isInQuotes = false;
            string quotedText = "";
            if (terms != null)
            {
                foreach (string term in terms)
                {
                    if (isInQuotes)
                    {
                        quotedText += " " + term;
                    }

                    if (term.StartsWith("\""))
                    {
                        isInQuotes = true;
                        quotedText = term;
                    }

                    if (term.EndsWith("\""))
                    {
                        isInQuotes = false;
                        quotedText = quotedText.Trim('\"');
                        Utils.SanitizeInput(quotedText);
                        return quotedText;
                    }
                }
            }
            return null;
        }

        public string GetIdentifierFromCommandTerms(string[] terms, string identifierTypeToFind)
        {
            List<Identifier> typeList = MonDB.getIdentifiers(identifierTypeToFind);

            if (terms != null)
            {
                terms = Utils.LowercaseStrings(terms);
                foreach (string term in terms)
                {
                    foreach (Identifier identifierType in typeList)
                    {
                        if (identifierType.type == term )
                        {
                            return term;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// First plain-text term that isn't part of a <c>[user]</c> / <c>[eicon]</c> / quoted
        /// span — the interaction type in <c>!pledge {name} {interactiontype}</c> and friends.
        ///
        /// Every caller passes <c>rawTerms</c>, which <see cref="BotMain.SeparateCommandTerms"/>
        /// has already stripped the command name from. This used to skip a leading term anyway
        /// and require three terms, which worked only by accident: a two-word character name
        /// fills two array slots, so the counting came out right. A single-word name left the
        /// interaction type sitting in the skipped slot, so <c>!pledge [user]Bob[/user] cuddle</c>
        /// silently found no type at all.
        /// </summary>
        public string GetInteractionTypeFromCommandTerms(string[] terms)
        {
            if (terms == null)
                return null;

            bool inUserTag = false;
            bool inQuotes = false;
            bool inEIcon = false;

            foreach (string term in terms)
            {
                if (string.IsNullOrEmpty(term))
                    continue;

                // Track if we're inside [user] tags
                if (term.StartsWith("[user]"))
                    inUserTag = true;
                if (term.EndsWith("[/user]"))
                {
                    inUserTag = false;
                    continue;
                }

                // Track if we're inside [eicon] tags
                if (term.StartsWith("[eicon]"))
                    inEIcon = true;
                if (term.EndsWith("[/eicon]"))
                {
                    inEIcon = false;
                    continue;
                }

                // Track if we're inside quotes
                if (term.StartsWith("\""))
                    inQuotes = true;
                if (term.EndsWith("\""))
                {
                    inQuotes = false;
                    continue;
                }

                // If we're not in any special tags, this is our interaction type
                if (!inUserTag && !inQuotes && !inEIcon)
                {
                    return term.ToLower();
                }
            }

            return null;
        }

        public string[] GetIntsFromCommandTermsAsStrings(string[] terms)
        {
            List<string> intTerms = new List<string>();
            foreach (string term in terms)
            {
                int outNumber = 0;
                if (Int32.TryParse(term, out outNumber))
                {
                    intTerms.Add(term);
                }
            }
            if (intTerms.Count > 0)
            {
                return intTerms.ToArray();
            }
            else
            {
                return null;
            }
        }

        public void SaveCharacterDataToDisk()
        {
            Utils.WriteToFileAsData(Bot.DiceBot.CharacterDatas, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.CharacterDataFileName));
        }

        public void SavePotionDataToDisk()
        {
            Utils.WriteToFileAsData(Bot.SavedPotions, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedPotionsFileName));
        }

        public void SaveJobsListDataToDisk()
        {
            Utils.WriteToFileAsData(Bot.SavedJobsLists, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedJobsListFileName));
        }

        public void SaveChipsToDisk(string source)
        {
            if(BotMain._debug)
            {
                Console.WriteLine("::SaveChipsToDisk from " + source + " with " + (Bot.DiceBot.ChipPiles == null ? "NULL chip piles" : Bot.DiceBot.ChipPiles.Count() + " chip piles"));
            }
            Utils.WriteToFileAsData(Bot.DiceBot.ChipPiles, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedChipsFileName));
        }

        public void SaveCouponsToDisk()
        {
            Utils.WriteToFileAsData(Bot.ChipsCoupons, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.ChipsCouponsFileName));
        }

        public void SaveChannelSettingsToDisk()
        {
            Utils.WriteToFileAsData(Bot.SavedChannelSettings, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.ChannelSettingsFileName));
        }

        public void SaveVcChipOrdersToDisk()
        {
            Utils.WriteToFileAsData(Bot.DiceBot.VcChipOrders, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.VcChipOrdersFileName));
        }

        public void SaveCustomDecksToDisk()
        {
            Utils.WriteToFileAsData(Bot.SavedDecks, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedDecksFileName));
        }

        public void SaveCustomTablesToDisk()
        {
            Utils.WriteToFileAsData(Bot.SavedTables, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedTablesFileName));
        }

        public void SaveAccountSettingsToDisk()
        {
            Utils.WriteToFileAsData(Bot.AccountSettings, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.AccountSettingsFileName));
        }
    }

    public enum CommandLockCategory
    {
        NONE,
        SavedTables,
        SavedChannels,
        ChannelDecks,
        ChannelScores,
        CharacterInventories,
        RPGData,
        ChannelOpsRequest
    }

    public class UserGeneratedCommand
    {
        public string characterName;
        public string channel;
        public string guild; //for discord messages
        public string commandName;
        public string channelOpRequested;
        public string[] rawTerms;
        public string[] terms;
        public string[] ops;
    }
}
