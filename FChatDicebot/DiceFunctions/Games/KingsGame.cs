using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace FChatDicebot.DiceFunctions
{
    public class KingsGame : IGame
    {
        public string GetGameName()
        {
            return "KingsGame";
        }

        public int GetMaxPlayers()
        {
            return 20;
        }

        public int GetMinPlayers()
        {
            return 3;
        }

        public bool AllowAnte()
        {
            return false;
        }

        public bool UsesFlatAnte()
        {
            return false;
        }

        public bool KeepSessionDefault()
        {
            return true;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 0;
        }

        public string GetGameHelp()
        {
            string thisGameCommands = "showallnumbers, endround" +
                    "\n(as current player only): awardpoints # # (award points to players of these numbers. i.e.- !gc awardpoints 2 4)";
            string thisGameStartupOptions = "(none)" +
                    "\nThe default rules are: The king is chosen at random each round, the king ends the round by awarding points";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbkingsgame1b[/eicon][eicon]dbkingsgame2b[/eicon]";
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            if(session.KingsGamePlayers == null || session.KingsGamePlayers.Count < 2)
            {
                return "No players assigned yet.";
            }
            else
            {
                if (GetCurrentKing(session) != null)
                {
                    return "The current king is " + GetCurrentKing(session).Name + ". " + (session.KingsGamePlayers.Count - 1) + " other players have been given numbers.";
                }
                else
                {
                    return "The current king has not been assigned.";
                }
            }
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            session.RandomPlayerQueueData.AddNewPlayer(botMain.DiceBot.random, characterName);
            messageString = "";
            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            session.RandomPlayerQueueData.RemovePlayer(characterName);
            string returnString = "";
            if(session.KingsGamePlayers != null)
            {
                if(PlayerIsKing(session, characterName))
                {
                    returnString = "The king has left the game. The round will be reset.";
                    ResetGameRound(session);
                }
                else
                {
                    var chara = session.KingsGamePlayers.FirstOrDefault(a => a.Name == characterName);
                    if(chara != null)
                        returnString = "A player has left the game. They had number " + chara.Role + ".";
                }

                session.KingsGamePlayers = session.KingsGamePlayers.Where(a => a.Name != characterName).ToList();
            }

            return returnString;
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            botMain.SendMessageInChannel("[color=yellow]Selecting a new king and assigning numbers...[/color]", session.ChannelId);

            //int index = r.Next(playerNames.Count);

            //string selectedKing = playerNames[index];

            string selectedKing = session.RandomPlayerQueueData.GetNextPlayerSpin(r, "");//player who spins can be the king still // executingPlayer);

            //while (session.KingsGamePlayers != null && session.KingsGamePlayers.Count > 1 && 
            //    session.KingsGamePlayers.FirstOrDefault(a => a.Role == 0) != null &&
            //    session.KingsGamePlayers.FirstOrDefault(a => a.Role == 0).Name == selectedKing) //verify that this king was not selected last round
            //{
            //    index = r.Next(playerNames.Count);

            //    selectedKing = playerNames[index];
            //}

            int kingTextNumber = r.Next(7);
            string kingIntroText = "";
            switch(kingTextNumber)
            {
                case 0:
                    kingIntroText = "All hail king ";
                    break;
                case 1:
                    kingIntroText = "The new king is ";
                    break;
                case 2:
                    kingIntroText = "Your king is ";
                    break;
                case 3:
                    kingIntroText = "Behold king ";
                    break;
                case 4:
                    kingIntroText = "A new king has been chosen, ";
                    break;
                case 5:
                    kingIntroText = "This round's king is ";
                    break;
                case 6:
                    kingIntroText = "The king is ";
                    break;
                default:
                    kingIntroText = "The new king is ";
                    break;
            }


            int[] assignedNumbers = new int[playerNames.Count - 1];

            int createdNumbers = 0;

            List<int> includedInts = new List<int>();

            //assign a number to every remaining player
            for (int i = 0; i < assignedNumbers.Length; i++)
            {
                int assignedNumber = r.Next(assignedNumbers.Length - createdNumbers) + 1;

                int counter = 0;
                for (int q = 0; q < assignedNumber; q++ )
                {
                    counter += 1;
                    while (includedInts.Contains(counter))
                        counter += 1;
                }

                includedInts.Add(counter);
                assignedNumbers[i] = counter;
                createdNumbers++;
            }

            int createdNumberIndex = 0;
            //int playerNumber = 0;

            session.KingsGamePlayers = new List<KingsGamePlayer>();

            foreach (string player in playerNames)
            {
                if (player != selectedKing)// playerNumber != index)
                {
                    int playerAssignedNumber = assignedNumbers[createdNumberIndex];
                    string messageToPlayer = "King's Game: Your number is " + playerAssignedNumber + " for this round.";
                    createdNumberIndex++;
                    session.KingsGamePlayers.Add(new KingsGamePlayer()
                    {
                        Name = player,
                        Role = playerAssignedNumber
                    });

                    botMain.SendPrivateMessage(messageToPlayer, player);
                }
                else
                {
                    session.KingsGamePlayers.Add(new KingsGamePlayer()
                    {
                        Name = player,
                        Role = 0
                    });
                }
                //playerNumber++;
            }

            botMain.SendPrivateMessage("King's Game: You are the [b]king[/b] this round!\nGive your command to the other players using the numbers 1 - " + assignedNumbers.Length + " and then award points based on if they completed the tasks.", selectedKing);

            string outputString = "" + kingIntroText + Utils.GetCharacterUserTags(selectedKing) +
                "\nEveryone has been assigned their numbers and the king may make decrees using the numbers 1 - " + assignedNumbers.Length + "." +
                "\n..." +
                "\nNext Step: After the tasks are finished, the king must assign points with !gc awardpoints [player number(s)], or the round can be cancelled with !gc endround" + 
                "\n[sub]Note: If there is any confusion about who has what number you can use !gc showallnumbers[/sub]";
            
            session.State = DiceFunctions.GameState.GameInProgress;
            
            return outputString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        private string GetPlayerNumbers(GameSession session)
        {
            string playerRoles = "player roles not recorded";
            if(session.KingsGamePlayers != null && session.KingsGamePlayers.Count > 0)
            {
                playerRoles = "";
                foreach(KingsGamePlayer p in session.KingsGamePlayers)
                {
                    if (!string.IsNullOrEmpty(playerRoles))
                    {
                        playerRoles += "\n";
                    }
                    if (p.Role != 0)
                    {
                        playerRoles += Utils.GetCharacterUserTags(p.Name) + " is #" + p.Role;
                    }
                    else
                    {
                        playerRoles += Utils.GetCharacterUserTags(p.Name) + " is the [b]king[/b]";
                    }
                }
            }
            return playerRoles;
        }

        private string GetPlayerNameAndNumber(GameSession session, int playerNumer)
        {
            string playerString = "player number not found";
            if (session.KingsGamePlayers != null && session.KingsGamePlayers.Count > 0)
            {
                KingsGamePlayer p = session.KingsGamePlayers.FirstOrDefault(a => a.Role == playerNumer);
                if(p != null)
                {
                    playerString = "#" + playerNumer + ": " + Utils.GetCharacterUserTags(p.Name);
                }
            }
            return playerString;
        }

        public bool PlayerIsKing(GameSession session, string playerName)
        {
            if(session.KingsGamePlayers != null && session.KingsGamePlayers.Count > 0)
            {
                if(session.KingsGamePlayers.FirstOrDefault(a => a.Role == 0 && a.Name == playerName) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public KingsGamePlayer GetCurrentKing(GameSession session)
        {
            if (session.KingsGamePlayers == null || session.KingsGamePlayers.Count < 1)
                return null;

            KingsGamePlayer thisKing = session.KingsGamePlayers.FirstOrDefault(a => a.Role == 0);

            return thisKing;
        }

        public void ResetGameRound(GameSession session)
        {
            session.State = GameState.Unstarted;
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            string returnString = "";

            if(terms.Contains("showallnumbers"))
            {
                if(session.State == GameState.GameInProgress)
                {
                    string messageString = "Showing all information for this round of The King's Game:\n" + GetPlayerNumbers(session);
                    botMain.SendPrivateMessage(messageString, character);
                    returnString = Utils.GetCharacterUserTags(character) + " has been sent the information for this round's secret numbers.";
                }
                else
                {
                    returnString = "The King's Game is not already in progress, there are no numbers to show.";
                }
            }
            else if (terms.Contains("endround"))
            {
                returnString = "King's Game round has ended manually.";
                ResetGameRound(session);
            }
            else if (terms.Contains("awardpoints"))
            {
                if (session.State == GameState.GameInProgress  )
                {
                    if(PlayerIsKing(session, character))
                    {
                        if(session.KingsGamePlayers == null || session.KingsGamePlayers.Count < GetMinPlayers())
                        {
                            returnString = "Error: King's Game players list not set correctly.";
                        }
                        else
                        {
                            List<int> playerAwardsTemp = Utils.GetAllNumbersFromInputs(terms);
                            List<int> playerAwards = new List<int>();

                            //decrease all the numbers by 1 to match array indexes, rather than the card position for a player
                            if (playerAwardsTemp.Count > 0)
                            {
                                foreach (int i in playerAwardsTemp)
                                {
                                    if (i > 0 && i < session.KingsGamePlayers.Count)
                                        playerAwards.Add(i);
                                }
                            }

                            string playerAwardsList = "";
                            if(playerAwards.Count > 0)
                            {
                                foreach (int i in playerAwards)
                                {
                                    playerAwardsList += GetPlayerNameAndNumber(session, i);
                                    KingsGamePlayer player = session.KingsGamePlayers.FirstOrDefault(a => a.Role == i);
                                    if(player != null)
                                    {
                                        string chipsAward = diceBot.AddChips(player.Name, channel, 100, false);
                                        playerAwardsList += "\n" + chipsAward;
                                    }
                                    playerAwardsList += "\n";
                                }
                            }
                            else
                            {
                                playerAwardsList = "no one";
                            }


                            string messageString = "King " + Utils.GetCharacterUserTags(character) + " is awarding Points to :\n" + playerAwardsList;
                            
                            returnString = messageString + "\n This round has finished.";

                            ResetGameRound(session);
                        }
                    }
                    else
                    {
                        returnString = "Only the king can award points for the round.";
                    }
                }
                else
                {
                    returnString = "The King's Game is not already in progress, points cannot be awarded.";
                }
            }
            else
            {
                returnString = "A command for " + GetGameName() + " was not found.";
            }

            return returnString;
        }
    }

    public class KingsGamePlayer
    {
        public string Name;
        public int Role; //king = 0, # = higher
    }
}
