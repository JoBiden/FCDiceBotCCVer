using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.Model;

namespace FChatDicebot.DiceFunctions
{
    public class DungeonDelve : IGame
    {
        public string CommandsList = "(!gc inventory, !gc status, !gc buyitem X, !gc continue, !gc leave, !gc giveitem X [playername], !gc pass, !gc dropitem X, !gc useitem X, !gc unlock X, !gc setshowdescriptions [on/off])";
        public const int BaseRunAnte = 1000;
        public const int MaximumInventoryItems = 7;
        public const int BaseMonsterStrengthBonus = 2;
        public const int LevelupXpRequired = 100;
        public const int XpPerMonsterBattle = 50;
        public const int XpPerTrap = 25;
        public string ExplorationString = "You may now choose to !gc continue, !gc leave. [sub]You can also use item commands: !gc useitem x, !gc giveitem x [playername], !gc dropitem x, !gc examineitem x[/sub]";

        public string GetGameName()
        {
            return "DungeonDelve";
        }

        public int GetMaxPlayers()
        {
            return 4;
        }

        public int GetMinPlayers()
        {
            return 1;
        }

        public bool AllowAnte()
        {
            return true;
        }

        public bool UsesFlatAnte()
        {
            return true;
        }

        public bool KeepSessionDefault()
        {
            return false;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 0;
        }

        public string GetGameHelp()
        {
            string thisGameCommands = "SetAnte #, continue, leave, pass, shop, scout, gamble, buyitem, giveitem, useitem, dropitem" +
                    "\n(as current player only): attack, slam, lightattack, pass, forfeit";
            string thisGameStartupOptions = "# (set ante to #)" +
                "\nThe default rules are: ante 1000";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return TextFormat.Emoji("dbdungeon1") + TextFormat.Emoji("dbdungeon2");
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            string playersList = "(Players have not started the game yet)";
            if (!(session.DungeonDelveData.DungeonDelvePlayers == null || session.DungeonDelveData.DungeonDelvePlayers.Count() == 0))
            {
                string allPlayers = "";
                foreach (DungeonDelvePlayer p in session.DungeonDelveData.DungeonDelvePlayers)
                {
                    if (!string.IsNullOrEmpty(allPlayers))
                        allPlayers += "\n";

                    allPlayers += p.Print();
                }
                playersList = allPlayers;
            }
            return playersList + "\n" + GetRulesText(session);
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            messageString = "";
            if (session.DungeonDelveData == null)
                session.DungeonDelveData = new DungeonDelveData(botMain.DiceBot);

            if (!session.DungeonDelveData.RulesAssigned)
            {
                session.DungeonDelveData.RulesAssigned = true;

                if (ante == 0 || terms.Contains("normalante") && !terms.Contains("0"))
                {
                    ante = BaseRunAnte;
                    session.Ante = BaseRunAnte;
                    messageString += "(ante: " + ante + ")";
                }
                else if (ante > 0)
                {
                    messageString += "(ante: " + ante + ")";
                }

                if (ante != BaseRunAnte)
                {
                    session.DungeonDelveData.RewardMultiplier = Math.Floor((((double)ante)*10) / (double) BaseRunAnte) / 10;
                    messageString += "(reward multiplier: " + session.DungeonDelveData.RewardMultiplier.ToString("F1") + ")";
                }

                if (terms.Contains("hidedescriptions"))
                {
                    session.DungeonDelveData.ShowDescriptions = false;
                    messageString += "(hide descriptions)";
                }
            }
            else
            {
                messageString += "(treasure found increased for more players)";
            }

            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            string returnString = PlayerLeavesGame(botMain, botMain.DiceBot,
                new MessageAddress() { channel = session.ChannelId, character = characterName, guild = session.GuildId }, session);

            return returnString;
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            string bettingString = "";

            session.DungeonDelveData.RewardMultiplier = Math.Floor((((double)session.Ante) * 10) / (double)BaseRunAnte) / 10;
            string rewardMultiplierString = session.DungeonDelveData.RewardMultiplier != 1? "(reward multiplier: " + session.DungeonDelveData.RewardMultiplier.ToString("F1") + ")":"";

            if (session.Ante > 0)
            {
                string outputErrorAnte = "";
                foreach(string player in session.Players)
                {
                    ChipPile playerPile = diceBot.GetChipPile(new MessageAddress(session.GetMessageAddress(), player), true);
                    if (playerPile.Chips < session.Ante)
                    {
                        if (!string.IsNullOrEmpty(outputErrorAnte))
                        {
                            outputErrorAnte += ", ";
                        }
                        outputErrorAnte = TextFormat.GetCharacterUserTags(player) + " cannot afford the ante of " + session.Ante + " " + BotMain.CurrencyPlaceholder + "s. (" + playerPile.Chips + " held)";
                    }
                }
                if (!string.IsNullOrEmpty(outputErrorAnte))
                {
                    session.State = GameState.Unstarted;
                    return "Session for DungeonDelve failed to start: " + outputErrorAnte;
                }
            }

            botMain.SendMessageInChannel("[color=yellow]A new [b]Dungeon Delve[/b] is starting...[/color]" + bettingString, session.GetMessageAddress());

            if(session.DungeonDelveData == null)
            {
                session.DungeonDelveData = new DungeonDelveData(diceBot);
            }
            else if (session.DungeonDelveData.DungeonDelvePlayers.Count > 0)
            {
                DungeonDelveData d = new DungeonDelveData(diceBot);
                d.ShowDescriptions = session.DungeonDelveData.ShowDescriptions;
                d.RewardMultiplier = session.DungeonDelveData.RewardMultiplier;
                d.RulesAssigned = session.DungeonDelveData.RulesAssigned;
                session.DungeonDelveData = d;
            }

            string playerIntrosOutput = "";
            MessageAddress relevantAddress = session.GetMessageAddress();

            foreach (string playerName in playerNames)
            {
                SavedData.CharacterData thisCharacterData = diceBot.GetCharacterData(new MessageAddress(relevantAddress, playerName), false);
                if (thisCharacterData == null)
                    return "Error: Character data not found for " + playerName;

                thisCharacterData.LastDungeonDelve = DoubleTime.GetCurrentTimestampSeconds();
                thisCharacterData.TimesDungeonDelved += 1;

                DungeonDelvePlayer p = new DungeonDelvePlayer();
                p.Name = playerName;
                p.StageName = playerName;
                p.Items = new List<DungeonDelveItem>();
                p.Eliminated = false;
                p.Active = true;
                p.Cursed = false;

                if(!string.IsNullOrEmpty(playerIntrosOutput))
                {
                    playerIntrosOutput += "\n";
                }

                playerIntrosOutput += TextFormat.GetCharacterIconTags(playerName) + " [b]begins their adventure[/b]! ";

                if (session.Ante > 0)
                {

                    MessageAddress currentAddress = new MessageAddress(session.GetMessageAddress(), playerName);
                    ChipPile playerPile = diceBot.GetChipPile(currentAddress, true);
                    playerPile.Chips -= session.Ante;
                    bettingString = " They paid " + session.Ante + " " + BotMain.CurrencyPlaceholder + "s to enter the dungeon delve.";

                    //betstring = diceBot.BetChips(new MessageAddress(session.GetMessageAddress(), playerName), session.Ante, false) + "\n";
                }

                playerIntrosOutput += bettingString += "\n";

                session.DungeonDelveData.DungeonDelvePlayers.Add(p);
            }

            bettingString = "";
            if (!string.IsNullOrEmpty(rewardMultiplierString))
            {
                bettingString = "\n" + rewardMultiplierString;
            }

            session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.StartingShop;

            string outputString = "" + playerIntrosOutput + bettingString;

            outputString += "\n" + "[i]Spend additional " + BotMain.CurrencyPlaceholder + "s to purchase items with !gc buy #, or use !gc continue to start the dungeon.[/i] [sub]You can look at your inventory with !gc inventory and drop items with !gc dropitem X[/sub]";
            outputString += "\n" + DisplayShopItems(botMain, session, true);
            session.State = DiceFunctions.GameState.GameInProgress;

            botMain.BotCommandController.SaveCharacterDataToDisk();
            botMain.BotCommandController.SaveChipsToDisk("startdungeondelve");
            return outputString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public string DisplayShopItems(BotMain botMain, GameSession session, bool spendChips)
        {
            string shopname = "town";
            string shopicon = "dbshop";

            int index = 1;
            List<DungeonDelveItem> relevantItems = session.DungeonDelveData.ItemsRepository.ShopItems;
            if (session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.CampsiteShop)
            {
                relevantItems = session.DungeonDelveData.CampShopItems;
                shopname = "camp";
                shopicon = "dbcamp";
            }

            string msg = "[eicon]" + shopicon + "[/eicon] [color=yellow]The " + shopname + " shop contains these items[/color]";
            foreach (DungeonDelveItem item in relevantItems)
            {
                msg += "\n" + index + ": " + item.Print(true, spendChips, session.DungeonDelveData);
                index++;
            }
            return msg;
            //botMain.SendMessageInChannel(msg, session.GetMessageAddress());
        }

        public string PlayerContinue(BotMain botMain, DiceBot diceBot, GameSession session, MessageAddress playerToMoveOn)
        {
            string outputString = "";

            DungeonDelvePlayer player = GetDungeonDelvePlayerByName(session, playerToMoveOn.character);
            if (player.ReadyToContinue)
            { outputString += ""; }
            else
            {
                player.ReadyToContinue = true;
                outputString += TextFormat.GetCharacterUserTags(player.Name) + " continues on...";
            }

            if (session.DungeonDelveData.AllPlayersReadyToContinue())
            {
                outputString += "\n...continued to the next floor.";

                session.DungeonDelveData.CurrentFloor++;
                DungeonDelveRoomType nextRoomType = session.DungeonDelveData.CurrentRoomType();

                outputString += "\n" + StartExploreRoom(botMain, diceBot, session);
            }
            else
            {
                outputString += session.DungeonDelveData.GetAllPlayersNotReady();
            }
            return outputString;
        }

        public string PlayerLeavesGame(BotMain botMain, DiceBot diceBot, MessageAddress address, GameSession session)
        {
            DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);
            if (play == null || !play.Active || play.Eliminated)
                return "";

            int currentTreasure = session.DungeonDelveData.TreasureFound;
            int activePlayers = session.DungeonDelveData.DungeonDelvePlayers.Count(a => a.Active && !a.Eliminated);
            int thisPlayerShare = currentTreasure / activePlayers;
            ChipPile playerPile = botMain.DiceBot.GetChipPile(address, false);
            play.Active = false;
            string convertedToChipsNote = "";
            string extraPlayersNote = activePlayers > 1 ? " They claim their share of the loot" : " They claim their loot";
            int lootTreasure = play.Items == null ? 0 : play.Items.Where(a => a.ItemType == DungeonDelveItemType.Loot).Sum(b => b.ItemPrice);
            string lootTreasureString = "";
            if (lootTreasure > 0)
                lootTreasureString = " (+" + lootTreasure + " gold from loot carried)";

            int chipsAdded = (int)Math.Floor((thisPlayerShare + lootTreasure) * session.DungeonDelveData.RewardMultiplier);
            if (session.DungeonDelveData.RewardMultiplier != 1)
            {
                convertedToChipsNote = " (x" + session.DungeonDelveData.RewardMultiplier.ToString("F1") + " = " + chipsAdded + " " + BotMain.CurrencyPlaceholderCapital + "s)";
            }
            else
            {
                convertedToChipsNote = " (" + chipsAdded + " " + BotMain.CurrencyPlaceholderCapital + "s)";
            }

            session.DungeonDelveData.TreasureFound -= thisPlayerShare;

            diceBot.AddChips(address, chipsAdded, false);

            botMain.BotCommandController.SaveChipsToDisk("leavegamedungeondelve");

            string drops = "";
            string extraEndString = "";
            if (session.DungeonDelveData.NumberOfLivingPlayers() == 0)
            {
                drops = "";
                extraEndString = "\n" + GameOver(diceBot, session, false);
            }
            else
            {
                drops = session.DungeonDelveData.DropAllItems(play, true);
                if (!string.IsNullOrEmpty(drops))
                    drops = "\n" + drops;

                if (session.DungeonDelveData.AllPlayersReadyToContinue())
                {
                    DungeonDelvePlayer p = session.DungeonDelveData.DungeonDelvePlayers.FirstOrDefault(a => a.ReadyToContinue && a.Active && !a.Eliminated);
                    drops += "\n" + PlayerContinue(botMain, botMain.DiceBot, session, new MessageAddress(address, p.Name));
                }
                else
                {
                    extraEndString += "\n" + session.DungeonDelveData.GetAllPlayersNotReady();
                }
            }

            return TextFormat.GetCharacterUserTags(play.Name) + " has chosen to leave the dungeon." + extraPlayersNote + " (" + thisPlayerShare + " gold)" + lootTreasureString + convertedToChipsNote + drops + extraEndString;
        }

        public string StartExploreRoom(BotMain botMain, DiceBot diceBot, GameSession session)
        {
            int floor = session.DungeonDelveData.CurrentFloor;
            session.DungeonDelveData.ResetPlayerContinueFlags();
            string result = "You enter [b]floor " + floor + "[/b]... ";
            DungeonDelveRoomType nextRoomType = session.DungeonDelveData.CurrentRoomType();
            List<ReactionItemPlayer> allReactionPlayers = null;// List<DungeonDelvePlayer> allReactionPlayers = null;
            switch (nextRoomType)
            {
                case DungeonDelveRoomType.EmptyRoom:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.EmptyRoomReaction;
                    result += "This room is [b]EMPTY.[/b]";
                    allReactionPlayers = session.DungeonDelveData.GetPlayersWithReaction(DungeonDelveItemActivationTriggerType.EmptyRoom);
                    break;
                case DungeonDelveRoomType.Monster:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.CombatReaction;
                    result += "This room contains a [b]MONSTER![/b]";

                    DungeonDelveMonster monster = session.DungeonDelveData.MonstersRepository.GetRandomMonsterForFloor(diceBot.random, floor);
                    session.DungeonDelveData.CurrentMonster = monster;

                    result += "\n" + monster.Print(diceBot, session.DungeonDelveData);

                    //get monster type
                    allReactionPlayers = session.DungeonDelveData.GetPlayersWithReaction(DungeonDelveItemActivationTriggerType.MonsterRoom);
                    break;
                case DungeonDelveRoomType.Trap:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.TrapReaction;
                    result += "This room contains a [b]TRAP![/b]";
                    var allItems = session.DungeonDelveData.DungeonDelvePlayers.SelectMany(a => a.Items);
                    bool activeMap = allItems.Where(a => a.Id == DungeonDelveItems.DungeonMap).Count(a => a.ItemLevel >= session.DungeonDelveData.CurrentFloor - 5) > 0;

                    if (activeMap)
                        allReactionPlayers = null;
                    else
                        allReactionPlayers = session.DungeonDelveData.GetPlayersWithReaction(DungeonDelveItemActivationTriggerType.TrapRoom);
                    break;
                case DungeonDelveRoomType.Camp:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Campsite;
                    result += "This room contains a [b]CAMP.[/b]";
                    break;
                case DungeonDelveRoomType.TreasureRoom:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Looting;
                    result += "This room is a [b]TREASURE ROOM![/b]";
                    break;
            }
            if (allReactionPlayers == null || allReactionPlayers.Count() == 0)
            {
                return result + "\n" + ExploreRoomAfterReactions(botMain, diceBot, session);
            }
            else
            {
                string playerPluralNoun = "You";
                if (allReactionPlayers.Count() > 1)
                {
                    playerPluralNoun = "Players";
                    //result += "\nPaused: " + string.Join(", ",
                    //    allReactionPlayers.Select(p =>
                    //        TextFormat.GetCharacterUserTags(p.Name))) + " have items that can be used. Use '!gc pass' or '!gc useitem X' to continue";
                }
                string reactionPrintout = "";
                foreach (ReactionItemPlayer itemPlayer in allReactionPlayers)
                {
                    if (!string.IsNullOrEmpty(reactionPrintout))
                    {
                        reactionPrintout += ", ";
                    }
                    reactionPrintout += itemPlayer.Print();
                }
                reactionPrintout = "\nPaused: " + playerPluralNoun + " have items that can be used. Use !gc pass or !gc useitem X to continue.\n" + reactionPrintout;

                foreach (DungeonDelvePlayer play in allReactionPlayers.Select(a => a.Player))
                {
                    if(play != null)
                        play.HasReactionReady = true;
                }
                return result + reactionPrintout;
            }
        }

