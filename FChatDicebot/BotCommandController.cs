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

namespace FChatDicebot
{
    public class BotCommandController
    {
        private BotMain Bot;

        public List<ChatBotCommand> BotCommands;

        private object SavedTablesFileLock = new object();
        private object SavedChannelsFileLock = new object();
        private object ChannelDecksLock = new object();
        private object ChannelScoresLock = new object();

        public BotCommandController(BotMain sourceBot)
        {
            Bot = sourceBot;
            BotCommands = new List<ChatBotCommand>();

            LoadChatBotCommands();
        }

        private void LoadChatBotCommands()
        {
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

            foreach(Type thisType in allTypes)
            {
                if (thisType.Namespace == "FChatDicebot.BotCommands")
                {
                    object obj = Activator.CreateInstance(thisType);

                    //dynamic is assumed to be any type you tell it, not validated at compile time. can crash runtime if methods not found.
                    dynamic changedObj = Convert.ChangeType(obj, thisType);

                    if (changedObj.GetType().BaseType == typeof(ChatBotCommand))
                        BotCommands.Add(changedObj);
                }
            }
        }

        public void RunChatBotCommand(UserGeneratedCommand command)
        {
            ChatBotCommand c = BotCommands.FirstOrDefault(a => a.Name == command.commandName);

            if (c == null)
            {
                if(string.IsNullOrEmpty(command.channel))
                {
                    Bot.SendPrivateMessage("Failed: That is not a valid command. Use !help for a list of commands.", command.characterName);
                }
                return;
            }

            string[] terms = Utils.LowercaseStrings(command.rawTerms);
            terms = Utils.TrimStringsAndRemoveEmpty(terms);
            terms = Utils.FixComparators(terms);
            command.terms = terms;

            object lockObject = GetObjectToLock(c.LockCategory);
            bool characterIsAdmin = Utils.IsCharacterAdmin(Bot.AccountSettings.AdminCharacters, command.characterName);

            bool fromChannel = MessageCameFromChannel(command.channel);
            if (MonDB.getProfile(command.characterName) == null && command.commandName != "joinchateau")
            {
                Bot.SendPrivateMessage(ChateauInteractionHandler.notRegisteredText(), command.characterName);
            }
            else if (fromChannel || !c.RequireChannel)
            {
                if (characterIsAdmin || !c.RequireBotAdmin)
                {
                    if (command.ops == null && c.RequireChannelAdmin && !characterIsAdmin && fromChannel)
                    {
                        Bot.RequestChannelOpListAndQueueFurtherRequest(command);
                    }
                    else if ((!c.RequireChannelAdmin) || characterIsAdmin ||
                        (!fromChannel && !c.RequireChannel) ||
                        (command.ops != null && command.ops.Contains(command.characterName)))
                    {
                        if (lockObject != null)
                        {
                            lock (lockObject)
                            {
                                c.Run(Bot, this, command.rawTerms, terms, command.characterName, command.channel, command);
                            }
                        }
                        else
                        {
                            c.Run(Bot, this, command.rawTerms, terms, command.characterName, command.channel, command);
                        }
                    }
                    else
                    {
                        Bot.SendMessageInChannel("Failed: " + Utils.GetCharacterUserTags(command.characterName) + ", you need to be a channel op to use this command (" + command.commandName + ").", command.channel);
                    }
                }
                else
                {
                    Bot.SendMessageInChannel("Failed: You do not have authorization to complete this command.", command.channel);
                }
            }
            else if(!fromChannel && c.RequireChannel)
            {
                Bot.SendPrivateMessage("Failed: This command requires a channel to use.", command.characterName);
            }
        }

        public bool MessageCameFromChannel(string channel)
        {
            return !string.IsNullOrEmpty(channel);
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

        public SlotsSetting GetDefaultSlotsSetting(bool defaultFruit)
        {
            if (defaultFruit)
                return Bot.SavedSlots.FirstOrDefault(a => a.SlotsId == "Fruits").SlotsSetting;
            else
                return Bot.SavedSlots.FirstOrDefault(a => a.SlotsId == "Bondage").SlotsSetting; //a.DefaultSlots not really used right now
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

        public IGame GetGameTypeForCommand(DiceBot diceBot, string channel, string[] terms, out string errorString)
        {
            errorString = "";
            IGame gametype = GetGameTypeFromCommandTerms(diceBot, terms);

            if (gametype == null)
            {
                //check game sessions and see if this channel has a session for anything
                var gamesPresent = diceBot.GameSessions.Where(a => a.ChannelId == channel);
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

        public string GetInteractionTypeFromCommandTerms(string[] terms)
        {
            // Get the first plain text term that's not part of special tags
            // This is typically the term after the command and username
            if (terms != null && terms.Length >= 3)
            {
                bool skipNext = false;
                bool inUserTag = false;
                bool inQuotes = false;
                bool inEIcon = false;
                int termsSeen = 0;

                foreach (string term in terms)
                {
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

                    // Skip command name (first term) and any terms inside special tags
                    if (termsSeen == 0)
                    {
                        termsSeen++;
                        continue;
                    }

                    // If we're not in any special tags, this is our interaction type
                    if (!inUserTag && !inQuotes && !inEIcon)
                    {
                        return term.ToLower();
                    }
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
    }

    public enum CommandLockCategory
    {
        NONE,
        SavedTables,
        SavedChannels,
        ChannelDecks,
        ChannelScores,
        CharacterInventories
    }

    public class UserGeneratedCommand
    {
        public string characterName;
        public string channel;
        public string commandName;
        public string[] rawTerms;
        public string[] terms;
        public string[] ops;
    }
}
