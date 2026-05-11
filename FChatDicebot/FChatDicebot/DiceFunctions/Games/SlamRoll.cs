using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace FChatDicebot.DiceFunctions
{
    public class SlamRoll : IGame
    {
        public string CommandsList = "(!gc attack, !gc slam, !gc forfeit)";

        public string GetGameName()
        {
            return "SlamRoll";
        }

        public int GetMaxPlayers()
        {
            return 2;
        }

        public int GetMinPlayers()
        {
            return 2;
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
            string thisGameCommands = "SetAnte #, ShowPlayers, ResetRound, SetHealth # (player name), SetLives # (player name), SetTurn (player name), SetGrowingTwos (on or off)" +
                    "\n(as current player only): attack, slam, forfeit";
            string thisGameStartupOptions = "lives# (set starting lives to #: 1, 2, 3, or 4) growingtwos (growing twos on) naturaltwos (growing twos off) singleslam (limited to 1 slam attack) rerollinit (reroll init instead of loser getting it) " +
                "\nThe default rules are: starting lives: 1, starting health: 500, unlimited slams, growing twos on, initiative passes to losing player";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbslamroll1[/eicon][eicon]dbslamroll2[/eicon]";
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            string playersList = GetPlayerList(session);
            return (string.IsNullOrEmpty(playersList)? "(Players have not started the game yet)" : playersList) + "\n" + GetRulesText(session);
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            messageString = "";
            
            if (!session.SlamRollData.RulesAssigned)
            {
                session.SlamRollData.RulesAssigned = true;

                if(ante > 0)
                {
                    messageString += "(ante: " + ante + ")";
                }

                if (terms.Contains("lives1"))
                {
                    session.SlamRollData.StartingLives = 1;
                    messageString += "(starting lives 1)";
                }
                else if (terms.Contains("lives2"))
                {
                    session.SlamRollData.StartingLives = 2;
                    messageString += "(starting lives 2)";
                }
                else if (terms.Contains("lives3"))
                {
                    session.SlamRollData.StartingLives = 3;
                    messageString += "(starting lives 3)";
                }
                else if (terms.Contains("lives4"))
                {
                    session.SlamRollData.StartingLives = 4;
                    messageString += "(starting lives 4)";
                }

                if (terms.Contains("growingtwos"))
                {
                    session.SlamRollData.GrowingTwos = true;
                    messageString += "(2's rolled in a row take multiple dice)";
                }

                if (terms.Contains("naturaltwos"))
                {
                    session.SlamRollData.GrowingTwos = false;
                    messageString += "(2's rolled in a row are unchanged)";
                }

                if (terms.Contains("rerollinit"))
                {
                    session.SlamRollData.RollInitiativeAfterRound = true;
                    messageString += "(rerolling initiative after round)";
                }
                
                if (terms.Contains("singleslam"))
                {
                    session.SlamRollData.AllowMultipleSlams = false;
                    messageString += "(one slam)";
                }
            }

            //botMain.SendMessageInChannel("debug parameters: " + messageString, session.ChannelId);
            
            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            string returnString = "A player left the game, resetting the round.";

            ResetGameRound(session);
            //if(session.KingsGamePlayers != null)
            //{
            //    if(PlayerIsKing(session, characterName))
            //    {
            //        returnString = "The king has left the game. The round will be reset.";
            //        ResetGameRound(session);
            //    }
            //    else
            //    {
            //        var chara = session.KingsGamePlayers.FirstOrDefault(a => a.Name == characterName);
            //        if(chara != null)
            //            returnString = "A player has left the game. They had number " + chara.Role + ".";
            //    }

            //    session.KingsGamePlayers = session.KingsGamePlayers.Where(a => a.Name != characterName).ToList();
            //}

            return returnString;
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            if(session.Ante > 0)
            {
                string outputErrorAnte = "";
                foreach(string player in session.Players)
                {
                    ChipPile playerPile = diceBot.GetChipPile(player, session.ChannelId, true);
                    if(playerPile.Chips < session.Ante)
                    {
                        if(!string.IsNullOrEmpty(outputErrorAnte))
                        {
                            outputErrorAnte += ", ";
                        }
                        outputErrorAnte = Utils.GetCharacterUserTags(player) + " cannot afford the ante of " + session.Ante + " chips. (" + playerPile.Chips + " held)";
                    }
                }
                if(!string.IsNullOrEmpty(outputErrorAnte))
                {
                    session.State = GameState.Unstarted;
                    return "Session for SlamRoll failed to start: " + outputErrorAnte;
                }
            }

            botMain.SendMessageInChannel("[color=yellow]A new [b]Slam Roll[/b] fight is starting...[/color]", session.ChannelId);

            if(session.SlamRollData == null)
            {
                session.SlamRollData = new SlamRollData();
            }
            else if (session.SlamRollData.SlamRollPlayers.Count > 0)
            {
                SlamRollData d = new SlamRollData();
                d.RollInitiativeAfterRound = session.SlamRollData.RollInitiativeAfterRound;
                d.AllowMultipleSlams = session.SlamRollData.AllowMultipleSlams;
                d.StartingLives = session.SlamRollData.StartingLives;
                session.SlamRollData = d;
            }

            string playerIntrosOutput = "";
            int playerPosition = 0;

            foreach(string playerName in playerNames)
            {
                SlamRollPlayer p = new SlamRollPlayer();
                p.Name = playerName;
                p.StageName = playerName;
                p.Health = session.SlamRollData.StartingHealth;
                p.Lives = session.SlamRollData.StartingLives;
                p.Initiative = playerPosition;
                p.Slam = true;
                p.Active = true;

                if(!string.IsNullOrEmpty(playerIntrosOutput))
                {
                    playerIntrosOutput += "\n";
                }
                playerPosition += 1;

                playerIntrosOutput += Utils.GetCharacterIconTags(playerName) + " has [b]entered the ring[/b]! ";

                string betstring = "";

                if (session.Ante > 0)
                {
                    betstring = diceBot.BetChips(playerName, session.ChannelId, session.Ante, false) + "\n";
                }

                playerIntrosOutput += betstring;

                session.SlamRollData.SlamRollPlayers.Add(p);
            }

            string rollsOutput = "";
            SlamRollPlayer highestInitiativePlayer = RollHighestInitiativePlayer(session, diceBot, out rollsOutput);
            playerIntrosOutput += rollsOutput;
            
            session.SlamRollData.CurrentPlayerIndex = session.SlamRollData.SlamRollPlayers.IndexOf(highestInitiativePlayer);

            //botMain.SendPrivateMessage("You are the king this round!\nGive your command to the other players using the numbers 1 - " + assignedNumbers.Length + " and then award points based on if they completed the tasks.", selectedKing);

            string outputString = "" + playerIntrosOutput;// + "\n" + turnsOutput;
            
            session.State = DiceFunctions.GameState.GameInProgress;
            
            return outputString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public SlamRollPlayer RollHighestInitiativePlayer(GameSession session, DiceBot diceBot, out string outputString)
        {
            outputString = "";

            if (session.SlamRollData == null || session.SlamRollData.SlamRollPlayers == null || session.SlamRollData.SlamRollPlayers.Count < 2)
                return null;
            if (!session.SlamRollData.RollForFirstTurn)
                return session.SlamRollData.SlamRollPlayers[0];

            int highestRoll = 0;
            foreach (SlamRollPlayer player in session.SlamRollData.SlamRollPlayers)
            {
                if (session.SlamRollData.RollForFirstTurn)
                {
                    DiceRoll initRoll = new DiceRoll(player.Name, session.ChannelId, diceBot) { DiceSides = 100, DiceRolled = 1 };
                    initRoll.Roll(diceBot.random);
                    player.Initiative = (int) initRoll.Total;
                    outputString += "\n" + Utils.GetCharacterUserTags(player.Name) + " rolled [color=gray]" + initRoll.ResultString() + " initiative[/color]";

                    if (highestRoll < initRoll.Total)
                    {
                        highestRoll = (int) initRoll.Total;
                    }
                }
            }
            
            SlamRollPlayer highestInitiativePlayer = session.SlamRollData.SlamRollPlayers.FirstOrDefault(a => a.Initiative == highestRoll);

            if (session.SlamRollData.SlamRollPlayers.Count(a => a.Initiative == highestRoll) > 1)
            {
                int ties = session.SlamRollData.SlamRollPlayers.Count(a => a.Initiative == highestRoll);
                int winnerSpot = diceBot.random.Next(ties);

                List<SlamRollPlayer> tiedPlayers = session.SlamRollData.SlamRollPlayers.Where(a => a.Initiative == highestRoll).ToList();
                highestInitiativePlayer = tiedPlayers[winnerSpot];

                outputString += "\nThey're evenly matched, but " + Utils.GetCharacterUserTags(highestInitiativePlayer.Name) + " seizes the initiative!";
            }

            outputString += "\n" + Utils.GetCharacterUserTags(highestInitiativePlayer.Name) + " has the first turn. [sub]" + CommandsList + "[/sub]";

            return highestInitiativePlayer;
        }

        public string GetCurrentActivePlayerName(GameSession session)
        {
            if (session.SlamRollData.SlamRollPlayers == null || session.SlamRollData.CurrentPlayerIndex < 0 || session.SlamRollData.CurrentPlayerIndex > session.SlamRollData.SlamRollPlayers.Count - 1)
                return "invalid data";

            return session.SlamRollData.SlamRollPlayers[session.SlamRollData.CurrentPlayerIndex].Name;
        }

        public SlamRollPlayer GetSlamRollPlayerByName(GameSession session, string name)
        {
            return session.SlamRollData.SlamRollPlayers.FirstOrDefault(a => a.Name == name);
        }

        private void MoveActiveTurnToNextPlayer(System.Random random, GameSession session)
        {
            if (session.SlamRollData == null || session.SlamRollData.SlamRollPlayers == null || session.SlamRollData.SlamRollPlayers.Count(a => a.Active && !a.Eliminated) <= 1)
                return;

            if (session.SlamRollData.CurrentPlayerIndex < 0)
                session.SlamRollData.CurrentPlayerIndex = random.Next(session.SlamRollData.SlamRollPlayers.Count);

            AdvanceActivePlayerOneSpotOnRoster(session);

            while (!session.SlamRollData.SlamRollPlayers[session.SlamRollData.CurrentPlayerIndex].Active || session.SlamRollData.SlamRollPlayers[session.SlamRollData.CurrentPlayerIndex].Eliminated)
            {
                AdvanceActivePlayerOneSpotOnRoster(session);
            }
        }

        private void AdvanceActivePlayerOneSpotOnRoster(GameSession session)
        {
            if (session.SlamRollData.CurrentPlayerIndex >= session.SlamRollData.SlamRollPlayers.Count - 1)
                session.SlamRollData.CurrentPlayerIndex = 0;
            else
                session.SlamRollData.CurrentPlayerIndex = session.SlamRollData.CurrentPlayerIndex + 1;
        }

        public void ResetGameRound(GameSession session)
        {
            session.State = GameState.Unstarted;
        }

        public string SetNewPlayerHealthTotal(int newHealth, DiceBot diceBot, GameSession session, SlamRollPlayer player, int setTwosRolled, bool fromAttack = true)
        {
            string result = "";

            result = Utils.GetCharacterUserTags(player.Name) + " was set to [color=yellow]" + newHealth + " health[/color].";// +(fromAttack ? " from that attack!" : ".");

            player.Health = newHealth;
            if(setTwosRolled >= 0)
                player.TwosRolled = setTwosRolled;

            if(player.Health == 2)
            {
                player.TwosRolled += 1;
                result += " (they have rolled [b]" + player.TwosRolled + "[/b] two" + (player.TwosRolled > 1 ? "s" : "") + ")";
            }
            else if(player.Health <= 1)
            {
                player.Lives--;
                if (player.Lives <= 0)
                {
                    player.Eliminated = true;
                    result += "\n ...And they were [color=red]ELIMINATED[/color]!";

                    SlamRollPlayer winner = null;
                    if(MatchFinished(session, out winner))
                    {
                        result += "\n" + EndMatch(diceBot, session, winner, true);
                    }
                }
                else
                {
                    result += "\n ...And they [color=red]LOST A LIFE[/color]! (" + player.Lives + " " + (player.Lives > 1? "lives": "life") + " remaining)";

                    if(session.SlamRollData.ResetBothPlayersForRound)
                    {
                        //player.Health = diceBot.random.Next(session.SlamRollData.StartingHealth) + 1;
                        //if (player.Health == 1)
                        //    player.Health = 2;
                        foreach(SlamRollPlayer playerReset in session.SlamRollData.SlamRollPlayers)
                        {
                            result += "\n" + SetNewPlayerHealthTotal(session.SlamRollData.StartingHealth, diceBot, session, playerReset, 0, false);
                        }
                    }
                    else
                    {
                        result += "\n" + SetNewPlayerHealthTotal(session.SlamRollData.StartingHealth, diceBot, session, player, 0, false);
                    }

                    if (session.SlamRollData.RollInitiativeAfterRound)
                    {
                        string outputInitiative = "";
                        SlamRollPlayer highestInitNew = RollHighestInitiativePlayer(session, diceBot, out outputInitiative);
                        int newHighestPlayerIndex = session.SlamRollData.SlamRollPlayers.IndexOf(highestInitNew);
                        session.SlamRollData.CurrentPlayerIndex = newHighestPlayerIndex;

                        result += "\n[color=yellow]Rerolling Initiative...[/color]" + outputInitiative;
                    }
                    else if(fromAttack)
                    {
                        //SlamRollPlayer otherPlayer = session.SlamRollData.SlamRollPlayers.FirstOrDefault(a => a.Name != player.Name);
                        //if(otherPlayer != null)
                        //{
                        //    result += "\nIt is now " + Utils.GetCharacterUserTags(otherPlayer.Name) + "'s turn";
                        //}  
                        result += "\nIt is now " + Utils.GetCharacterUserTags(player.Name) + "'s turn";
                    }
                    //result += " (new health total: " + player.Health + ")";
                }
            }
            return result;
        }

        public bool MatchFinished(GameSession session, out SlamRollPlayer winner)
        {
            winner = null;

            if(session.SlamRollData != null && session.SlamRollData.SlamRollPlayers != null && session.SlamRollData.SlamRollPlayers.Count > 1)
            {
                if (session.SlamRollData.SlamRollPlayers.Count(a => !a.Eliminated) == 1)
                {
                    winner = session.SlamRollData.SlamRollPlayers.FirstOrDefault(a => !a.Eliminated);
                    return true;
                }
            }
            return false;
        }

        public string EndMatch(DiceBot bot, GameSession session, SlamRollPlayer winner, bool finishedByElimination)
        {
            string output = "This match is over. ";
            if(winner != null)
            {
                output += "The winner is " + Utils.GetCharacterIconTags(winner.Name) + "!";
            }

            output += " " + GetFlavorTextForMatch(bot.random, finishedByElimination);

            if(session.Ante > 0)
            {
                output += "\n" + bot.ClaimPot(winner.Name, session.ChannelId, 1);
            }

            session.State = GameState.Finished;

            bot.RemoveGameSession(session.ChannelId, session.CurrentGame);
            return output;
        }

        public static string GetFlavorTextForAttack(System.Random random, bool criticalAttack)
        {
            string flavorText = "";
            if (criticalAttack)
            {
                int flavorRoll = random.Next(6);
                switch (flavorRoll)
                {
                    case 0:
                        flavorText = " [sub]Is that even legal!?[/sub]";
                        break;
                    case 1:
                        flavorText = " [sub]That must have hurt...[/sub]";
                        break;
                    case 2:
                        flavorText = " [sub]Ouch.[/sub]";
                        break;
                    case 3:
                        flavorText = " [sub]They're barely standing![/sub]";
                        break;
                    case 4:
                        flavorText = " [sub]What an attack![/sub]";
                        break;
                    case 5:
                        flavorText = " [sub]I can't bear to watch...[/sub]";
                        break;
                    case 6:
                        flavorText = " [sub]This is what the fans love to see![/sub]";
                        break;
                }
            }

            return flavorText;
        }

        public static string GetFlavorTextForMatch(System.Random random, bool closeMatch)
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
            SlamRollData dat = session.SlamRollData;
            string rules = string.Format("Starting Health: {0}, Starting Lives: {1}, Reroll Init: {2}, Slam Threshold: {3}, Single Slam: {4}, Growing Twos: {5}", dat.StartingHealth, dat.StartingLives, dat.RollInitiativeAfterRound, dat.SlamThreshold, !dat.AllowMultipleSlams, dat.GrowingTwos);
            return rules;
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            if(session.State != GameState.GameInProgress)
            {
                return "Game commands for " + GetGameName() + " only work while the game is running.";
            }
            else if(session.SlamRollData.SlamRollPlayers.Count(a => a.Name == character) < 1)
            {
                return "Game commands for " + GetGameName() + " can only be used by characters who are playing the game.";
            }

            bool characterIsCurrentActivePlayer = character == GetCurrentActivePlayerName(session);

            string returnString = "";
            if (terms.Contains("resetround"))
            {
                session.SlamRollData = new SlamRollData();
                ResetGameRound(session);
                returnString = "The session has been reset. Start again with !startgame.";
            }
            else if (terms.Contains("showplayers"))
            {
                string playerList = GetPlayerList(session);

                returnString = playerList;
            }
            else if (terms.Contains("attack") || terms.Contains("slam"))
            {
                if(!characterIsCurrentActivePlayer)
                {
                    returnString = "It is currently " + Utils.GetCharacterUserTags(GetCurrentActivePlayerName(session)) + "'s turn.";
                }
                else
                {
                    if(terms.Contains("attack"))
                    {
                        SlamRollPlayer targetPlayer = session.SlamRollData.SlamRollPlayers.FirstOrDefault(a => a.Name != character);

                        if(targetPlayer == null)
                        {
                            returnString = "Error: Target not found";
                        }
                        else
                        {
                            DiceRoll dmgRoll = new DiceRoll() { DiceSides = targetPlayer.Health, DiceRolled = 1 };

                            if(session.SlamRollData.GrowingTwos && targetPlayer.TwosRolled > 0)
                            {
                                dmgRoll.KeepLowest = 1;
                                dmgRoll.DiceRolled = 1 + targetPlayer.TwosRolled;
                            }

                            dmgRoll.Roll(diceBot.random);

                            int newHealthRoll = (int) dmgRoll.Total;// dmgRoll.Total; //diceBot.random.Next(targetPlayer.Health) + 1;

                            string flavorText = GetFlavorTextForAttack(diceBot.random, newHealthRoll <= .25 * targetPlayer.Health);

                            string actionString = Utils.GetCharacterUserTags(character) + " attacks! " + dmgRoll.ResultString() + " " 
                                //+ Utils.GetCharacterUserTags(targetPlayer.Name) + " has been reduced to " + newHealthRoll + " health!" 
                                + flavorText;

                            MoveActiveTurnToNextPlayer(diceBot.random, session);

                            string setResult = SetNewPlayerHealthTotal(newHealthRoll, diceBot, session, targetPlayer, -1, true);
                            
                            returnString = actionString + "\n" + setResult;
                        }
                    }
                    else if (terms.Contains("slam"))
                    {
                        SlamRollPlayer targetPlayer = session.SlamRollData.SlamRollPlayers.FirstOrDefault(a => a.Name != character);
                        SlamRollPlayer thisPlayer = GetSlamRollPlayerByName(session, character);

                        if (targetPlayer == null)
                        {
                            returnString = "Error: Target not found";
                        }
                        else if (thisPlayer == null)
                        {
                            returnString = "Error: Acting player not found";
                        }
                        else if(!session.SlamRollData.AllowMultipleSlams && thisPlayer.Slam == false)
                        {
                            returnString = "You have [b]already used[/b] your slam attack!";
                        }
                        else if (targetPlayer.Health <= session.SlamRollData.SlamThreshold && targetPlayer.Health < thisPlayer.Health)
                        {
                            thisPlayer.Slam = false;
                            DiceRoll dmgRoll = new DiceRoll() { DiceSides = session.SlamRollData.SlamDieSides, DiceRolled = 1 };

                            if (session.SlamRollData.GrowingTwos && targetPlayer.TwosRolled > 0)
                            {
                                dmgRoll.KeepLowest = 1;
                                dmgRoll.DiceRolled = 1 + targetPlayer.TwosRolled;
                            }

                            dmgRoll.Roll(diceBot.random);

                            int newHealthRoll = (int) dmgRoll.Total;// diceBot.random.Next(session.SlamRollData.SlamDieSides) + 1;

                            int slamDamage = targetPlayer.Health - newHealthRoll;

                            string flavorText = GetFlavorTextForAttack(diceBot.random, true);

                            string actionString = Utils.GetCharacterUserTags(character) + " [b]SLAMS[/b]! " + dmgRoll.ResultString() + " " 
                                //+ Utils.GetCharacterUserTags(targetPlayer.Name) + " has been reduced to " + newHealthRoll + " health!" 
                                + flavorText;

                            string setResult = SetNewPlayerHealthTotal(newHealthRoll, diceBot, session, targetPlayer, -1, true);

                            MoveActiveTurnToNextPlayer(diceBot.random, session);

                            string setSelfResult = "";
                            if(newHealthRoll != 1)
                            {
                                setSelfResult = "\nThe impact from their [b]slam[/b] returned: " + SetNewPlayerHealthTotal(thisPlayer.Health - slamDamage, diceBot, session, thisPlayer, -1, true);
                            }

                            returnString = actionString + "\n" + setResult + setSelfResult;
                        }
                        else
                        {
                            returnString = "The target has too much health to be slammed. Reduce them to [color=yellow]" + session.SlamRollData.SlamThreshold + " health[/color] or lower first, and have a higher health total than them.";
                        }
                    }
                }
            }
            else if (terms.Contains("sethealth"))
            {
                if (terms.Length < 2)
                {
                    returnString = "Error: improper command format. Use 'sethealth # (playername)' with # as the number of health and (playername) as the user's full display name.";
                }
                else
                {
                    string allInputs = Utils.GetUserNameFromFullInputs(rawTerms);
                    string playerName = allInputs.Substring(allInputs.IndexOf(' ')).Trim();
                    if(terms.Length == 2)
                    {
                        playerName = character;
                    }
                    else
                        playerName = playerName.Substring(playerName.IndexOf(' ')).Trim();

                    int inputNumber = Utils.GetNumberFromInputs(terms);

                    SlamRollPlayer relevantPlayer = GetSlamRollPlayerByName(session, playerName);

                    if (inputNumber <= 0)
                        returnString = "Error: Cannot set health to 0 or less.";
                    else if (relevantPlayer != null)
                    {
                        string setResult = SetNewPlayerHealthTotal(inputNumber, diceBot, session, relevantPlayer, -1, false);
                        returnString = setResult;
                    }
                    else
                    {
                        returnString = "Player name (" + playerName + ") was invalid.";
                    }
                }
            }
            else if (terms.Contains("setlives"))
            {
                if(terms.Length < 2)
                {
                    returnString = "Error: improper command format. Use 'setlives # (playername)' with # as the number of lives and (playername) as the user's full display name.";
                }
                else
                {
                    string allInputs = Utils.GetUserNameFromFullInputs(rawTerms);
                    string playerName = allInputs.Substring(allInputs.IndexOf(' ')).Trim();
                    if (terms.Length == 2)
                    {
                        playerName = character;
                    }
                    else
                        playerName = playerName.Substring(playerName.IndexOf(' ')).Trim();
                    //playerName = playerName.Substring(playerName.IndexOf(' ')).Trim();

                    int inputNumber = Utils.GetNumberFromInputs(terms);

                    SlamRollPlayer relevantPlayer = GetSlamRollPlayerByName(session, playerName);

                    if (inputNumber <= 0)
                        returnString = "Error: Cannot set lives to 0 or less.";
                    if (relevantPlayer != null)
                    {
                        relevantPlayer.Lives = inputNumber;
                        returnString = Utils.GetCharacterUserTags(relevantPlayer.Name) + " was set to " + inputNumber + " lives.";
                    }
                    else
                    {
                        returnString = "Player name (" + playerName + ") was invalid.";
                    }
                }
            }
            else if (terms.Contains("setturn"))
            {
                if (terms.Length < 2)
                {
                    returnString = "Error: improper command format. Use 'setturn (playername)' with (playername) as the user's full display name.";
                }
                else
                {
                    string allInputs = Utils.GetUserNameFromFullInputs(rawTerms);
                    string playerName = allInputs.Substring(allInputs.IndexOf(' ')).Trim();

                    SlamRollPlayer relevantPlayer = GetSlamRollPlayerByName(session, playerName);

                    if (relevantPlayer != null)
                    {
                        session.SlamRollData.CurrentPlayerIndex = session.SlamRollData.SlamRollPlayers.IndexOf(relevantPlayer);
                        returnString = "It was set to " + Utils.GetCharacterUserTags(playerName) + "'s turn.";
                    }
                    else
                    {
                        returnString = "Player name (" + playerName + ") was invalid.";
                    }
                }
            }
            else if (terms.Contains("forfeit"))
            {
                SlamRollPlayer otherPlayer = session.SlamRollData.SlamRollPlayers.FirstOrDefault(a => a.Name != character);

                if (otherPlayer != null)
                {
                    string endResult = EndMatch(diceBot, session, otherPlayer, false);

                    returnString = Utils.GetCharacterUserTags(character) + " has forefeit the match.\n" + endResult;
                }
                else
                {
                    returnString = "Error: One or more of the players was invalid. You may have to !cancelgame and rejoin.";
                }
            }
            else if (terms.Contains("setgrowingtwos"))
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
                        if(trueFalse == "on")
                        {
                            setValue = true;
                        }
                    }

                    if (successfulParse)
                    {
                        session.SlamRollData.GrowingTwos = setValue;
                        returnString = "'Growing Twos' rule set to was set to " + (setValue? "ON" : "OFF");
                    }
                    else
                    {
                        returnString = "Error: Input was invalid. GrowingTwos must be set to on/ true, or off/ false";
                    }
                }
            }
            else
            {
                returnString = "A command for " + GetGameName() + " was not found.";
            }
            
            return returnString;

        }
    }

    public class SlamRollData
    {
        public bool RulesAssigned = false;

        public bool ResetBothPlayersForRound = true;
        //public bool FirstStrikeHalf = false;
        public bool AllowMultipleSlams = true;

        //public int MaxTwos = 99;
        public bool GrowingTwos = true;

        public bool RollForFirstTurn = true;
        public bool NotifyOfSlamAvailable = true;
        
        public int SlamThreshold = 166;
        public int StartingHealth = 500;
        public int SlamDieSides = 2; //on a 1 the player is defeated
        public int StartingLives = 1;

        public bool RollInitiativeAfterRound = false;

        public int CurrentPlayerIndex = 0;
        public List<SlamRollPlayer> SlamRollPlayers = new List<SlamRollPlayer>();
    }

    public class SlamRollPlayer
    {
        public bool Active;
        public bool Eliminated;
        public string Name;
        public string StageName;
        public int Health;
        public bool Slam;
        public int TwosRolled;
        public int Lives;
        public int Initiative;

        public override string ToString()
        {
            return StageName + " ([color=yellow]" + Health + " health[/color], [color=gray]" + Initiative + " initiative[/color]" + 
                (Lives > 1? ", " + Lives.ToString() + " lives" : "") + ")" + 
                (Active ? "" : " (inactive)") + (Eliminated ? "(eliminated)" : "");
        }
    }
}