        public string ExploreRoomAfterReactions(BotMain botMain, DiceBot diceBot, GameSession session)
        {
            int floor = session.DungeonDelveData.CurrentFloor;
            DungeonDelveRoomType nextRoomType = session.DungeonDelveData.CurrentRoomType();
            string result = "";
            string roomEicon = "dbemptyroom";
            bool continueExploration = false;
            bool bypass = session.DungeonDelveData.DungeonDelvePlayers.Count(a => a.ActivatedBypass) > 0;
            switch (nextRoomType)
            {
                case DungeonDelveRoomType.EmptyRoom:
                    roomEicon = "dbemptyroom";
                    result = "Nothing happens in the empty room.";
                    continueExploration = true;
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Exploration;
                    break;
                case DungeonDelveRoomType.Monster:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Combat;
                    DungeonDelveMonster monster = session.DungeonDelveData.CurrentMonster;
                    roomEicon = monster.EiconName;

                    bool victory = false;
                    if (bypass)
                    {
                        result += "[b]Battle against " + monster.Name + " was skipped![/b]";
                        continueExploration = true;
                        victory = true;
                    }
                    else
                    {
                        result += "[b]Battle begins against " + monster.Name + "![/b] (Strength " + monster.Strength + ")";

                        int fightCount = 1;
                        if (monster.Id == DungeonDelveMonsters.GangOfOrcs)
                            fightCount = 5;

                        for (int i = 0; i < fightCount; i++)
                        {
                            result += ResolveFight(botMain, diceBot, monster, session, out continueExploration, out victory);
                            if (!victory || !continueExploration)
                                break;
                        }
                    }//end no bypass

                    if (victory && continueExploration)//loot and xp afterwards
                    {
                        result += " " + session.DungeonDelveData.AwardXp(XpPerMonsterBattle);
                        result += "\n" + RollLoot(botMain, diceBot, session, false, 0, monster.TreasureMultiplier);
                    }
                    break;
                case DungeonDelveRoomType.Trap:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Trap;
                    int diceRoll = diceBot.random.Next(10);

                    var allItems = session.DungeonDelveData.DungeonDelvePlayers.SelectMany(a => a.Items);
                    bool activeMap = allItems.Where(a => a.Id == DungeonDelveItems.DungeonMap).Count(a => a.ItemLevel >= session.DungeonDelveData.CurrentFloor - 5) > 0;

                    DungeonDelveTrap randomTrap = session.DungeonDelveData.TrapsRepository.GetRandomTrap(diceBot.random, floor);

                    bool avoidTrap = diceRoll >= 5;// false;
                    roomEicon = "";
                    bool victoryVsTrap = true;
                    result += randomTrap.Print() + "\n";
                    if (bypass)
                    {
                        result += "[b]Trap bypassed![/b]";
                        continueExploration = true;
                    }
                    else if (activeMap)
                    {
                        result += "[b]Trap bypassed by map![/b]";
                        continueExploration = true;
                    }
                    else if (avoidTrap)
                    {
                        result += "[i]Luckily, you have avoided the trap.[/i]";
                        continueExploration = true;
                    }
                    else
                    {
                        List<DungeonDelvePlayer> activePartyTrap = session.DungeonDelveData.DungeonDelvePlayers.Where(p => p.Active && !p.Eliminated).ToList();

                        bool ignoreGameOverCheckHere = false;
                        victoryVsTrap = false;

                        switch (randomTrap.Id)
                        {
                            case DungeonDelveTraps.CrushingWall:
                                result += "The crushing wall trap activated! ";
                                result += "\n" + session.DungeonDelveData.EliminateAllPlayers("PLAYER was crushed to death!");
                                continueExploration = false;
                                break;
                            case DungeonDelveTraps.PoisonGas:
                                result += "The gas trap has poisoned you! ";
                                string poisonResult = session.DungeonDelveData.ProcessPoisonForPlayers(diceBot, activePartyTrap);
                                result += poisonResult;
                                continueExploration = false;
                                break;
                            case DungeonDelveTraps.Acid:
                                result += "The acid trap has destroyed your items! ";
                                result += RemoveRandomItems(diceBot, activePartyTrap, "destroyed");
                                continueExploration = true;
                                break;
                            case DungeonDelveTraps.Pit:
                                int diceRoll2 = diceBot.random.Next(10) + 1;
                                session.DungeonDelveData.CurrentFloor += diceRoll2;
                                if (session.DungeonDelveData.CurrentFloor >= 200)
                                {
                                    session.DungeonDelveData.CurrentFloor = 200;
                                }
                                result += "You have fallen down a pit trap! You advance " + diceRoll2 + " floors to floor " + session.DungeonDelveData.CurrentFloor + "!";
                                result += "\n" + StartExploreRoom(botMain, diceBot, session);

                                continueExploration = false;
                                ignoreGameOverCheckHere = true;
                                break;
                            case DungeonDelveTraps.Disintegration:
                                result += "The disintegration trap has destroyed your treasure! ";
                                int diceRoll3 = diceBot.random.Next(10) + 1 + session.DungeonDelveData.CurrentFloor;
                                int treasurelost = diceRoll3 * 100;
                                int currentTreasure = session.DungeonDelveData.TreasureFound;
                                int actualLoss = Math.Min(treasurelost, currentTreasure);
                                session.DungeonDelveData.TreasureFound -= actualLoss;
                                result += "\n[color=orange]You lost " + actualLoss + " gold.[/color]";
                                continueExploration = true;
                                break;
                            default:
                                result += "You have avoided the trap.";
                                continueExploration = true;
                                break;
                        }

                        if (!ignoreGameOverCheckHere)
                        {
                            if (session.DungeonDelveData.DungeonDelvePlayers.Count(a => a.Active && !a.Eliminated) == 0)
                            {
                                result += "\n" + GameOver(diceBot, session, true);
                            }
                            else
                            {
                                continueExploration = true;
                            }
                        }
                    }
                    if (victoryVsTrap)
                    {
                        result += " " + session.DungeonDelveData.AwardXp(XpPerTrap);
                    }
                    break;
                case DungeonDelveRoomType.Camp:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Campsite;
                    roomEicon = "dbcamp";
                    result += "Options: continue, gamble, scout, shop";
                    break;
                case DungeonDelveRoomType.TreasureRoom:
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Exploration;
                    roomEicon = "dbtreasureroom";
                    result += RollLoot(botMain, diceBot, session, false, 0, 1);
                    continueExploration = true;
                    break;
            }

            if (continueExploration)
            {
                result += "\n" + ExplorationString;
                session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Exploration;
            }
            if (!string.IsNullOrEmpty(roomEicon))
                roomEicon = "[eicon]" + roomEicon + "[/eicon] ";
            result = roomEicon + result;
            return result;
        }

        public string RemoveRandomItems(DiceBot diceBot, List<DungeonDelvePlayer> activeParty, string removeVerb)
        {
            string result = "";
            foreach (DungeonDelvePlayer p in activeParty)
            {
                if (p.Items != null && p.Items.Count > 0)
                {
                    int itemIdx = diceBot.random.Next(p.Items.Count);
                    DungeonDelveItem stolenItem = p.Items[itemIdx];
                    p.Items.RemoveAt(itemIdx);
                    result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " had their [b]" + stolenItem.Name + "[/b] " + removeVerb + "!";
                }
                else
                {
                    result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " has no items to steal.";
                }
            }
            return result;
        }

        public string GameOver(DiceBot diceBot, GameSession session, bool badEnding)
        {
            string rtn = "";
            session.State = GameState.Finished;
            rtn = badEnding? "- - - Game Over - - -" : "- - - Delve Complete - - -";
            rtn += "\n" + "Floors explored: " + session.DungeonDelveData.CurrentFloor + ", Highest Level obtained: " + session.DungeonDelveData.DungeonDelvePlayers.Max(a=> a.Level) + ", Items held: " + session.DungeonDelveData.DungeonDelvePlayers.SelectMany(a => a.Items).Count()  + ", Total Treasure found: " + session.DungeonDelveData.TotalTreasureFound + ".";
            return rtn;
        }

        public string RollLoot(BotMain botMain, DiceBot diceBot, GameSession session, bool treasureChestContents, int treasureChestLevel = 0, double lootMonsterMultiplier = 1)
        {
            int floor = treasureChestLevel > 0? treasureChestLevel : session.DungeonDelveData.CurrentFloor;
            string result = treasureChestContents? "You open a treasure chest! " : "You look for treasure on floor " + floor + "... ";
            DungeonDelveRoomType thisRoomType = session.DungeonDelveData.AllFloors[floor];

            int baseTreasureFound = 200 + 50 * floor;// * lootMonsterMultiplier);
            int halfTreasureFound = 100 + 25 * floor;
            int treasureFound = 0;
            int bonusTreasure = 0;
            int midasBonusTreasure = 0;
            if (!treasureChestContents)
            {
                if (session.DungeonDelveData.DungeonDelvePlayers.Count() > 1)
                {
                    double multi = 1 + (session.DungeonDelveData.DungeonDelvePlayers.Count() - 1) * 0.5;
                    baseTreasureFound = (int)Math.Floor(baseTreasureFound * multi);
                    halfTreasureFound = (int)Math.Floor(halfTreasureFound * multi);
                }
                if (thisRoomType == DungeonDelveRoomType.TreasureRoom)
                    bonusTreasure = baseTreasureFound;
                if (lootMonsterMultiplier > 1)
                    bonusTreasure = (int)Math.Floor(baseTreasureFound * (lootMonsterMultiplier - 1));
                int midasCount = session.DungeonDelveData.DungeonDelvePlayers.SelectMany(a => a.Items).Count(a => a.Id == DungeonDelveItems.HandOfMidas);
                midasBonusTreasure = (!treasureChestContents && thisRoomType == DungeonDelveRoomType.Monster) ? midasCount * 500 : 0;
            }

            Dictionary<int, List<DungeonDelveItem>> itemsPerPlayer = new Dictionary<int, List<DungeonDelveItem>>();
            //by player index
            List<DungeonDelveItem> itemsFound = new List<DungeonDelveItem>();
            DungeonDelveItems repo = session.DungeonDelveData.ItemsRepository;

            treasureFound = baseTreasureFound;
            int treasureTypeRoll = diceBot.random.Next(6);
            if (!treasureChestContents)
            {
                int itemsMultiple = 1;
                for (int i = 1; i < session.DungeonDelveData.NumberOfLivingPlayers(); i++)//50% chance for every additional player to make another item
                {
                    itemsMultiple += Utils.Percentile(diceBot.random, 50) ? 1 : 0;
                }
                switch (treasureTypeRoll)
                {
                    case 0:
                        treasureFound = halfTreasureFound;
                        break;
                    case 1:
                        //no change
                        break;
                    case 2:
                    case 3://treasure chests
                        treasureFound = 0;
                        for (int i = 0; i < itemsMultiple; i++)
                        {
                            DungeonDelveItem chest = repo.GetItem(DungeonDelveItems.TreasureChest);
                            chest.ItemLevel = floor;
                            itemsFound.Add(chest);
                        }
                        break;
                    case 4://key
                        treasureFound = halfTreasureFound;
                        for (int i = 0; i < itemsMultiple; i++)
                        {
                            DungeonDelveItem key = repo.GetItem(DungeonDelveItems.Key);
                            itemsFound.Add(key);
                        }
                        break;
                    case 5:
                        treasureFound = halfTreasureFound;
                        for (int i = 0; i < itemsMultiple; i++)
                        {
                            List<DungeonDelveItem> itemsAwarded = RollRandomItems(botMain, diceBot, session);
                            itemsFound.AddRange(itemsAwarded);
                        }
                        break;
                }
            }
            else //treasure chest contents not multiplied by players
            {
                switch (treasureTypeRoll)
                {
                    case 0:
                        //no change
                        break;
                    case 1:
                    case 2:
                        bonusTreasure = treasureFound;
                        break;
                    case 3:
                    case 4:
                        treasureFound = halfTreasureFound;
                        //# of random items...
                        List<DungeonDelveItem> itemsAwarded = RollRandomItems(botMain, diceBot, session);
                        DungeonDelveItem chest = repo.GetItem(DungeonDelveItems.TreasureChest); //chest gold not multiplied...
                        chest.ItemLevel = floor;
                        itemsFound.AddRange(itemsAwarded);
                        break;
                    case 5:
                        treasureFound = 0;
                        List<DungeonDelveItem> itemsAwarded2 = RollRandomItems(botMain, diceBot, session);
                        List<DungeonDelveItem> itemsAwarded3 = RollRandomItems(botMain, diceBot, session);
                        itemsFound.AddRange(itemsAwarded2);
                        itemsFound.AddRange(itemsAwarded3);
                        break;
                }
            }

            int totalTreasureAward = treasureFound + bonusTreasure + midasBonusTreasure;
            session.DungeonDelveData.AddTreasureFound(totalTreasureAward);

            //result += "You found " + totalTreasureAward + " gold.";
            string goldString = "You found [color=yellow]" + totalTreasureAward + " gold[/color].";
            if (totalTreasureAward > 0)
                result += goldString;
            if (itemsFound.Count() == 0 && totalTreasureAward == 0)
                result += "You didn't find any treasure.";

            //divide items found among players
            string totalItemsString = "";
            foreach (DungeonDelveItem item in itemsFound)
            {
                DungeonDelvePlayer p = session.DungeonDelveData.GetNextTreasurePlayer();
                p.Items.Add(item);
                totalItemsString += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " found a " + item.Print(false, false, session.DungeonDelveData);
            }

            string revealText = "";
            //dungeon map show rooms
            if (itemsFound.Count(a => a.Id == DungeonDelveItems.DungeonMap) > 0)
            {
                revealText = DungeonMapRevealText(session);
            }

            return result + totalItemsString + revealText;
        }

