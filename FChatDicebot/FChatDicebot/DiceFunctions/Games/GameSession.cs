using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class GameSession
    {
        public string ChannelId;

        public IGame CurrentGame;

        public GameState State;

        public List<String> Players;
        //TODO: add to the game sessions a way for them to store their own data or interpret a shared field here
        public List<RouletteBetData> RouletteBets = new List<RouletteBetData>();
        public List<KingsGamePlayer> KingsGamePlayers = new List<KingsGamePlayer>();
        public BlackjackGameData BlackjackGameData = new BlackjackGameData();
        public LiarsDiceData LiarsDiceData = new LiarsDiceData();
        public SlamRollData SlamRollData = new SlamRollData();
        public RockPaperScissorsData RockPaperScissorsData = new RockPaperScissorsData();
        public PokerData PokerData = new PokerData();
        public MafiaData MafiaData = new MafiaData();
        public HighRollData HighRollData = new HighRollData();

        public PlayerRandomQueueData RandomPlayerQueueData = new PlayerRandomQueueData();

        public int Ante;
        public bool AnteSet = false;

        public int MinimumBetIncrement;

        public double CreationTime; //currently unused
        public double StartTime; //currently unused
        public double PausedTime;
        public bool Paused = false;

        public List<QueuedAction> QueuedActions = new List<QueuedAction>();

        public string RunGame(String executingCharacter, DiceBot diceBot, BotMain botMain)
        {
            StartTime = Utils.GetCurrentTimestampSeconds();

            string returnString = CurrentGame.RunGame(diceBot.random, executingCharacter, Players, diceBot, botMain, this);

            return returnString;
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, string[] terms, string[] rawTerms)
        {
            string returnString = "";
            if(CurrentGame == null)
            {
                returnString = "Error: CurrentGame not set on IssueGameCommand";
            }
            else if (terms.Contains("help"))
            {
                returnString = CurrentGame.GetGameHelp();
            }
            else if (terms.Contains("pause") || terms.Contains("pausegame"))
            {
                PauseGame(Utils.GetCurrentTimestampSeconds());

                returnString = "Game paused.";
            }
            else if (terms.Contains("unpause") || terms.Contains("unpausegame"))
            {
                UnpauseGame(Utils.GetCurrentTimestampSeconds());

                returnString = "Game unpaused.";
            }
            else if (terms.Contains("showchips") || terms.Contains("showchippiles"))
            {
                string output = "";
                if(Players == null || Players.Count() == 0)
                {
                    output = "Failed: No players found.";
                }
                else
                {
                    foreach (string player in Players)
                    {
                        ChipPile pile = diceBot.GetChipPile(player, channel, true);

                        if (pile != null)
                        {
                            if (!string.IsNullOrEmpty(output))
                                output += "\n";

                            output += Utils.GetCharacterUserTags(player) + "'s pile: [b]" + pile.Chips + "[/b] chips.";
                        }

                    }
                }

                returnString = "List of Chip Piles for players in " + CurrentGame.GetGameName() + ":\n" + output;
            }
            else if (terms.Contains("setante") || terms.Contains("ante"))
            {
                if (CurrentGame.UsesFlatAnte())
                {
                    int amount = Utils.GetNumberFromInputs(terms);
                    if (amount > 0 || terms.Contains("0"))
                    {
                        Ante = amount;
                        returnString = "Set the ante this session to " + Ante + " chips.";
                    }
                    else
                    {
                        returnString = "Failed: no positive ante amount was found.";
                    }
                }
                else
                    returnString = "Failed: this game does not allow ante";
            }
            else if(Paused)
            {
                returnString = "Failed: you cannot give commands while the game is paused. Use !gc unpause";
            }
            else
            {
                returnString = CurrentGame.IssueGameCommand(diceBot, botMain, character, channel, this, terms, rawTerms);
            }

            return returnString;
        }

        public void PauseGame(double currentTime)
        {
            PausedTime = currentTime;
            Paused = true;
        }

        public void UnpauseGame(double currentTime)
        {
            if (PausedTime > 0 && Paused)
            {

                double timeDifference = currentTime - PausedTime;

                if (timeDifference > 0)
                {
                    foreach (QueuedAction action in QueuedActions)
                    {
                        action.TriggerTime += timeDifference;
                    }
                }
            }
            PausedTime = 0;
            Paused = false;
        }

        public void UpdateGame(BotMain botMain, double currentTime)
        {
            if (CurrentGame != null && !Paused)
            {
                CurrentGame.Update(botMain, this, currentTime);
            }
        }

        public void AddQueuedAction(QueuedActionType actionType, string name, double triggerTime)
        {
            QueuedActions.Add(new QueuedAction() {
                Name = name,
                QueuedActionType = actionType,
                TriggerTime = triggerTime
            });
        }

        public List<QueuedAction> GetTriggeredActions(double currentTime)
        {
            List<QueuedAction> returnActions = null;

            if(QueuedActions != null && QueuedActions.Count > 0)
            {
                returnActions = QueuedActions.Where(a => a.TriggerTime <= currentTime).ToList();
            }

            return returnActions;
        }

        public void RemoveQueuedAction(QueuedActionType actionType, string name, double triggerTime)
        {
            int before = QueuedActions.Count();
            QueuedActions.RemoveAll(a => a.QueuedActionType == actionType && a.Name == name && a.TriggerTime == triggerTime);
            Console.WriteLine("removedQueuedActionA " + before + " after " + QueuedActions.Count() + " = " + actionType + " " + name);
        }

        public void RemoveQueuedAction(QueuedAction action)
        {
            int before = QueuedActions.Count();
            QueuedActions.RemoveAll(a => a.QueuedActionType == action.QueuedActionType && a.Name == action.Name && a.TriggerTime == action.TriggerTime);
            Console.WriteLine("removedQueuedActionB " + before + " after " + QueuedActions.Count() + " = " + action.QueuedActionType + " " + action.Name);
        }

        public string GetStatus(double secondOnTimer = 0)
        {
            string readyToStartString = "";
            if (Players.Count >= CurrentGame.GetMinPlayers() && secondOnTimer == 0)
            {
                readyToStartString += "[b] (Ready to start!)[/b]";
            }
            else if(secondOnTimer > 0)
            {
                readyToStartString += "[i] (" + secondOnTimer.ToString("N3") + " seconds until ready to start)[/i]";
            }
            
            string gameRelatedInfo = CurrentGame.GameStatus(this);
            if (!string.IsNullOrEmpty(gameRelatedInfo))
                gameRelatedInfo = "\n" + gameRelatedInfo;

            return string.Format("[b]Game Name: {0}[/b] \nMin Players: {1}, Max Players: {2}, Keep Session: {3}, Ante: {4}\n" + 
                "Game State: [b]{5}[/b]\nCurrent Players: {6}{7}" + 
                "{8}", 
                CurrentGame.GetGameName(), CurrentGame.GetMinPlayers(), CurrentGame.GetMaxPlayers(), CurrentGame.KeepSessionDefault(), Ante,
                State.ToString(), Utils.PrintList(Players), readyToStartString, gameRelatedInfo);
        }

        public bool HasEnoughPlayersToStart()
        {
            return Players != null && CurrentGame != null && Players.Count >= CurrentGame.GetMinPlayers();
        }

        public static string GetGameHelp(string gameName, string gameCommands, string startupOptions, bool useStartingBet, bool rouletteBet)
        {
            string returnString = "[b]GameCommands for " + gameName + "[/b] (!gc):\n" + gameCommands +
                "\n[b]Startup Options:[/b] " + startupOptions +
                "\nJoin the game with !joingame " + gameName + (useStartingBet? " # (your bet amount)":"") + (rouletteBet? " (bet type)" : "") + 
                ", leave the game with !leavegame " + gameName + "," +
                "\nStart the game with !startgame " + gameName + ", view the game's current state with !gamestatus " + gameName + ", remove the entire game session with !cancelgame " + gameName;

            return returnString;
        }

        public bool AddGameData(object gameDataObject)
        {
            if (typeof(RouletteBetData) == gameDataObject.GetType())
            {
                RouletteBets.Add((RouletteBetData)gameDataObject);
                return true;
            }
            else
                return true; 
        }

        public bool RemoveRouletteBet(string characterName)
        {
            if(RouletteBets != null && RouletteBets.Count > 0)
                RouletteBets.RemoveAll(a => a.characterName == characterName);

            return true;
        }
    }

    public enum GameState
    {
        NONE,
        Unstarted,
        Started,
        Waiting,
        Finished,
        GameInProgress
    }

    public enum ContinueGameType
    {
        NONE,
        AwardPointsKingsGame
    }


    public class PlayerRandomQueueData
    {
        public List<String> PlayerQueue;
        public int currentQueueIndex;
        public string LastPlayerSpun;

        public bool shuffled;

        public PlayerRandomQueueData()
        {
            PlayerQueue = new List<string>();
            shuffled = false;
            currentQueueIndex = 0;
        }

        public string GetNextPlayerSpin(Random rnd, string playerName)
        {
            if (!shuffled)
            {
                ShuffleAllPlayers(rnd, false);
                shuffled = true;
            }

            string thisPlayer = PlayerQueue[currentQueueIndex];

            if (thisPlayer == playerName)
            {
                currentQueueIndex++;
                if (currentQueueIndex >= PlayerQueue.Count)
                {
                    currentQueueIndex = 0;
                }
                thisPlayer = PlayerQueue[currentQueueIndex];
            }

            currentQueueIndex++;
            if (currentQueueIndex >= PlayerQueue.Count)
            {
                ShuffleAllPlayers(rnd, true);
            }
            LastPlayerSpun = thisPlayer;
            return thisPlayer;
        }
        
        public void AddNewPlayer(Random rnd, string playerName)
        {
            int num = rnd.Next(0, PlayerQueue.Count);

            if (num < currentQueueIndex)
            {
                currentQueueIndex++;
            }

            PlayerQueue.Insert(num, playerName);
        }

        public void RemovePlayer(string playername)
        {
            int index = PlayerQueue.IndexOf(playername);
            if (index >= 0)
            {
                if (index < currentQueueIndex)
                {
                    currentQueueIndex -= 1;
                }
                PlayerQueue.RemoveAt(index);

                if (currentQueueIndex >= PlayerQueue.Count)
                {
                    Random rnd = new Random();
                    ShuffleAllPlayers(rnd, true);
                }
            }
        }

        public void ShuffleAllPlayers(Random rnd, bool moveLastTo2Plus)
        {
            currentQueueIndex = 0;
            if (PlayerQueue.Count >= 0)
            {
                List<string> newPlayerNames = new List<string>();
                int startingPlayerCount = PlayerQueue.Count;
                string movedPlayer = null;
                for (int i = 0; i < startingPlayerCount; i++)
                {
                    if (movedPlayer != null)
                    {
                        PlayerQueue.Add(movedPlayer);
                        movedPlayer = null;
                    }
                    if (i == 0 && moveLastTo2Plus && PlayerQueue.Count > 1)
                    {
                        movedPlayer = PlayerQueue[PlayerQueue.Count - 1];
                        PlayerQueue.RemoveAt(PlayerQueue.Count - 1);
                    }

                    int num = rnd.Next(0, PlayerQueue.Count);

                    string nextInQueue = PlayerQueue[num];
                    PlayerQueue.RemoveAt(num);
                    newPlayerNames.Add(nextInQueue);
                }
                PlayerQueue = newPlayerNames;
            }
        }
    }

    public class QueuedAction
    {
        public double TriggerTime;
        public string Name;
        public QueuedActionType QueuedActionType;
    }

    public enum QueuedActionType
    {
        NONE,
        AdvanceGamePhase
    }
}