        public List<DungeonDelveItem> RollRandomItems(BotMain botMain, DiceBot diceBot, GameSession session)
        {
            List<DungeonDelveItem> itemReward = new List<DungeonDelveItem>();
            bool commonItems = Utils.Percentile(diceBot.random, 40);
            double randomSeed = diceBot.random.NextDouble();
            DungeonDelveItems repo = session.DungeonDelveData.ItemsRepository;
            if (commonItems)
            {
                //for (int i = 0; i < 5; i++)
                //{
                int itemIndex = diceBot.random.Next(session.DungeonDelveData.ItemsRepository.CommonDropItems.Count());
                DungeonDelveItem item = repo.GetItem(session.DungeonDelveData.ItemsRepository.CommonDropItems[itemIndex].Id);

                if (item.Id == DungeonDelveItems.EmeraldGem && session.DungeonDelveData.CurrentFloor > 6)
                    item = session.DungeonDelveData.ItemsRepository.GetItem(DungeonDelveItems.DiamondGem);

                itemReward.Add(item);

                itemIndex = diceBot.random.Next(session.DungeonDelveData.ItemsRepository.CommonDropItems.Count());
                DungeonDelveItem item2 = repo.GetItem(session.DungeonDelveData.ItemsRepository.CommonDropItems[itemIndex].Id);
                itemReward.Add(item2);
            }
            else
            {
                //for (int i = 0; i < 5; i++)
                //{
                int itemIndex = diceBot.random.Next(session.DungeonDelveData.ItemsRepository.DropItems.Count());

                DungeonDelveItem item = repo.GetItem(session.DungeonDelveData.ItemsRepository.DropItems[itemIndex].Id);
                itemReward.Add(item);
                if (item.Id == DungeonDelveItems.DiamondGem) //bonus emerald gem when you roll a diamond
                    itemReward.Add(session.DungeonDelveData.ItemsRepository.GetItem(DungeonDelveItems.EmeraldGem));
                //}
                //DungeonDelveItem item3 = repo.GetItem(DungeonDelveItems.AnkhOfReincarnation);
                //itemReward.Add(item3);



                //if (randomSeed < .15)
                //{
                //    DungeonDelveItem item = repo.GetItem(DungeonDelveItems.DungeonMap);
                //    item.ItemLevel = session.DungeonDelveData.CurrentFloor;
                //    itemReward.Add(item);
                //}
                //else if (randomSeed < .30)
                //    itemReward.Add(repo.GetItem(DungeonDelveItems.TrickDice));
                //else if (randomSeed < .45)
                //    itemReward.Add(repo.GetItem(DungeonDelveItems.BowOfMonsterSlaying));
                //else if (randomSeed < .6)
                //    itemReward.Add(repo.GetItem(DungeonDelveItems.SoulGem));
                //else if (randomSeed < .75)
                //    itemReward.Add(repo.GetItem(DungeonDelveItems.AnkhOfReincarnation));
                //else if (randomSeed < .9)
                //    itemReward.Add(repo.GetItem(DungeonDelveItems.ImperviousArmor));
                //else
                //    itemReward.Add(repo.GetItem(DungeonDelveItems.HandOfMidas));
            }

            return itemReward;
        }
        //public string GetCurrentActivePlayerName(GameSession session)
        //{
        //    if (session.SlamRollData.SlamRollPlayers == null || session.SlamRollData.CurrentPlayerIndex < 0 || session.SlamRollData.CurrentPlayerIndex > session.SlamRollData.SlamRollPlayers.Count - 1)
        //        return "invalid data";

        //    return session.SlamRollData.SlamRollPlayers[session.SlamRollData.CurrentPlayerIndex].Name;
        //}

        public string ResolveFight(BotMain botMain, DiceBot diceBot, DungeonDelveMonster monster, GameSession session, out bool continueExploration, out bool victory)
        {
            string result = "";
            continueExploration = false;
            victory = false;
            int bestRollDifference = 999;
            List<DungeonDelvePlayer> rolledPlayers = new List<DungeonDelvePlayer>();

            int playerCount = session.DungeonDelveData.NumberOfLivingPlayers();

            foreach (DungeonDelvePlayer p in session.DungeonDelveData.DungeonDelvePlayers)
            {
                if (p.Active && !p.Eliminated)
                {
                    if (p.ActivatedBypass)
                    {
                        p.CurrentCombatTotal = monster.Strength + 1; // auto-win
                        result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " [color=green]bypassed[/color] the combat!";
                        victory = true;
                        rolledPlayers.Add(p);
                        bestRollDifference = Math.Min(bestRollDifference, monster.Strength - p.CurrentCombatTotal);
                        continue;
                    }
                    else
                    {
                        bool isHarpy = monster.Id == DungeonDelveMonsters.Harpy;
                        CombatDiceRoll diceBonus = p.GetCombatBonus(monster.Id == DungeonDelveMonsters.Wraith, isHarpy);
                        string rollsPrint = diceBonus.Roll(botMain, new MessageAddress() { channel = session.ChannelId, guild = session.GuildId, character = p.Name }, diceBot, 0);
                        p.CurrentCombatTotal = diceBonus.Total;

                        result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " rolls: " + rollsPrint + " + " + diceBonus.Bonus + " = [b]" + p.CurrentCombatTotal + "[/b] vs " + monster.Name + " (Str " + monster.Strength + ")";

                        bool failed = playerCount > 1? p.CurrentCombatTotal <= monster.Strength : p.CurrentCombatTotal < monster.Strength;

                        // Demon free reroll
                        if (failed && monster.Id == DungeonDelveMonsters.Demon)
                        {
                            string rerollPrint = diceBonus.Roll(botMain, new MessageAddress() { channel = session.ChannelId, guild = session.GuildId, character = p.Name }, diceBot, 0);
                            if(p.CurrentCombatTotal < diceBonus.Total)
                                p.CurrentCombatTotal = diceBonus.Total;

                            result += "\n" + "[color=orange]The Demon grants a free reroll![/color] " + TextFormat.GetCharacterUserTags(p.Name) + " rerolls: " + rerollPrint + " + " + diceBonus.Bonus + " = [b]" + diceBonus.Total + "[/b]";
                            failed = playerCount > 1 ? p.CurrentCombatTotal <= monster.Strength : p.CurrentCombatTotal < monster.Strength;
                        }

                        bestRollDifference = Math.Min(bestRollDifference, monster.Strength - p.CurrentCombatTotal);
                        rolledPlayers.Add(p);
                    }
                }
            }

            //handle all healing potion rerolls at random
            // Healing Potion reroll(s)
            bool failedCombat = playerCount > 1 ? bestRollDifference >= 0 : bestRollDifference > 0;

            if (rolledPlayers.Count() > 0 && rolledPlayers.SelectMany(a => a.Items) != null && rolledPlayers.SelectMany(a => a.Items).Count() > 0)
            {
                int potionCount = rolledPlayers.SelectMany(a => a.Items).Count(item => item.HealingPotion);
                while (failedCombat && potionCount > 0)
                {
                    var allPotionPlayers = rolledPlayers.Where(a => a.Items.Count(b => b.HealingPotion) > 0).ToList();
                    if (allPotionPlayers.Count() == 0)
                        break;
                    DungeonDelvePlayer randomPlayer = allPotionPlayers[diceBot.random.Next(allPotionPlayers.Count())];
                    if (randomPlayer == null || randomPlayer.Items == null)
                        break;

                    DungeonDelveItem potion = randomPlayer.Items.FirstOrDefault(item => item.HealingPotion);
                    if (potion != null)
                    {
                        string extraText = "";
                        if (potion.Id == DungeonDelveItems.AngelElixir)
                        {
                            if (Utils.Percentile(diceBot.random, 50))
                            {
                                randomPlayer.Items.Remove(potion);
                                extraText = " [i]The " + potion.Name + " was exhausted.[/i]";
                            }
                        }
                        else
                            randomPlayer.Items.Remove(potion);

                        CombatDiceRoll diceBonus = randomPlayer.GetCombatBonus(monster.Id == DungeonDelveMonsters.Wraith, monster.Id == DungeonDelveMonsters.Harpy);
                        //string rollsPrint = diceBonus.Roll(botMain, new MessageAddress() { channel = session.ChannelId, guild = session.GuildId, character = p.Name }, diceBot, 0);
                        //randomPlayer.CurrentCombatTotal = diceBonus.Total;

                        string rerollPrint = diceBonus.Roll(botMain, new MessageAddress() { channel = session.ChannelId, guild = session.GuildId, character = randomPlayer.Name }, diceBot, potion.CombatBonus);
                        if (randomPlayer.CurrentCombatTotal < diceBonus.Total)
                            randomPlayer.CurrentCombatTotal = diceBonus.Total;
                        result += "\n" + TextFormat.GetCharacterUserTags(randomPlayer.Name) + " [color=green]consumed a " + potion.Name + " to reroll![/color] Rerolls: " + rerollPrint + " + " + diceBonus.Bonus + " = [b]" + diceBonus.Total + "[/b]" + extraText;

                    }
                    bestRollDifference = Math.Min(bestRollDifference, monster.Strength - randomPlayer.CurrentCombatTotal);
                    potionCount = rolledPlayers.SelectMany(a => a.Items).Count(item => item.HealingPotion);
                    failedCombat = playerCount > 1 ? bestRollDifference >= 0 : bestRollDifference > 0;
                }
            }

            //

            int activePlayers = rolledPlayers.Count();
            List<DungeonDelvePlayer> activeParty = session.DungeonDelveData.DungeonDelvePlayers.Where(p => p.Active && !p.Eliminated).ToList();

            bool ignoreGameOverCheckhere = false;
            if (bestRollDifference > 0)
            {
                victory = false;
                // Party / Player Defeat
                switch (monster.OnLoss)
                {
                    case OnLossTrigger.Death:
                        result += "\n[color=red]The " + monster.Name + " has defeated you![/color]";
                        result += "\n" + session.DungeonDelveData.EliminateAllPlayers("PLAYER was [color = red]slain by the " + monster.Name + "![/color]");
                        continueExploration = false;
                        break;
                    case OnLossTrigger.Poison:
                        result += "\n[color=red]The Giant Mushroom releases toxic spores! You are poisoned![/color]";
                        string poisonRes = session.DungeonDelveData.ProcessPoisonForPlayers(diceBot, activeParty);
                        result += poisonRes;
                        continueExploration = false;
                        break;
                    case OnLossTrigger.StealItem:
                        result += "\n[color=yellow]The Horde of Gremlins swarms you and steals items![/color]";
                        result += RemoveRandomItems(diceBot, activeParty, "stolen");
                        //foreach (DungeonDelvePlayer p in activeParty)
                        //{
                        //    if (p.Items != null && p.Items.Count > 0)
                        //    {
                        //        int itemIdx = diceBot.random.Next(p.Items.Count);
                        //        DungeonDelveItem stolenItem = p.Items[itemIdx];
                        //        p.Items.RemoveAt(itemIdx);
                        //        result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " had their [b]" + stolenItem.Name + "[/b] stolen!";
                        //    }
                        //    else
                        //    {
                        //        result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " has no items to steal.";
                        //    }
                        //}
                        continueExploration = true;
                        break;
                    case OnLossTrigger.LoseMoney:
                        result += "\n[color=yellow]The Golden Golem crushes your coin purses! You lose all treasure![/color]";
                        session.DungeonDelveData.TreasureFound = 0;
                        continueExploration = true;
                        break;
                    case OnLossTrigger.LoseAllLevels:
                        result += "\n[color=yellow]The Wraith causes you unthinkable trauma! You lose all levels![/color]";
                        foreach (DungeonDelvePlayer p in activeParty)
                        {
                            p.Level = 0;
                            result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " was set to level 0!";
                        }
                        continueExploration = true;
                        break;
                    case OnLossTrigger.PitDrop:
                        int wormFloors = diceBot.random.Next(10) + 1;
                        session.DungeonDelveData.CurrentFloor += wormFloors;
                        if (session.DungeonDelveData.CurrentFloor >= 200)
                        {
                            session.DungeonDelveData.CurrentFloor = 200;
                        }
                        result += "\n[color=red]The Giant Worm defeats you and drags you down a deep pit![/color]";
                        result += "\nYou fall down " + wormFloors + " floors to floor " + session.DungeonDelveData.CurrentFloor + "!";
                        foreach (DungeonDelvePlayer p in activeParty)
                        {
                            if (p.Items != null && p.Items.Count > 0)
                            {
                                int itemIdx = diceBot.random.Next(p.Items.Count);
                                DungeonDelveItem lostItem = p.Items[itemIdx];
                                p.Items.RemoveAt(itemIdx);
                                result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " lost their [b]" + lostItem.Name + "[/b] in the fall!";
                            }
                            else
                            {
                                result += "\n" + TextFormat.GetCharacterUserTags(p.Name) + " had no items to lose.";
                            }
                        }
                        result += "\n" + StartExploreRoom(botMain, diceBot, session);
                        ignoreGameOverCheckhere = true;
                        continueExploration = false;
                        break;
                    default:
                        result += "\n[color=red]The " + monster.Name + " has defeated you![/color]";
                        result += "\n" + session.DungeonDelveData.EliminateAllPlayers("PLAYER was [color = red]slain by the " + monster.Name + "![/color]");
                        continueExploration = false;
                        break;
                }
            }
            else if (bestRollDifference == 0 && activePlayers > 1)
            {
                victory = true;
                // Party won "at a cost" - apply bad penalty strictly to lowest roll player (except Giant Worm which does nothing)
                result += "\n[color=yellow]You have defeated the monster... But at a cost.[/color]";
                DungeonDelvePlayer lowestRoll = rolledPlayers.FirstOrDefault(a => a.CurrentCombatTotal == rolledPlayers.Min(q => q.CurrentCombatTotal));

                switch (monster.OnLoss)
                {
                    case OnLossTrigger.Death:
                        result += "\n" + session.DungeonDelveData.EliminatePlayer(lowestRoll, "PLAYER has perished in the fight.");
                        break;
                    case OnLossTrigger.Poison:
                        result += "\n" + TextFormat.GetCharacterUserTags(lowestRoll.Name) + " is poisoned by the Giant Mushroom!";
                        string poisonRes = session.DungeonDelveData.ProcessPoisonForPlayers(diceBot, new List<DungeonDelvePlayer> { lowestRoll });
                        result += poisonRes;
                        break;
                    case OnLossTrigger.StealItem:
                        result += "\n" + TextFormat.GetCharacterUserTags(lowestRoll.Name) + " is targeted by the Gremlins!";
                        if (lowestRoll.Items != null && lowestRoll.Items.Count > 0)
                        {
                            int itemIdx = diceBot.random.Next(lowestRoll.Items.Count);
                            DungeonDelveItem stolenItem = lowestRoll.Items[itemIdx];
                            lowestRoll.Items.RemoveAt(itemIdx);
                            result += "\n" + TextFormat.GetCharacterUserTags(lowestRoll.Name) + " had their [b]" + stolenItem.Name + "[/b] stolen!";
                        }
                        else
                        {
                            result += "\n" + TextFormat.GetCharacterUserTags(lowestRoll.Name) + " had no items to steal.";
                        }
                        break;
                    case OnLossTrigger.LoseMoney:
                        result += "\n" + TextFormat.GetCharacterUserTags(lowestRoll.Name) + " caused the Golden Golem to crush the treasure!";
                        session.DungeonDelveData.TreasureFound = 0;
                        break;
                    case OnLossTrigger.LoseAllLevels:
                        result += "\n" + TextFormat.GetCharacterUserTags(lowestRoll.Name) + " suffered unthinkable trauma and lost all their levels!";
                        lowestRoll.Level = 0;
                        break;
                    case OnLossTrigger.PitDrop:
                        result += "\n[color=yellow]The Giant Worm thrashes around but fails to drag anyone down! No penalty is applied.[/color]";
                        break;
                    default:
                        result += "\n" + session.DungeonDelveData.EliminatePlayer(lowestRoll, "PLAYER has perished in the fight.");
                        break;
                }
            }
            else
            {
                victory = true;
                // Clean victory
                result += "\n[color=green]You have defeated the monster![/color]";
                continueExploration = true;
            }

            // Post-Combat Random Poison Chance check
            if (continueExploration && monster.PoisonChance > 0)
            {
                List<DungeonDelvePlayer> playersToPoison = new List<DungeonDelvePlayer>();
                foreach (DungeonDelvePlayer p in rolledPlayers)
                {
                    if (p.Active && !p.Eliminated)
                    {
                        if (diceBot.random.NextDouble() < monster.PoisonChance)
                        {
                            playersToPoison.Add(p);
                        }
                    }
                }
                if (playersToPoison.Count > 0)
                {
                    result += "\n[color=orange]Due to the toxic presence of the " + monster.Name + ", some players were exposed to poison![/color]";
                    result += session.DungeonDelveData.ProcessPoisonForPlayers(diceBot, playersToPoison);
                }
            }

            if (continueExploration && monster.DeathChance > 0)
            {
                List<DungeonDelvePlayer> playersToDeath = new List<DungeonDelvePlayer>();
                foreach (DungeonDelvePlayer p in rolledPlayers)
                {
                    if (p.Active && !p.Eliminated)
                    {
                        if (diceBot.random.NextDouble() < monster.DeathChance)
                        {
                            playersToDeath.Add(p);
                        }
                    }
                }
                if (playersToDeath.Count > 0)
                {
                    //string numberString = "";
                    string allEliminations = "";
                    for (int i = 0; i < playersToDeath.Count; i++)
                    {
                        allEliminations += "\n" + session.DungeonDelveData.EliminatePlayer(playersToDeath[i], "[color=orange]Due to deadly gaze of the " + monster.Name + ", PLAYER died![/color]" );
                        //numberString += TextFormat.GetCharacterUserTags(playersToDeath[i].Name);
                        //dropsString += (playersToDeath[i].Items != null && playersToDeath[i].Items.Count() > 0)? "\n" + session.DungeonDelveData.DropAllItems(playersToDeath[i], false) : "";
                    }
                    result += allEliminations;
                }
            }

            if (!ignoreGameOverCheckhere)
            {
                if (session.DungeonDelveData.DungeonDelvePlayers.Count(a => a.Active && !a.Eliminated) == 0)
                {
                    result += "\n" + GameOver(diceBot, session, true);
                    continueExploration = false;
                }
                else
                {
                    continueExploration = true;
                }
            }

            return result;
        }

        public string DungeonMapRevealText(GameSession session)
        {
            int currentFloor = session.DungeonDelveData.CurrentFloor;
            List<string> futureFloors = new List<string>();
            for (int i = 1; i <= 5; i++)
            {
                int targetFloor = currentFloor + i;
                if (targetFloor < session.DungeonDelveData.AllFloors.Count)
                {
                    futureFloors.Add("Floor " + targetFloor + ": " + session.DungeonDelveData.AllFloors[targetFloor].ToString());
                }
            }
            return "\n[color=green]Dungeon Map reveals future floors:[/color]\n" + string.Join("\n", futureFloors);
        }

        public DungeonDelvePlayer GetDungeonDelvePlayerByName(GameSession session, string name)
        {
            return session.DungeonDelveData.DungeonDelvePlayers.FirstOrDefault(a => a.Name.ToLower() == name.ToLower());
        }

        public static string GetFlavorTextForVictory(System.Random random, bool closeMatch)
        {
            string flavorText = "";
            if (closeMatch)
            {
                int flavorRoll = random.Next(6);
                switch (flavorRoll)
                {
                    case 0:
                        flavorText = " [sub]I always believed in you.[/sub]";
                        break;
                    case 1:
                        flavorText = " [sub]That wasn't even a fair fight.[/sub]";
                        break;
                    case 2:
                        flavorText = " [sub]A glorious victory![/sub]";
                        break;
                    case 3:
                        flavorText = " [sub]Invite me to the afterparty?[/sub]";
                        break;
                    case 4:
                        flavorText = " [sub]A showdown for the ages.[/sub]";
                        break;
                    case 5:
                        flavorText = " [sub]What a match![/sub]";
                        break;
                    case 6:
                        flavorText = " [sub]And both of them will be sore tomorrow.[/sub]";
                        break;
                }
            }

            return flavorText;
        }

        private string GetPlayerList(GameSession session)
        {
            string rtn = "";
            foreach (SlamRollPlayer p in session.SlamRollData.SlamRollPlayers)
            {
                if (!string.IsNullOrEmpty(rtn))
                    rtn += ", ";

                rtn += p.ToString();
            }
            return rtn;
        }

        public string GetRulesText(GameSession session)
        {
            DungeonDelveData dat = session.DungeonDelveData;
            string rules = string.Format("Show Descriptions: {0}", dat.ShowDescriptions);
            return rules;
        }

        public string PurchaseItem(BotMain botMain, GameSession session, MessageAddress address, List<DungeonDelveItem> shopItems, int buyIndexPlusOne)
        {
            if (buyIndexPlusOne <= 0 || buyIndexPlusOne > shopItems.Count())
                return "Failed: Specify a number corresponding to the shop item you wish to buy";

            DungeonDelveItem selectedItem = shopItems[buyIndexPlusOne - 1];
            ChipPile playerPile = botMain.DiceBot.GetChipPile(address, false);
            bool spendChips = session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.StartingShop;
            DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);
            int itemPrice = selectedItem.GetItemPrice(session.DungeonDelveData, spendChips);
            if (playerPile == null || selectedItem == null)
            {
                return "Error: Player chip pile or item not found";
            }
            else if (play.Eliminated || !play.Active)
            {
                return "Failed: You cannot use the shop after dying or leaving the dungeon";
            }
            else if (play.Items.Count() >= play.GetMaximumInventoryItems())
            {
                return "Failed: You cannot buy more items while carrying " + play.GetMaximumInventoryItems() + " items";
            }
            else if (spendChips && (playerPile.Chips < itemPrice))
            {
                return "Failed: You do not have " + selectedItem.GetItemPrice(session.DungeonDelveData, true) + " " + BotMain.CurrencyPlaceholder + "s to buy " + selectedItem.Name + ".";
            }
            else if (!spendChips && (session.DungeonDelveData.TreasureFound < itemPrice))
            {
                return "Failed: You do not have " + selectedItem.GetItemPrice(session.DungeonDelveData, true) + " gold to buy " + selectedItem.Name + ".";
            }
            else
            {
                string currencyName = " gold";
                if (spendChips)
                {
                    botMain.DiceBot.AddChips(address, -1 * itemPrice, false);
                    currencyName = " " + BotMain.CurrencyPlaceholder + "s";
                }
                else
                    session.DungeonDelveData.TreasureFound -= itemPrice;

                DungeonDelveItem itemCopy = selectedItem.Copy();
                string revealText = "";
                if (itemCopy.Id == DungeonDelveItems.DungeonMap)
                {
                    itemCopy.ItemLevel = session.DungeonDelveData.CurrentFloor;
                    revealText = DungeonMapRevealText(session);
                }

                play.Items.Add(itemCopy);

                return play.Name + " purchased a " + selectedItem.Name + " for " + itemPrice + currencyName + revealText;
            }
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, MessageAddress address, GameSession session, string[] terms, string[] rawTerms)
        {
            string returnString = "";
            //set rules
            if (terms.Contains("setshowdescriptions"))
            {
                if (terms.Length < 2)
                {
                    returnString = "Error: improper command format. Use 'setgrowingtwos (on) or (off)'.";
                }
                else
                {
                    string allInputs = Utils.GetFullStringOfInputs(rawTerms);
                    string trueFalse = allInputs.Substring(allInputs.IndexOf(' ')).Trim().ToLower();

                    bool setValue = false;
                    bool successfulParse = bool.TryParse(trueFalse, out setValue);

                    if (trueFalse == "on" || trueFalse == "off")
                    {
                        successfulParse = true;
                        if (trueFalse == "on")
                        {
                            setValue = true;
                        }
                    }

                    if (successfulParse)
                    {
                        session.DungeonDelveData.ShowDescriptions = setValue;
                        returnString = "'ShowDescriptions' rule set to was set to " + (setValue ? "ON" : "OFF");
                    }
                    else
                    {
                        returnString = "Error: Input was invalid. GrowingTwos must be set to on/ true, or off/ false";
                    }
                }
            }
            if (!string.IsNullOrEmpty(returnString))
                return returnString;

            //other ingame commands
            if (session.State != GameState.GameInProgress)
            {
                return "Game commands for " + GetGameName() + " only work while the game is running.";
            }
            else if(session.DungeonDelveData.DungeonDelvePlayers.Count(a => a.Name == address.character) < 1)
            {
                return "Game commands for " + GetGameName() + " can only be used by characters who are playing the game.";
            }

            bool activePlayerChoice = session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.Exploration
                || session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.StartingShop
                || session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.CampsiteShop
                || session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.Campsite;
            //bool characterIsCurrentActivePlayer = address.character == GetCurrentActivePlayerName(session);

            if (terms.Contains("showplayers"))
            {
                string playerList = GetPlayerList(session);

                returnString = playerList;
            }
            else if (terms.Contains("showitems") || terms.Contains("status") || terms.Contains("inventory"))
            {
                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);
                string partyTreasure = session.DungeonDelveData.TreasureFound + " party gold";
                return play.Print() + ", " + partyTreasure;
            }
            else if (terms.Contains("flooritems") || terms.Contains("search") || terms.Contains("searchfloor"))
            {
                if (session.DungeonDelveData.FloorItems == null || session.DungeonDelveData.FloorItems.Count() == 0)
                    returnString = "There are currently no items on the floor that have been dropped.";
                else
                    returnString = "These items have been dropped on the floor: " + session.DungeonDelveData.GetFloorItemsList();
            }
            else if (terms.Contains("buy") || terms.Contains("b") || terms.Contains("buyitem") || terms.Contains("itembuy"))
            {
                if (!activePlayerChoice || !(session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.StartingShop || session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.CampsiteShop))
                {
                    return "Failed: A shop is not currently active to buy from";
                }
                List<int> allNumbers = new List<int>();
                int safety = 0;
                int rtn = Utils.GetNumberFromInputs(terms);
                string[] remainingTerms = terms;
                while (safety < 100 && rtn > 0)
                {
                    allNumbers.Add(rtn);
                    remainingTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingTerms, rtn.ToString());
                    rtn = Utils.GetNumberFromInputs(remainingTerms);
                    safety++;
                }
                int testValue = allNumbers.Count() < 1 ? -1 : allNumbers[0];

                List<DungeonDelveItem> shopItems = session.DungeonDelveData.ItemsRepository.ShopItems;
                if (session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.CampsiteShop)
                    shopItems = session.DungeonDelveData.CampShopItems;

                if (terms.Count() < 2 || allNumbers.Count() < 1 || testValue <= 0 || testValue > shopItems.Count)
                {
                    return "Failed: This command requires a number for an item in the shop to buy.";
                }

                string allOutputs = "";
                foreach (var indexPlusOne in allNumbers)
                {
                    if (!string.IsNullOrEmpty(allOutputs))
                        allOutputs += "\n";
                    allOutputs += PurchaseItem(botMain, session, address, shopItems, indexPlusOne);
                }
                returnString = allOutputs;
            }
            else if (terms.Contains("continue") || terms.Contains("c") || terms.Contains("proceed"))
            {
                //ensure choice to continue active
                if (!activePlayerChoice)
                {
                    return "Failed: Choice to continue/ stop is not currently active.";
                }

                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);
                if (!play.Active || play.Eliminated)
                {
                    return "Failed: You cannot continue the dungeon after dying or leaving the dungeon.";
                }

                //decided to continue to next floor
                if (play.ReadyToContinue)
                {
                    return "You have already declared you are ready to continue, currently waiting for other players.";
                }
                else if (play.Items.Count() > play.GetMaximumInventoryItems())
                {
                    return "Failed: You cannot move forward while carrying more than " + play.GetMaximumInventoryItems() + " items";
                }
                else
                {
                    returnString += PlayerContinue(botMain, diceBot, session, address);
                }
            }
            else if (terms.Contains("gamble") || terms.Contains("scout") || terms.Contains("shop"))
            {
                if (session.DungeonDelveData.CurrentRoomType() != DungeonDelveRoomType.Camp)
                {
                    return "Failed: This option is only available at a camp.";
                }
                if (session.DungeonDelveData.CurrentGameState != DungeonDelveGameState.Campsite)
                {
                    return "Failed: Choice of campsite action already made.";
                }

                bool continueExploration = false;
                if (terms.Contains("gamble"))
                {
                    int rtn = Utils.GetNumberFromInputs(terms);
                    //if the player has trick dice give them gold here instead of rolling
                    DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);
                    DungeonDelveItem relevantDice = play.Items.FirstOrDefault(a => a.Id == DungeonDelveItems.TrickDice);

                    if (rtn <= 0 || rtn > session.DungeonDelveData.TreasureFound)
                    {
                        return "Failed: Gamble requires a number over 0 and up to your current treasure found.";
                    }
                    else if (relevantDice != null)
                    {
                        play.Items.Remove(relevantDice);
                        int roll = 100;
                        returnString = "[i]You use your trick dice to gamble...[/i]\n";
                        returnString += "Your bet for " + rtn + " gold is accepted.\n";
                        session.DungeonDelveData.TreasureFound -= rtn;
                        returnString += "The roll is [b]" + roll + "[/b]. ";


                        returnString += "You won! You gain " + (rtn * 2) + " gold.";
                        session.DungeonDelveData.TreasureFound += rtn * 2;
                    }
                    else
                    {
                        int roll = botMain.DiceBot.random.Next(100) + 1;
                        returnString += "Your bet for " + rtn + " gold is accepted.\n";
                        session.DungeonDelveData.TreasureFound -= rtn;
                        returnString += "The roll is [b]" + roll + "[/b]. ";
                        if (roll > 50)
                        {
                            returnString += "You won! You gain [b]" + (rtn * 2) + " gold.[/b]";
                            session.DungeonDelveData.AddTreasureFound(rtn * 2);
                        }
                        else
                            returnString += "You lost!";

                        continueExploration = true;
                    }
                }
                else if (terms.Contains("scout") || terms.Contains("rumors"))
                {
                    int currentFloor = session.DungeonDelveData.CurrentFloor;
                    DungeonDelveRoomType nextRoomType = DungeonDelveRoomType.EmptyRoom;
                    if (currentFloor < session.DungeonDelveData.AllFloors.Count() + 1)
                        nextRoomType = session.DungeonDelveData.AllFloors[session.DungeonDelveData.CurrentFloor + 1];

                    returnString = "You scout ahead and see the next room is a " + nextRoomType;
                    continueExploration = true;
                }
                else if (terms.Contains("shop"))
                {
                    returnString = "You enter the campsite shop to trade with your treasure.";
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.CampsiteShop;
                    session.DungeonDelveData.RollRandomShopItems(diceBot.random);
                    returnString += "\n" + DisplayShopItems(botMain, session, false);
                    //show shop options
                }

                if (continueExploration)
                {
                    returnString += "\n" + ExplorationString;
                    session.DungeonDelveData.CurrentGameState = DungeonDelveGameState.Exploration;
                }
            }
            else if (terms.Contains("stop") || terms.Contains("leave") || terms.Contains("exit"))
            {
                //ensure choice to continue active
                if (!activePlayerChoice)
                {
                    return "Failed: Choice to continue/ stop is not currently active.";
                }
                //chose to leave the dungeon

                //split loot for party, this player leaves with it
                if (terms.Length > 1)
                {
                    return "Error: improper command format. Use 'sethealth # (playername)' with # as the number of health and (playername) as the user's full display name.";
                }

                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);

                if (!play.Active || play.Eliminated)
                {
                    return "Failed: You cannot leave the dungeon after dying or leaving the dungeon.";
                }

                returnString = PlayerLeavesGame(botMain, diceBot, address, session);
            }
            else if (terms.Contains("pass") || terms.Contains("p"))
            {
                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);

                if (play == null)
                    return "Error: player not found";
                if (play.HasReactionReady)
                {
                    play.HasReactionReady = false;
                }
                else
                {
                    return "Failed: " + TextFormat.GetCharacterUserTags(play.Name) + " does not have a reaction item ready right now.";
                }

                if (session.DungeonDelveData.DungeonDelvePlayers.Count(a => a.HasReactionReady) == 0)
                {
                    returnString = ExploreRoomAfterReactions(botMain, diceBot, session);
                }
                else
                {
                    returnString = TextFormat.GetCharacterUserTags(play.Name) + " passes. " + session.DungeonDelveData.GetAllPlayersNotDoneReacting();
                }
            }
            else if (terms.Contains("useitem") || terms.Contains("use") || terms.Contains("item") || terms.Contains("itemuse"))
            {
                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);

                int itemNumber = Utils.GetNumberFromInputs(terms);
                if (play == null)
                    return "Error: player not found";
                else if (itemNumber <= 0 || itemNumber > play.Items.Count())
                    return "Failed: select an item number within your inventory";

                DungeonDelveItem selectedItem = play.Items[itemNumber - 1];

                if (selectedItem.ItemType == DungeonDelveItemType.Key)
                {
                    if (play.Items.Count(a => a.ItemType == DungeonDelveItemType.Chest) <= 0)
                        return "Failed: You are not currently holding any chests to unlock.";
                    if (!activePlayerChoice)
                        return "Failed: Choice to continue/ stop is not currently active and keys are unusable.";

                    DungeonDelveItem selectedChest = play.Items.FirstOrDefault(a => a.ItemType == DungeonDelveItemType.Chest && a.ItemLevel == play.Items.Where(b => b.ItemType == DungeonDelveItemType.Chest).Max(b => b.ItemLevel));

                    if (selectedChest == null)
                        return "Error: failed to select chest";

                    returnString = TextFormat.GetCharacterUserTags(play.Name) + " has opened a chest.\n" + RollLoot(botMain, diceBot, session, true, selectedChest.ItemLevel);
                    play.Items.Remove(selectedItem);
                    play.Items.Remove(selectedChest);
                }
                else if (!selectedItem.Usable)
                {
                    return "Failed: " + selectedItem.Name + " cannot be used manually.";
                }
                else
                {
                    if(!play.HasReactionReady)
                    {
                        return "Failed: " + TextFormat.GetCharacterUserTags(play.Name) + " cannot use " + selectedItem.Name + " right now (requires reaction step).";
                    }

                    bool reactionMatches = selectedItem.ActivateOnTrap && session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.TrapReaction ||
                        selectedItem.ActivateOnEmpty && session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.EmptyRoomReaction ||
                        selectedItem.ActivateOnCombat && session.DungeonDelveData.CurrentGameState == DungeonDelveGameState.CombatReaction;
                    if (!reactionMatches)
                        return "Failed: (" + selectedItem.Name + ") cannot be used right now.";
                    else
                    {
                        string bonusOutput = "";
                        bool newRoomOutput = false;
                        //apply reaction...
                        if (selectedItem.ActivateOnCombat)
                        {
                            if (selectedItem.Id == DungeonDelveItems.SoulGem)//soul gem
                            {
                                play.ActivatedBypass = true;
                                bonusOutput = " and will bypass this monster without rolling.";
                            }
                            else
                            {
                                play.TemporaryCombatBonus = selectedItem.CombatBonus;//scroll of doom
                                bonusOutput = " and will gain a +" + play.TemporaryCombatBonus + " bonus to their combat rolls.";
                            }
                        }
                        else if(selectedItem.ActivateOnTrap)
                        {
                            if (selectedItem.Id == DungeonDelveItems.TrapKit)//trap kit
                            {
                                play.ActivatedBypass = true;
                                bonusOutput = " and will bypass this trap without rolling.";
                            }
                        }
                        else if (selectedItem.ActivateOnEmpty)
                        {
                            if (selectedItem.Id == DungeonDelveItems.Torch)//torch
                            {
                                DungeonDelveRoomType newRoomType = session.DungeonDelveData.GetRandomRoomType(diceBot.random, session.DungeonDelveData.CurrentFloor);
                                session.DungeonDelveData.AllFloors[session.DungeonDelveData.CurrentFloor] = newRoomType;

                                bonusOutput = " and has transformed this room into a " + newRoomType + ".";
                                newRoomOutput = true;
                            }
                        }

                        returnString = TextFormat.GetCharacterUserTags(play.Name) + " has activated their " + selectedItem.Name + bonusOutput;
                        play.HasReactionReady = false;
                        if (selectedItem.ItemType == DungeonDelveItemType.Consumable)
                            play.Items.Remove(selectedItem);

                        if (play.ActivatedBypass || newRoomOutput) //deactivate all players' reactions
                        {
                            foreach (DungeonDelvePlayer playDeactivate in session.DungeonDelveData.DungeonDelvePlayers)
                            {
                                if (playDeactivate != null)
                                    playDeactivate.HasReactionReady = false;
                            }
                        }

                        if(newRoomOutput)
                            returnString += "\n" + StartExploreRoom(botMain, diceBot, session);
                        else if (session.DungeonDelveData.DungeonDelvePlayers.Count(a => a.HasReactionReady) == 0)
                        {
                            returnString += "\n" + ExploreRoomAfterReactions(botMain, diceBot, session);
                        }
                    }
                }
            }
            else if (terms.Contains("giveitem") || terms.Contains("itemgive"))
            {
                //ensure choice to continue active
                if (!activePlayerChoice)
                {
                    return "Failed: Choice to continue/ stop is not currently active.";
                }

                if (terms.Length < 2)
                {
                    return "Error: improper command format. Use 'giveitem # (playername)' with # as the index of the item in your bags and (playername) as the user's full display name.";
                }

                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);
                if (!play.Active || play.Eliminated)
                {
                    return "Failed: You cannot use your inventory after having left or perished.";
                }
                if (play.ReadyToContinue)
                {
                    return "Failed: You have already left the room and cannot currently give items to other players.";
                }

                int itemIndex = Utils.GetNumberFromInputs(rawTerms);

                if (play.Items.Count <= 0 || play.Items.Count < itemIndex)
                    return "Failed: the specified item is too high for your inventory";
                else if (itemIndex <= 0)
                    return "Failed: specify a number for the item inside your inventory to give";

                string giveItemTerm = rawTerms.FirstOrDefault(t => t.Equals("giveitem", StringComparison.OrdinalIgnoreCase));

                string[] relevantTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, giveItemTerm);
                relevantTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(relevantTerms, itemIndex.ToString());
                //ensure the first digit is a number, ensure the player has more items than 0
                string targetName = Utils.GetUserNameFromFullInputs(relevantTerms);
                DungeonDelvePlayer play2 = GetDungeonDelvePlayerByName(session, targetName);

                if (play2 == null)
                {
                    return "Failed: could not find player with name \"" + targetName + "\"";
                }
                else if (!play2.Active || play2.Eliminated)
                {
                    return "Failed: You cannot give items to a player who has left or is eliminated.";
                }
                else if (play2.ReadyToContinue)
                {
                    return "Failed: " + TextFormat.GetCharacterUserTags(play2.Name) + " has already left the room and you cannot currently give items to them.";
                }
                else if (play2.Items.Count() >= play2.GetMaximumInventoryItems())
                {
                    return "Failed: You cannot give items to " + TextFormat.GetCharacterUserTags(play2.Name) +  " items while they are carrying more than " + play2.GetMaximumInventoryItems() + " items";
                }

                DungeonDelveItem itemToGive = play.Items[itemIndex - 1];
                play.Items.Remove(itemToGive);
                play2.Items.Add(itemToGive);
                returnString = TextFormat.GetCharacterUserTags(play.Name) + " has given their " + itemToGive.Name + " to " + TextFormat.GetCharacterUserTags(play2.Name) + ".";
            }
            else if (terms.Contains("dropitem") || terms.Contains("itemdrop") || terms.Contains("drop"))
            {
                //ensure choice to continue active
                if (!activePlayerChoice)
                {
                    return "Failed: Choice to continue/ stop is not currently active.";
                }

                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);
                if (!play.Active || play.Eliminated)
                {
                    return "Failed: You cannot use your inventory after having left or perished.";
                }

                List<int> allNumbers = new List<int>();
                int safety = 0;
                int rtn = Utils.GetNumberFromInputs(terms);
                string[] remainingTerms = terms;
                while (safety < 100 && rtn > 0)
                {
                    allNumbers.Add(rtn);
                    remainingTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingTerms, rtn.ToString());
                    rtn = Utils.GetNumberFromInputs(remainingTerms);
                    safety++;
                }
                int testValue = allNumbers.Count() < 1 ? -1 : allNumbers[0];

                List<DungeonDelveItem> playerItems = play.Items;
                if (play.Items.Count <= 0 || playerItems.Count < testValue)
                    return "Failed: the specified item is too high for your inventory";
                else if (testValue <= 0)
                    return "Failed: specify a number for the item inside your inventory to drop";

                List<DungeonDelveItem> itemsToDrop = new List<DungeonDelveItem>();
                foreach (var indexPlusOne in allNumbers)
                {
                    int trueindex = indexPlusOne - 1;
                    if (trueindex >= 0 && trueindex < playerItems.Count())
                        itemsToDrop.Add(playerItems[trueindex]);
                }
                itemsToDrop = itemsToDrop.Distinct().ToList();

                string allOutputs = "";
                foreach (var item in itemsToDrop)
                {
                    if (!string.IsNullOrEmpty(allOutputs))
                        allOutputs += "\n";
                    allOutputs += session.DungeonDelveData.DropItem(play, item, false);
                }
                returnString = allOutputs;
            }
            else if (terms.Contains("pickupitem") || terms.Contains("pickup") || terms.Contains("itempickup"))
            {
                //ensure choice to continue active
                if (!activePlayerChoice)
                {
                    return "Failed: Choice to continue/ stop is not currently active.";
                }

                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);
                if (!play.Active || play.Eliminated)
                {
                    return "Failed: You cannot use your inventory after having left or perished.";
                }
                if (session.DungeonDelveData.FloorItems.Count() == 0)
                    return "Failed: There are currently no items on the floor that have been dropped.";

                int itemIndex = Utils.GetNumberFromInputs(rawTerms);

                if (itemIndex <= 0 || session.DungeonDelveData.FloorItems.Count < itemIndex)
                    return "Failed: the specified item number is too high for the items on the floor";
                else if (itemIndex <= 0)
                    return "Failed: specify a number for the item on the floor to pick up";

                DungeonDelveItem itemToPickup = session.DungeonDelveData.FloorItems[itemIndex - 1];
                string pickup = session.DungeonDelveData.PickupItem(play, itemToPickup);
                returnString = pickup;
            }
            else if (terms.Contains("iteminfo") || terms.Contains("examineitem") || terms.Contains("item"))
            {
                DungeonDelvePlayer play = GetDungeonDelvePlayerByName(session, address.character);

                if (play == null || play.Items == null || play.Items.Count == 0)
                    return "Failed: Player inventory does not contain any items.";

                int itemIndex = Utils.GetNumberFromInputs(rawTerms);

                if (play.Items.Count <= 0 || play.Items.Count < itemIndex)
                    return "Failed: the specified item is too high for your inventory";
                else if (itemIndex <= 0)
                    return "Failed: specify a number for the item inside your inventory to give";

                DungeonDelveItem itemToGive = play.Items[itemIndex - 1];
                string output = itemToGive.Print(false, false, session.DungeonDelveData);
                returnString = output;
            }
            else
            {
                returnString = "A command for " + GetGameName() + " was not found.";
            }

            return returnString;

        }
    }
    #region gamedata types
    public class DungeonDelveData
    {
        public bool RulesAssigned = false;

        public bool ShowDescriptions = true;
        public int TreasureFound = 0;
        public int TotalTreasureFound = 0;

        public void AddTreasureFound(int treasureAmount)
        {
            TreasureFound += treasureAmount;
            TotalTreasureFound += treasureAmount;
        }
        public double RewardMultiplier = 1;
        public int CurrentTreasurePlayerIndex = 0;

        public DungeonDelveMonster CurrentMonster = null;

        public DungeonDelvePlayer GetNextTreasurePlayer()
        {
            int safety = 0;
            do
            {
                safety++;
                CurrentTreasurePlayerIndex++;
                if (CurrentTreasurePlayerIndex >= DungeonDelvePlayers.Count)
                    CurrentTreasurePlayerIndex = 0;
            }
            while ((!DungeonDelvePlayers[CurrentTreasurePlayerIndex].Active || DungeonDelvePlayers[CurrentTreasurePlayerIndex].Eliminated) && safety < 300);

            return DungeonDelvePlayers[CurrentTreasurePlayerIndex];
        }

        public DungeonDelveGameState CurrentGameState;

        public DungeonDelveItems ItemsRepository = new DungeonDelveItems();
        public DungeonDelveMonsters MonstersRepository = new DungeonDelveMonsters();
        public DungeonDelveTraps TrapsRepository = new DungeonDelveTraps();

        public int CurrentPlayerIndex = 0;
        public List<DungeonDelvePlayer> DungeonDelvePlayers = new List<DungeonDelvePlayer>();

        public int CurrentFloor = 0;
        public List<DungeonDelveRoomType> AllFloors = new List<DungeonDelveRoomType>();
        public List<DungeonDelveItem> CampShopItems = new List<DungeonDelveItem>();
        public List<DungeonDelveItem> FloorItems = new List<DungeonDelveItem>();

        public void RollRandomShopItems(Random rnd)
        {
            CampShopItems = new List<DungeonDelveItem>();
            foreach (DungeonDelveItem shopItem in ItemsRepository.ShopItems)
            {
                if (rnd.NextDouble() >= 0.5)
                    CampShopItems.Add(shopItem.Copy());
            }

        }

        public int NumberOfLivingPlayers()
        {
            return DungeonDelvePlayers.Count(a => a.Active && !a.Eliminated);
        }

        public bool AllPlayersReadyToContinue()
        {
            int activePlayers = NumberOfLivingPlayers();
            int countNotReady = DungeonDelvePlayers.Count(a => !a.ReadyToContinue && a.Active && !a.Eliminated);
            int countNotReadyPart = DungeonDelvePlayers.Count(a => !a.ReadyToContinue && a.Active);
            int countNotReadyPart2 = DungeonDelvePlayers.Count(a => !a.ReadyToContinue);

            return countNotReady == 0;
        }

        public string GetAllPlayersNotReady()
        {
            string rtn = "";
            List<DungeonDelvePlayer> relevantPlayers = DungeonDelvePlayers.Where(a => a.Active && !a.Eliminated && !a.ReadyToContinue).ToList();
            string plural = (relevantPlayers != null && relevantPlayers.Count() > 1) ? " have" : " has";
            if (relevantPlayers != null && relevantPlayers.Count() > 0)
            {
                foreach (DungeonDelvePlayer play in relevantPlayers)
                {
                    if (!string.IsNullOrEmpty(rtn))
                        rtn += ", ";
                    rtn += play.Name;
                }
            }
            return " [sub]" + rtn + plural + " not yet continued or left the dungeon[/sub]";
        }

        public string GetAllPlayersNotDoneReacting()
        {
            string rtn = "";
            List<DungeonDelvePlayer> relevantPlayers = DungeonDelvePlayers.Where(a => a.Active && !a.Eliminated && a.HasReactionReady).ToList();
            string plural = (relevantPlayers != null && relevantPlayers.Count() > 1) ? " have" : " has";
            if (relevantPlayers != null && relevantPlayers.Count() > 0)
            {
                foreach (DungeonDelvePlayer play in relevantPlayers)
                {
                    if (!string.IsNullOrEmpty(rtn))
                        rtn += ", ";
                    rtn += play.Name;
                }
            }
            return " [sub]" + rtn + plural + " not yet passed or used their item[/sub]";
        }

        public DungeonDelveRoomType CurrentRoomType()
        {
            if (CurrentFloor >= 0 && CurrentFloor < AllFloors.Count)
                return AllFloors[CurrentFloor];

            return DungeonDelveRoomType.EmptyRoom;
        }

        public List<ReactionItemPlayer> GetPlayersWithReaction(DungeonDelveItemActivationTriggerType triggerType)
        {
            Func<DungeonDelveItem, bool> matchesTrigger;

            switch (triggerType)
            {
                case DungeonDelveItemActivationTriggerType.Gamble:
                    matchesTrigger = item => item.ActivateOnGamble;
                    break;

                case DungeonDelveItemActivationTriggerType.EmptyRoom:
                    matchesTrigger = item => item.ActivateOnEmpty;
                    break;

                case DungeonDelveItemActivationTriggerType.MonsterRoom:
                    matchesTrigger = item => item.ActivateOnCombat;
                    break;

                case DungeonDelveItemActivationTriggerType.TrapRoom:
                    matchesTrigger = item => item.ActivateOnTrap;
                    break;

                case DungeonDelveItemActivationTriggerType.TreasureChest:
                    matchesTrigger = item => item.ActivateOnChest;
                    break;

                default:
                    return new List<ReactionItemPlayer>();
            }

            var results = new List<ReactionItemPlayer>();

            foreach (var player in DungeonDelvePlayers)
            {
                var addedItemIds = new HashSet<int>();

                for (int i = 0; i < player.Items.Count; i++)
                {
                    var item = player.Items[i];

                    if (!matchesTrigger(item))
                        continue;

                    // Skip duplicate item types for this player
                    if (!addedItemIds.Add(item.Id))
                        continue;

                    results.Add(new ReactionItemPlayer
                    {
                        Player = player,
                        Item = item,
                        ItemIndex = i + 1
                    });
                }
            }

            return results;
        }

        //public List<DungeonDelvePlayer> GetPlayersWithReaction(DungeonDelveItemActivationTriggerType triggerType)
        //{
        //    switch (triggerType)
        //    {
        //        case DungeonDelveItemActivationTriggerType.Gamble:
        //            return DungeonDelvePlayers.Where(a => a.Items.Count(b => b.ActivateOnGamble) > 0).ToList();
        //        case DungeonDelveItemActivationTriggerType.EmptyRoom:
        //            return DungeonDelvePlayers.Where(a => a.Items.Count(b => b.ActivateOnEmpty) > 0).ToList();
        //        case DungeonDelveItemActivationTriggerType.MonsterRoom:
        //            return DungeonDelvePlayers.Where(a => a.Items.Count(b => b.ActivateOnCombat) > 0).ToList();
        //        case DungeonDelveItemActivationTriggerType.TrapRoom:
        //            return DungeonDelvePlayers.Where(a => a.Items.Count(b => b.ActivateOnTrap) > 0).ToList();
        //        case DungeonDelveItemActivationTriggerType.TreasureChest:
        //            return DungeonDelvePlayers.Where(a => a.Items.Count(b => b.ActivateOnChest) > 0).ToList();
        //    }
        //    return null;
        //}

        public void MarkPlayersNeedingToPass(List<DungeonDelvePlayer> playersWithReaction)
        {
            foreach (DungeonDelvePlayer player in playersWithReaction)
            {
                player.HasReactionReady = true;
            }
        }

        public string UseAntidote(DiceBot diceBot, out bool antidoteUsed)
        {
            antidoteUsed = false;
            List<DungeonDelvePlayer> playerD = DungeonDelvePlayers.Where(a => a.Items.Count(b => b.Antidote) > 0).ToList();

            if (playerD.Count() == 0)
            {
                return "No antidote was found...";
            }
            else
            {
                int playerNumber = diceBot.random.Next(playerD.Count);
                DungeonDelvePlayer playerUsed = playerD[playerNumber];
                DungeonDelveItem itemUsed = playerUsed.Items.FirstOrDefault(a => a.Antidote);
                playerUsed.Items.Remove(itemUsed);
                antidoteUsed = true;
                return TextFormat.GetCharacterUserTags(playerUsed.Name) + " used " + itemUsed.Name;
            }
        }

        public string ProcessPoisonForPlayers(DiceBot diceBot, List<DungeonDelvePlayer> targets)
        {
            string res = "";
            foreach (DungeonDelvePlayer target in targets)
            {
                if (!target.Active || target.Eliminated) continue;

                // Look for anyone in the party who has an antidote
                List<DungeonDelvePlayer> playersWithAntidote = DungeonDelvePlayers
                    .Where(a => a.Active && !a.Eliminated && a.Items != null && a.Items.Any(b => b.Antidote))
                    .ToList();

                if (playersWithAntidote.Count > 0)
                {
                    // Prefer the target themselves, otherwise random player with antidote
                    DungeonDelvePlayer provider = playersWithAntidote.Contains(target)
                        ? target
                        : playersWithAntidote[diceBot.random.Next(playersWithAntidote.Count)];

                    DungeonDelveItem antidoteItem = provider.Items.FirstOrDefault(b => b.Antidote);

                    string removeAntidote = "";
                    if (antidoteItem.Id == DungeonDelveItems.EverfullAntidote)
                    {
                        if (Utils.Percentile(diceBot.random, 25))
                        {
                            removeAntidote = " The everfull antidote was exhausted.";
                            provider.Items.Remove(antidoteItem);
                        }
                    }
                    else
                        provider.Items.Remove(antidoteItem);

                    if (target.Name == provider.Name)
                    {
                        res += "\n" + TextFormat.GetCharacterUserTags(target.Name) + " [color=orange]was poisoned[/color], but they used an antidote!" + removeAntidote;
                    }
                    else
                    {
                        res += "\n" + TextFormat.GetCharacterUserTags(target.Name) + " [color=orange]was poisoned[/color], but " +
                               TextFormat.GetCharacterUserTags(provider.Name) + " used an antidote to cure them!" + removeAntidote;
                    }
                }
                else
                {
                    res += "\n" + EliminatePlayer(target, "PLAYER [color=orange]was poisoned[/color] and had no antidote! They have [color=red]perished[/color]!");
                }
            }
            return res;
        }

        public string EliminateAllPlayers(string eliminationString)
        {
            string noun = "Everyone";
            if (NumberOfLivingPlayers() == 1)
            {
                noun = TextFormat.GetCharacterUserTags(DungeonDelvePlayers.FirstOrDefault(a => !a.Eliminated && a.Active).Name);
            }

            string allElms = "";
            foreach (DungeonDelvePlayer player in DungeonDelvePlayers.Where(a => !a.Eliminated && a.Active))
            {
                allElms += EliminatePlayer(player, eliminationString);
            }

            if (DungeonDelvePlayers.Count(a => !a.Eliminated && a.Active) == 0)
            {
                allElms = eliminationString.Replace("PLAYER", noun);
                DungeonDelvePlayers[0].Items.AddRange(FloorItems);
            }

            return allElms;
        }

        public string EliminatePlayer(DungeonDelvePlayer player, string eliminationString)
        {
            string rtn = eliminationString.Replace("PLAYER", TextFormat.GetCharacterUserTags(player.Name));

            if (player.Items != null && player.Items.Count(a => a.Id == DungeonDelveItems.AnkhOfReincarnation) > 0)
            {
                rtn += " However, because of their Ankh of Reincarnation they have returned to life! The ankh has been destroyed.";
                var removeme = player.Items.FirstOrDefault(a => a.Id == DungeonDelveItems.AnkhOfReincarnation);
                player.Items.Remove(removeme);
            }
            else
            {
                player.Eliminated = true;

                if (NumberOfLivingPlayers() > 0)
                {
                    if (player.Items != null && player.Items.Count() > 0)
                    {
                        rtn += "\n" + DropAllItems(player, false);
                    }
                }
            }
            return rtn;
        }

        public DungeonDelveData(DiceBot diceBot)
        {
            AllFloors = new List<DungeonDelveRoomType>();
            for (int i = 0; i < 200; i++)
            {
                DungeonDelveRoomType thisRoomType = GetRandomRoomType(diceBot.random, i);
                AllFloors.Add(thisRoomType);
            }
        }

        public DungeonDelveRoomType GetRandomRoomType(System.Random rnd, int floorNumber)
        {
            int roll = rnd.Next(6);
            DungeonDelveRoomType thisRoomType = DungeonDelveRoomType.EmptyRoom;
            switch (roll)
            {
                case 0:
                    thisRoomType = DungeonDelveRoomType.Trap;
                    break;
                case 1:
                case 2:
                    thisRoomType = DungeonDelveRoomType.Monster;
                    break;
                case 3:
                    if (floorNumber <= 4)
                        thisRoomType = DungeonDelveRoomType.Monster;
                    else
                    {
                        thisRoomType = DungeonDelveRoomType.Camp;

                        if (AllFloors != null && floorNumber > 0)
                        {
                            //if the floor before or after (for torch) is a camp, set this to monster
                            if (floorNumber - 1 < AllFloors.Count() && AllFloors[floorNumber - 1] == DungeonDelveRoomType.Camp)
                                thisRoomType = DungeonDelveRoomType.Monster;

                            if (floorNumber + 1 < AllFloors.Count() && AllFloors[floorNumber + 1] == DungeonDelveRoomType.Camp)
                                thisRoomType = DungeonDelveRoomType.Monster;
                        }
                    }
                    break;
                case 4:
                    thisRoomType = DungeonDelveRoomType.EmptyRoom;
                    break;
                case 5:
                    thisRoomType = DungeonDelveRoomType.TreasureRoom;
                    break;
            }
            return thisRoomType;
        }

        public string AwardXp(int xpAmount, DungeonDelvePlayer play = null)
        {
            string returnString = "";
            returnString = "You gained " + xpAmount + " xp. ";
            if (play != null)
                play.CurrentXP += xpAmount;
            else
            {
                foreach (DungeonDelvePlayer p in DungeonDelvePlayers)
                {
                    if(p.Active && !p.Eliminated)
                        p.CurrentXP += xpAmount;
                }
            }

            foreach (DungeonDelvePlayer p in DungeonDelvePlayers)
            {
                while (p.CurrentXP >= DungeonDelve.LevelupXpRequired)
                {
                    p.CurrentXP -= DungeonDelve.LevelupXpRequired;
                    p.Level += 1;
                    returnString += TextFormat.GetCharacterUserTags(p.Name) + " leveled up to " + p.Level + "! ";
                }
            }
            return returnString;
        }

        public void ResetPlayerContinueFlags()
        {
            if (DungeonDelvePlayers != null && DungeonDelvePlayers.Count() > 0)
            {
                foreach (DungeonDelvePlayer p in DungeonDelvePlayers)
                {
                    p.ReadyToContinue = false;
                    p.ActivatedBypass = false;
                    p.TemporaryCombatBonus = 0;
                }
            }
            ResetFloorItems();
        }

        public void ResetFloorItems()
        {
            FloorItems = new List<DungeonDelveItem>();
        }

        public string DropAllItems(DungeonDelvePlayer player, bool leaveDungeon)
        {
            if (player == null)
                return "Error: player not found";
            string rtn = "";
            int itemsStarting = player.Items.Count();
            for (int i = 0; i < itemsStarting; i++)
            {
                if (!string.IsNullOrEmpty(rtn))
                    rtn += "\n";
                rtn += DropItem(player, 0, leaveDungeon);
            }
            return rtn;
        }

        public string DropItem(DungeonDelvePlayer player, int itemExactIndex, bool dropForLeavingDungeon)
        {
            if (itemExactIndex >= player.Items.Count || itemExactIndex < 0)
            {
                return "Failed: Item outside scope of inventory";
            }
            DungeonDelveItem item = player.Items[itemExactIndex];
            return DropItem(player, item, dropForLeavingDungeon);
        }

        public string DropItem(DungeonDelvePlayer player, DungeonDelveItem item, bool dropForLeavingDungeon)
        {
            if (item == null || player == null || !player.Items.Contains(item))
                return "Error: DropItem item or player missing";
            if (item.KeepOnLeave && dropForLeavingDungeon)
                return "";

            player.Items.Remove(item);
            FloorItems.Add(item);
            return TextFormat.GetCharacterUserTags(player.Name) + " dropped " + item.Name + " on the floor. You can retrieve it with !gc pickupitem " + FloorItems.Count();
        }

        public string PickupItem(DungeonDelvePlayer player, int itemExactIndex)
        {
            if (itemExactIndex >= FloorItems.Count || itemExactIndex < 0)
            {
                return "Failed: Item outside scope of FloorItems";
            }
            DungeonDelveItem item = FloorItems[itemExactIndex];
            return PickupItem(player, item);
        }

        public string PickupItem(DungeonDelvePlayer player, DungeonDelveItem item)
        {
            if (item == null || player == null)
                return "Error: PickupItem item or player missing";
            player.Items.Add(item);
            FloorItems.Remove(item);
            return TextFormat.GetCharacterUserTags(player.Name) + " has picked up " + item.Name + " off the floor. You can view items on the floor with !gc flooritems";
        }

        public string GetFloorItemsList()
        {
            string rtn = "";
            if (FloorItems == null || FloorItems.Count() == 0)
                return rtn;

            int index = 1;
            foreach (DungeonDelveItem item in FloorItems)
            {
                if (!string.IsNullOrEmpty(rtn))
                    rtn += ", ";
                rtn += "(" + index + ") " + item.Name;

                index++;
            }
            return rtn;
        }

    }

    public class ReactionItemPlayer
    {
        public DungeonDelvePlayer Player;
        public DungeonDelveItem Item;
        public int ItemIndex;

        public string Print()
        {
            return TextFormat.GetCharacterUserTags(Player.Name) + " has [color=yellow][sub](" + ItemIndex + ")[/sub] " + Item.Name + "[/color]";
        }
    }

    public enum DungeonDelveGameState
    {
        StartingShop,
        Combat,
        CombatReaction,
        Trap,
        TrapReaction,
        Campsite,
        CampsiteShop,
        EmptyRoomReaction,
        Looting,
        Exploration
    }

    public enum DungeonDelveItemType
    {
        Key,
        Chest,
        Weapon,
        Armor,
        Consumable,
        Loot,
        Charm,
        Miscellaneous
    }

    public enum DungeonDelveRoomType
    {
        EmptyRoom,
        Monster,
        Trap,
        Camp,
        TreasureRoom
    }

    public enum DungeonDelveItemActivationTriggerType
    {
        EmptyRoom,
        MonsterRoom,
        TrapRoom,
        Gamble,
        TreasureChest
    }

    public enum DungeonDelveClass
    {
        Fighter, //+1 to combat total
        Wizard, //? 3 activations of Magic Missile, +3 to combat score
        Cleric, //? healing potion for free on ally 3/ dungeon
        Thief, //25% chance to disarm traps for free
        Scout //50% chance to find next room type after each room
    }

    public class DungeonDelveItem
    {
        public int Id;
        public int CombatBonus;
        public int InventorySpaceMaximum;
        public DungeonDelveItemType ItemType;
        public int ItemPrice;
        public int ItemLevel;
        public string Description;
        public string Name;
        public bool ActivateOnTrap;
        public bool ActivateOnCombat;
        public bool ActivateOnGamble;
        public bool ActivateOnEmpty;
        public bool ActivateOnChest;
        public bool Antidote;
        public bool HealingPotion;
        public bool KeepOnLeave;
        public bool Usable;

        public int GetItemPrice(DungeonDelveData data, bool spendChips)
        {
            if (spendChips)
                return (int)Math.Round(data.RewardMultiplier * ItemPrice);
            else
                return ItemPrice;
        }

        public string GetItemColor()
        {
            switch (ItemType)
            {
                case DungeonDelveItemType.Weapon:
                case DungeonDelveItemType.Armor:
                case DungeonDelveItemType.Charm:
                    return "red";
                case DungeonDelveItemType.Loot:
                    return "white";
                default:
                    return "yellow";
            }
        }

        public string Print(bool shopPrint, bool spendChips, DungeonDelveData data)
        {
            string label = spendChips ? BotMain.CurrencyPlaceholderCapital + "s" : " Gold";
            string level = (ItemLevel > 0 || Id == DungeonDelveItems.DungeonMap) ? " [i](level " + ItemLevel + ")[/i]" : "";
            return "[color=" + GetItemColor() + "]" + Name + "[/color]: (" + (shopPrint ? (GetItemPrice(data, spendChips)) + " " + label + ", " : "") + "[color=" + GetItemColor() + "]" + ItemType.ToString() + "[/color]) " + Description + level;
        }

        public DungeonDelveItem Copy()
        {
            return (DungeonDelveItem) this.MemberwiseClone();
        }
    }

    public class CombatDiceRoll
    {
        public int Bonus;
        public int DiceNumber;
        public int DiceSides;
        public int Total;

        public string Print()
        {
            return DiceNumber + "d" + DiceSides + " " + (Bonus > 0 ? "+" + Bonus : (Bonus < 0 ? "" + Bonus : ""));
        }
        public string Roll(BotMain botMain, MessageAddress address, DiceBot diceBot, int temporaryBonus)
        {
            DiceRoll d = new DiceRoll(address, botMain);
            d.DiceSides = DiceSides;
            d.DiceRolled = DiceNumber;
            d.Roll(botMain.DiceBot.random);
            string printRolls = d.PrintRollsList(d.Rolls, false);
            Total = (int)d.Total + Bonus + temporaryBonus;
            return printRolls;
            //string result = diceBot.GetRollResult(new string[] { Print() }, address, false); //DiceNumber + "d" + DiceSides }, .GetRollResult(
        }

    }

    public class DungeonDelvePlayer
    {
        public bool Active;
        public bool Eliminated;
        public string Name;
        public string StageName;
        public int Endurance;
        public int Level;

        public int CurrentCombatTotal;
        public int TemporaryCombatBonus;
        public int CurrentXP;
        public bool ActivatedBypass;

        public bool Cursed;
        public bool ReadyToContinue;
        public bool HasReactionReady;
        public DungeonDelveClass PlayerClass;
        public List<DungeonDelveItem> Items;

        public CombatDiceRoll GetCombatBonus(bool excludeArmor, bool isHarpy = false)
        {
            int weaponBonus = Items != null && Items.Count > 0 ? Items.Where(a => a.ItemType == DungeonDelveItemType.Weapon).Select(b => b.CombatBonus).DefaultIfEmpty(0).Max() : 0;
            int armorBonus = !excludeArmor && Items != null && Items.Count > 0 ? Items.Where(a => a.ItemType == DungeonDelveItemType.Armor).Select(b => b.CombatBonus).DefaultIfEmpty(0).Max() : 0;
            int charmsBonus = Items != null ? Items.Where(a => a.ItemType == DungeonDelveItemType.Charm).Sum(a => a.CombatBonus) : 0;
            int bonus = Level + TemporaryCombatBonus + weaponBonus + armorBonus + charmsBonus;
            return new CombatDiceRoll() { DiceNumber = isHarpy ? 1 : 2,
                DiceSides = 6,
                Bonus = bonus};
        }

        public int GetMaximumInventoryItems()
        {
            int maxSpaceOnItems = DungeonDelve.MaximumInventoryItems;
            if (Items != null && Items.Count() > 0)
                maxSpaceOnItems = Math.Max(maxSpaceOnItems, Items.Max(a => a.InventorySpaceMaximum));

            return maxSpaceOnItems;
        }

        public string GetItemsList(string colorAlreadySet)
        {
            string rtn = "";
            int index = 1;
            foreach (DungeonDelveItem item in Items)
            {
                string color = item.GetItemColor();
                if (!string.IsNullOrEmpty(rtn))
                    rtn += ", ";

                if(color == colorAlreadySet)//color yellow already set in items list
                    rtn += "[sub](" + index + ")[/sub] " + item.Name + "";
                else
                    rtn += "[sub](" + index + ")[/sub] [color=" + color + "]" + item.Name + "[/color]";

                index++;
            }
            return rtn;
        }

        public string Print()
        {
            return TextFormat.GetCharacterUserTags(StageName) + " (Level [b]" + Level + "[/b], XP: " + CurrentXP + "/" + DungeonDelve.LevelupXpRequired +
                ", Combat Roll " + GetCombatBonus(false).Print() + ") ([color=yellow][color=green]Items:[/color] " + GetItemsList("yellow") + "[/color])" +
                (Cursed? " (cursed)":"") +
                (Active ? "" : " (inactive)") + (Eliminated ? "(eliminated)" : "");
        }
    }

    public class DungeonDelveItems
    {
        public List<DungeonDelveItem> AllItems;
        public List<DungeonDelveItem> ShopItems;
        public List<DungeonDelveItem> CommonDropItems;
        public List<DungeonDelveItem> DropItems;

        public const int SwordOfSharpness = 1, ArmorOfStoutness = 2, ScrollOfDoom = 3, Antidote = 4, TrapKit = 5, HealingPotion = 6, Torch = 7;
        public const int Lockpick = 8, DungeonMap = 9, FlimsyDagger = 10, LeatherArmor = 11, BigBackpack = 12, GratuitousGoldMace = 13;
        public const int TreasureChest = 101, Key = 102, EmeraldGem = 103, DiamondGem = 104;

        public const int TrickDice = 201, BowOfMonsterSlaying = 202, SoulGem = 203, AnkhOfReincarnation = 204, ImperviousArmor = 205, HandOfMidas = 206,
            EverfullAntidote = 207, BagOfHolding = 208, AngelElixir = 209, StrengthCharm = 210;

        public DungeonDelveItems()
        {
            AllItems = new List<DungeonDelveItem>();
            ShopItems = new List<DungeonDelveItem>();
            CommonDropItems = new List<DungeonDelveItem>();
            DropItems = new List<DungeonDelveItem>();
            Initialize();
        }

        private void Initialize()
        {
            DungeonDelveItem item1 = new DungeonDelveItem() { Id = SwordOfSharpness, Name = "Sword of Sharpness", CombatBonus = 3, Description = "+3 Combat Bonus.", ItemType = DungeonDelveItemType.Weapon, ItemPrice = 900, Usable = false };
            AllItems.Add(item1);
            ShopItems.Add(item1);
            DungeonDelveItem item1b = new DungeonDelveItem() { Id = FlimsyDagger, Name = "Flimsy Dagger", CombatBonus = 1, Description = "+1 Combat Bonus.", ItemType = DungeonDelveItemType.Weapon, ItemPrice = 150, Usable = false };
            AllItems.Add(item1b);
            ShopItems.Add(item1b);
            DungeonDelveItem item9c = new DungeonDelveItem() { Id = GratuitousGoldMace, Name = "Gratuitous Gold Mace", CombatBonus = 6, Description = "+6 Combat Bonus.", ItemType = DungeonDelveItemType.Weapon, ItemPrice = 6900, Usable = false };
            AllItems.Add(item9c);
            ShopItems.Add(item9c);

            DungeonDelveItem item2 = new DungeonDelveItem() { Id = ArmorOfStoutness,
                Name = "Armor of Stoutness", CombatBonus = 3, Description = "+3 Combat Bonus.", ItemType = DungeonDelveItemType.Armor, ItemPrice = 1500, Usable = false };
            AllItems.Add(item2);
            ShopItems.Add(item2);
            DungeonDelveItem item2b = new DungeonDelveItem() { Id = LeatherArmor, Name = "Leather Armor", CombatBonus = 1, Description = "+1 Combat Bonus.", ItemType = DungeonDelveItemType.Armor, ItemPrice = 200, Usable = false };
            AllItems.Add(item2b);
            ShopItems.Add(item2b);

            DungeonDelveItem item3 = new DungeonDelveItem() { Id = ScrollOfDoom,
                Name = "Scroll of Doom", CombatBonus = 7, Description = "+7 Combat Bonus.", ItemType = DungeonDelveItemType.Consumable, ActivateOnCombat = true, ItemPrice = 600, Usable = true };
            AllItems.Add(item3);
            ShopItems.Add(item3);

            DungeonDelveItem item4 = new DungeonDelveItem() { Id = Antidote,
                Name = "Antidote", CombatBonus = 0, Description = "Cures poison from any monster or trap.", ItemType = DungeonDelveItemType.Consumable, Antidote = true, ItemPrice = 300, Usable = false };
            AllItems.Add(item4);
            ShopItems.Add(item4);
            CommonDropItems.Add(item4);

            DungeonDelveItem item5 = new DungeonDelveItem() { Id = TrapKit,
                Name = "Trap Kit", CombatBonus = 0, Description = "Bypass a trap without rolling.", ItemType = DungeonDelveItemType.Consumable, ActivateOnTrap = true, ItemPrice = 600, Usable = true };
            AllItems.Add(item5);
            ShopItems.Add(item5);

            DungeonDelveItem item6 = new DungeonDelveItem() { Id = HealingPotion,
                Name = "Healing Potion", CombatBonus = 0, Description = "Reroll a failed monster battle.", ItemType = DungeonDelveItemType.Consumable, HealingPotion = true, ItemPrice = 500, Usable = false };
            AllItems.Add(item6);
            ShopItems.Add(item6);
            CommonDropItems.Add(item6);

            DungeonDelveItem item7 = new DungeonDelveItem() { Id = Torch,
                Name = "Torch", CombatBonus = 0, Description = "Reroll an empty room.", ItemType = DungeonDelveItemType.Consumable, ActivateOnEmpty = true, ItemPrice = 100, Usable = true };
            AllItems.Add(item7);
            ShopItems.Add(item7);
            CommonDropItems.Add(item7);

            DungeonDelveItem item8 = new DungeonDelveItem() { Id = Lockpick,
                Name = "Lockpick", CombatBonus = 0, Description = "Open a chest without a proper key.", ItemType = DungeonDelveItemType.Key, ActivateOnChest = true, ItemPrice = 200, Usable = true };
            AllItems.Add(item8);
            ShopItems.Add(item8);
            CommonDropItems.Add(item8);

            DungeonDelveItem item9 = new DungeonDelveItem() { Id = DungeonMap, Name = "Dungeon Map", CombatBonus = 0, Description = "See the next 5 rooms of the dungeon and auto bypass traps within them.", ItemType = DungeonDelveItemType.Miscellaneous, ItemPrice = 1000, Usable = false };
            AllItems.Add(item9);
            ShopItems.Add(item9);

            DungeonDelveItem item9b = new DungeonDelveItem() { Id = BigBackpack, Name = "Big Backpack", CombatBonus = 0, Description = "You can now carry up to 12 items.", ItemType = DungeonDelveItemType.Miscellaneous, InventorySpaceMaximum = 12, ItemPrice = 300, Usable = false };
            AllItems.Add(item9b);
            ShopItems.Add(item9b);

            DungeonDelveItem itemJewels = new DungeonDelveItem() { Id = EmeraldGem, Name = "Emerald", CombatBonus = 0, Description = "Sells for 300 gold after leaving the dungeon.", KeepOnLeave = true, ItemType = DungeonDelveItemType.Loot, ItemPrice = 300, Usable = false };
            AllItems.Add(itemJewels);
            CommonDropItems.Add(itemJewels);

            DungeonDelveItem itemJewels2 = new DungeonDelveItem() { Id = DiamondGem, Name = "Diamond", CombatBonus = 0, Description = "Sells for 600 gold after leaving the dungeon.", KeepOnLeave = true, ItemType = DungeonDelveItemType.Loot, ItemPrice = 600, Usable = false };
            AllItems.Add(itemJewels2);
            DropItems.Add(itemJewels2);

            DungeonDelveItem item10 = new DungeonDelveItem() { Id = TreasureChest,
                Name = "Treasure Chest", CombatBonus = 0, Description = "A treasure chest contains valuable treasure, but requires a key to open.", ItemType = DungeonDelveItemType.Chest, Usable = false };
            AllItems.Add(item10);
            //DropItems.Add(item10);

            DungeonDelveItem item11 = new DungeonDelveItem()
            {
                Id = Key,
                Name = "Key",
                CombatBonus = 0,
                Description = "Can be used to open a treasure chest.",
                ActivateOnChest = true,
                ItemType = DungeonDelveItemType.Key,
                Usable = true
            };
            AllItems.Add(item11);
            //DropItems.Add(item11);

            DungeonDelveItem item12 = new DungeonDelveItem() { Id = TrickDice, Name = "Trick Dice", CombatBonus = 0, Description = "When gambling, you will automatically win the die roll.", ActivateOnGamble = true, ItemType = DungeonDelveItemType.Consumable, Usable = false };
            AllItems.Add(item12);
            DropItems.Add(item12);

            DungeonDelveItem item13 = new DungeonDelveItem() { Id = BowOfMonsterSlaying, Name = "Bow of Monster Slaying", CombatBonus = 7, Description = "+7 combat bonus.", ItemType = DungeonDelveItemType.Weapon, Usable = false };
            AllItems.Add(item13);
            DropItems.Add(item13);

            DungeonDelveItem item14 = new DungeonDelveItem() { Id = ImperviousArmor, Name = "Impervious Armor", CombatBonus = 6, Description = "+6 combat bonus.", ItemType = DungeonDelveItemType.Armor, Usable = false };
            AllItems.Add(item14);
            DropItems.Add(item14);

            DungeonDelveItem item15 = new DungeonDelveItem() { Id = SoulGem, Name = "Soul Gem", CombatBonus = 0, Description = "Defeat any monster without rolling.", ActivateOnCombat = true, ItemType = DungeonDelveItemType.Consumable, Usable = true };
            AllItems.Add(item15);
            DropItems.Add(item15);

            DungeonDelveItem item16 = new DungeonDelveItem() { Id = HandOfMidas, Name = "Hand of Midas", CombatBonus = 0, Description = "Whenever you defeat a monster, gain an extra 500 gold.", ItemType = DungeonDelveItemType.Miscellaneous, Usable = false };
            AllItems.Add(item16);
            DropItems.Add(item16);

            DungeonDelveItem item17 = new DungeonDelveItem() { Id = AnkhOfReincarnation, Name = "Ankh of Reincarnation", CombatBonus = 0, Description = "When you would perish from any effect, instead destroy this item.", ItemType = DungeonDelveItemType.Miscellaneous, Usable = false };
            AllItems.Add(item17);
            DropItems.Add(item17);

            DungeonDelveItem item18 = new DungeonDelveItem() { Id = EverfullAntidote, Name = "Everfull Antidote", CombatBonus = 0, Description = "Cures poison from any monster or trap. Has a 75% chance to remain after use.", Antidote = true, ItemType = DungeonDelveItemType.Consumable, Usable = false };
            AllItems.Add(item18);
            DropItems.Add(item18);

            DungeonDelveItem item19 = new DungeonDelveItem() { Id = BagOfHolding, Name = "Bag of Holding", CombatBonus = 0, Description = "You can now carry up to 999 items.", InventorySpaceMaximum = 999, ItemType = DungeonDelveItemType.Miscellaneous, Usable = false };
            AllItems.Add(item19);
            DropItems.Add(item19);

            DungeonDelveItem item20 = new DungeonDelveItem() { Id = AngelElixir, Name = "Angel Elixir", CombatBonus = 3, Description = "Reroll a failed monster battle with a +3 bonus. Has a 50% chance to remain after use.", HealingPotion = true, ItemType = DungeonDelveItemType.Consumable, Usable = false };
            AllItems.Add(item20);
            DropItems.Add(item20);

            DungeonDelveItem item21 = new DungeonDelveItem() { Id = StrengthCharm, Name = "Strength Charm", CombatBonus = 2, Description = "+2 Combat Bonus", ItemType = DungeonDelveItemType.Charm, Usable = false };
            AllItems.Add(item21);
            DropItems.Add(item21);
        }

        public DungeonDelveItem GetItem(int id)
        {
            return AllItems.FirstOrDefault(a => a.Id == id)?.Copy();
        }

        public DungeonDelveItem R(int id)
        {
            return AllItems.FirstOrDefault(a => a.Id == id)?.Copy();
        }

    }

    public class DungeonDelveMonster
    {
        public int Id;
        public string Name;
        public string Description;
        public string EiconName;

        public int Strength;
        public int StrengthBonus;
        public double StrengthMultiplier;
        public double TreasureMultiplier;
        public bool StrengthAssigned;
        public double PoisonChance;
        public double DeathChance;
        public OnLossTrigger OnLoss;

        public string Print(DiceBot diceBot, DungeonDelveData data)
        {
            if (!StrengthAssigned)
            {
                if (StrengthMultiplier != 0 && StrengthMultiplier != 1)
                {
                    Strength = (int)Math.Floor((double)data.CurrentFloor * StrengthMultiplier) + StrengthBonus + DungeonDelve.BaseMonsterStrengthBonus;
                }
                else
                {
                    Strength = data.CurrentFloor + StrengthBonus + DungeonDelve.BaseMonsterStrengthBonus;
                }
                if (Id == DungeonDelveMonsters.GoldGolem)
                {
                    int playerCount = data.DungeonDelvePlayers.Count();
                    int trueTreasure = (int)Math.Floor(((double)data.TreasureFound / ((playerCount - 1 * 0.5) + 1)) / 100);
                    Strength = trueTreasure / 2;
                }
                if (Id == DungeonDelveMonsters.ShadowWarrior)
                {
                    int die1 = diceBot.random.Next(10) + 1;
                    int bonusTotal = StrengthBonus - die1;
                    Strength = data.CurrentFloor + bonusTotal + DungeonDelve.BaseMonsterStrengthBonus;
                }
                StrengthAssigned = true;
                if (Strength < 0)
                    Strength = 0;
            }

            return "[eicon]"+ EiconName+"[/eicon] " +  Name + ": (Strength " + Strength + ") " + Description;
        }

        public DungeonDelveMonster Copy()
        {
            return (DungeonDelveMonster)this.MemberwiseClone();
        }
    }

    public enum OnLossTrigger
    {
        Death,
        Poison,
        StealItem,
        LoseMoney,
        PitDrop,
        LoseAllLevels,
        Special
    }

    public class DungeonDelveMonsters
    {
        public List<DungeonDelveMonster> AllMonsters;
        public List<DungeonDelveMonster> BossMonsters;

        public const int Minotaur = 1, GiantBeast = 2, GangOfOrcs = 3, HordeOfGremlins = 4, Dragon = 5, GiantSnake = 6, Cockatrice = 7;
        public const int GiantMushroom = 8, PoisonFrog = 9, Demon = 10, ShadowWarrior = 11, GoldGolem = 12, Harpy = 13, GiantWorm = 14, Wraith = 15, SkeletonKnight = 16;

        public DungeonDelveMonsters()
        {
            AllMonsters = new List<DungeonDelveMonster>();
            BossMonsters = new List<DungeonDelveMonster>();
            Initialize();
        }

        private void Initialize()
        {
            DungeonDelveMonster mon1 = new DungeonDelveMonster()
            {
                Id = Minotaur,
                Name = "Minotaur",
                EiconName = "dbminotaur",
                StrengthBonus = 0,
                Description = "A large half cow half man; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(mon1);
            DungeonDelveMonster mon2 = new DungeonDelveMonster()
            {
                Id = GiantBeast,
                Name = "Giant Beast",
                EiconName = "dbgiantbeast",
                StrengthBonus = 0,
                Description = "A giant beast of unknown type; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(mon2);
            DungeonDelveMonster monOrc = new DungeonDelveMonster()
            {
                Id = GangOfOrcs,
                Name = "Gang of Orcs",
                EiconName = "dborcgang",
                StrengthBonus = 0,
                StrengthMultiplier = 0.5,
                Description = "A gang of unrly greenskins; must win combat 5 times; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(monOrc);
            DungeonDelveMonster mon4 = new DungeonDelveMonster()
            {
                Id = Wraith,
                Name = "Wraith",
                EiconName = "dbwraith",
                StrengthBonus = -2,
                Description = "A pitch black evil ghost; Ignores armor bonus; On loss: lose all levels",
                OnLoss = OnLossTrigger.LoseAllLevels
            };
            AllMonsters.Add(mon4);
            DungeonDelveMonster monGremlins = new DungeonDelveMonster()
            {
                Id = HordeOfGremlins,
                Name = "Horde of Gremlins",
                EiconName = "dbgremlinhorde",
                StrengthBonus = 4,
                Description = "A room full of tiny, greedy gremlins; On loss: lose 1 item",
                OnLoss = OnLossTrigger.StealItem
            };
            AllMonsters.Add(monGremlins);
            DungeonDelveMonster monDragon = new DungeonDelveMonster()
            {
                Id = Dragon,
                Name = "Dragon",
                EiconName = "dbdragon",
                StrengthBonus = 3,
                TreasureMultiplier = 2,
                Description = "A large winged firebreathing dragon; Awards double treasure; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(monDragon);
            DungeonDelveMonster mon3 = new DungeonDelveMonster()
            {
                Id = GiantSnake,
                Name = "Giant Snake",
                EiconName = "dbgiantsnake",
                StrengthBonus = -3,
                PoisonChance = 0.5,
                Description = "A giant serpent with venomous fangs; 50% chance to inflict poison; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(mon3);
            DungeonDelveMonster mon5 = new DungeonDelveMonster()
            {
                Id = SkeletonKnight,
                Name = "Skeleton Knight",
                EiconName = "dbskeletonknight",
                StrengthBonus = -1,
                Description = "An undead knight in armor; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(mon5);
            DungeonDelveMonster mon6 = new DungeonDelveMonster()
            {
                Id = Cockatrice,
                Name = "Cockatrice",
                EiconName = "dbcockatrice",
                StrengthBonus = 0,
                StrengthMultiplier = 0.5,
                DeathChance = 0.25,
                Description = "A serpent-like bird creature with a deadly gaze; 25% chance to inflict instant death; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(mon6);
            DungeonDelveMonster monShroom = new DungeonDelveMonster()
            {
                Id = GiantMushroom,
                Name = "Giant Mushroom",
                EiconName = "dbgiantmushroom",
                StrengthBonus = 3,
                Description = "A gigantic poisonous mushroom; On loss: poison",
                OnLoss = OnLossTrigger.Poison
            };
            AllMonsters.Add(monShroom);
            DungeonDelveMonster poisonfrog = new DungeonDelveMonster()
            {
                Id = PoisonFrog,
                Name = "Poison Frog",
                EiconName = "dbpoisonfrog",
                StrengthBonus = -1,
                PoisonChance = 0.17,
                Description = "A fast poisonous frog; 17% chance to get poisoned; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(poisonfrog);
            DungeonDelveMonster demon = new DungeonDelveMonster()
            {
                Id = Demon,
                Name = "Demon",
                EiconName = "dbdemon",
                StrengthBonus = 3,
                Description = "A mighty evil winged demon; You get to roll twice; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(demon);
            DungeonDelveMonster shadowMon = new DungeonDelveMonster()
            {
                Id = ShadowWarrior,
                Name = "Shadow Warrior",
                EiconName = "dbshadowwarrior",
                StrengthBonus = 5,//manually assigned strength
                Description = "A shadowy warrior; Highly variable strength; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(shadowMon);
            DungeonDelveMonster goldgol = new DungeonDelveMonster()
            {
                Id = GoldGolem,
                Name = "Golden Golem",
                EiconName = "dbgoldengolem",
                StrengthBonus = 0,//manually assigned strength
                Description = "A solid gold humanoid golem; Strength is based on carried treasure; On loss: lose all treasure",
                OnLoss = OnLossTrigger.LoseMoney
            };
            AllMonsters.Add(goldgol);
            DungeonDelveMonster harpymon = new DungeonDelveMonster()
            {
                Id = Harpy,
                Name = "Harpy",
                EiconName = "dbharpy",
                StrengthBonus = 1,
                StrengthMultiplier = 0.5,
                Description = "A loud and fierce humanoid bird woman; You only roll one die on combat; On loss: death",
                OnLoss = OnLossTrigger.Death
            };
            AllMonsters.Add(harpymon);
            DungeonDelveMonster giantwormmon = new DungeonDelveMonster()
            {
                Id = GiantWorm,
                Name = "Giant Worm",
                EiconName = "dbgiantworm",
                StrengthBonus = 2,
                Description = "A giant burrowing worm; On loss: advance 1d10 floors and lose items",
                OnLoss = OnLossTrigger.PitDrop
            };
            AllMonsters.Add(giantwormmon);
        }

        public DungeonDelveMonster GetRandomMonsterForFloor(System.Random rnd, int floor)
        {
            if (AllMonsters == null || AllMonsters.Count == 0)
                return null;

            List<int> eligibleIds = new List<int>();

            if (floor <= 7)
            {
                // Early floors:
                eligibleIds.AddRange(new[] { SkeletonKnight, PoisonFrog, GiantSnake, HordeOfGremlins, GiantBeast, GiantBeast, GiantBeast, Minotaur, GiantMushroom, Harpy, GangOfOrcs });
            }
            else if (floor <= 14)
            {
                // Mid floors:
                eligibleIds.AddRange(new[] { SkeletonKnight, PoisonFrog, GiantSnake, HordeOfGremlins, Minotaur, Minotaur, Minotaur, GiantMushroom, Harpy, GangOfOrcs, Wraith, ShadowWarrior, GiantWorm, Cockatrice, GiantBeast });
            }
            else
            {
                // Deep floors:
                eligibleIds.AddRange(new[] { SkeletonKnight, GiantSnake, GiantBeast, GoldGolem, Demon, Dragon, Wraith, ShadowWarrior, Cockatrice, GiantWorm, Minotaur });
            }

            if (eligibleIds.Count == 0)
            {
                return AllMonsters[rnd.Next(AllMonsters.Count)].Copy();
            }

            int selectedId = eligibleIds[rnd.Next(eligibleIds.Count)];
            return GetMonster(selectedId);
        }

        public DungeonDelveMonster GetMonster(int id)
        {
            return AllMonsters.FirstOrDefault(b => b.Id == id)?.Copy();
        }
    }

    public class DungeonDelveTrap
    {
        public int Id;
        public string Name;
        public string Description;
        public string EiconName;

        public string Print()
        {
            return "[eicon]" + EiconName + "[/eicon] " + Name + ": " + Description;
        }

        public DungeonDelveTrap Copy()
        {
            return (DungeonDelveTrap)this.MemberwiseClone();
        }
    }

    public class DungeonDelveTraps
    {
        public List<DungeonDelveTrap> AllTraps;

        public const int CrushingWall = 1, Acid = 2, Disintegration = 3, Pit = 4, PoisonGas = 5, Blade = 6;

        public DungeonDelveTraps()
        {
            AllTraps = new List<DungeonDelveTrap>();
            Initialize();
        }

        private void Initialize()
        {
            DungeonDelveTrap mon1 = new DungeonDelveTrap()
            {
                Id = CrushingWall,
                Name = "Crushing Wall Trap",
                EiconName = "dbcrushingwalltrap",
                Description = "Giant spiked wall; On failure: death"
            };
            AllTraps.Add(mon1);
            DungeonDelveTrap mon2 = new DungeonDelveTrap()
            {
                Id = Acid,
                Name = "Acid Trap",
                EiconName = "dbacidtrap",
                Description = "A corrosive acid trap; On failure: item destroyed"
            };
            AllTraps.Add(mon2);
            DungeonDelveTrap mon3 = new DungeonDelveTrap()
            {
                Id = PoisonGas,
                Name = "Poison Gas Trap",
                EiconName = "dbpoisongastrap",
                Description = "A toxic mist; On failure: poison"
            };
            AllTraps.Add(mon3);
            DungeonDelveTrap mon4 = new DungeonDelveTrap()
            {
                Id = Blade,
                Name = "Blade Trap",
                EiconName = "dbbladetrap",
                Description = "A heavy blade concealed in the stone; On failure: death"
            };
            AllTraps.Add(mon4);
            DungeonDelveTrap mon5 = new DungeonDelveTrap()
            {
                Id = Disintegration,
                Name = "Disintegration Trap",
                EiconName = "dbdisintegrationtrap",
                Description = "A magical antimatter spell; On failure: lose gold"
            };
            AllTraps.Add(mon5);
            DungeonDelveTrap mon6 = new DungeonDelveTrap()
            {
                Id = Pit,
                Name = "Pit Trap",
                EiconName = "dbpittrap",
                Description = "A surprising pit in the floor; On failure: fall 1d10 floors"
            };
            AllTraps.Add(mon6);
        }

        public DungeonDelveTrap GetRandomTrap(System.Random rnd, int floor)
        {
            if (AllTraps == null || AllTraps.Count == 0)
                return null;

            List<int> eligibleIds = new List<int>();

            eligibleIds.AddRange(new[] { CrushingWall, Acid, Disintegration, Pit, PoisonGas });//Blade,

            if (eligibleIds.Count == 0)
            {
                return AllTraps[rnd.Next(AllTraps.Count)].Copy();
            }

            int selectedId = eligibleIds[rnd.Next(eligibleIds.Count)];
            return GetTrap(selectedId);
        }

        public DungeonDelveTrap GetTrap(int id)
        {
            return AllTraps.FirstOrDefault(b => b.Id == id)?.Copy();
        }
    }
    #endregion
}

// ([color=yellow][color=green][b]Items:[/b][/color] [color=orange](1)[/color]??Flimsy Dagger, [color=orange](2)[/color]???Leather Armor,  [color=orange](3)[/color]??Big Backpack, [color=orange](4)[/color] ??Scroll of Doom, [color=orange](5)[/color]??Antidote, [color=orange](6)[/color]?? Antidote, [color=orange](7)[/color]??Everfull Antidote, [color=orange](8)[/color]??Diamond, [color=orange](9)[/color]??Key, [color=orange](10)[/color]??Dungeon Map, [color=orange](11)[/color]??Trap Kit[/color]),