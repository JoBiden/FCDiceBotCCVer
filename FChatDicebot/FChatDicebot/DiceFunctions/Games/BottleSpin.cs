using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class BottleSpin : IGame
    {
        public string GetGameName()
        {
            return "BottleSpin";
        }

        public int GetMaxPlayers()
        {
            return 20;
        }

        public int GetMinPlayers()
        {
            return 2;
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
            string thisGameCommands = "lastspin";
            string thisGameStartupOptions = "(none)" +
                "\nThe default rules are: Spinning player will not roll themself, spins are in a mostly-random queue";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbbottle1[/eicon][eicon]dbbottle2[/eicon]";
        }

        public string GetEndingDisplay()
        {
            return "";//Round finished. Thank you for playing Bottle Spin using [user]Dice Bot[/user]!";
        }

        public string GameStatus(GameSession session)
        {
            return GetLastPlayerSpun(session);
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
            return "";
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            //int index = r.Next(playerNames.Count);

            //string selectedPlayer = playerNames[index];

            string selectedPlayer = session.RandomPlayerQueueData.GetNextPlayerSpin(r, executingPlayer);

            int spinTextNumber = r.Next(8);
            string spinText = "";
            switch(spinTextNumber)
            {
                case 0:
                    spinText = "spins around quickly";
                    break;
                case 1:
                    spinText = "spins around and wobbles";
                    break;
                case 2:
                    spinText = "spins around slowly";
                    break;
                case 3:
                    spinText = "bounces awkwardly";
                    break;
                case 4:
                    spinText = "only spends a moment spinning";
                    break;
                case 5:
                    spinText = "spins a long time";
                    break;
                case 6:
                    spinText = "rolls over several times";
                    break;
                case 7:
                    spinText = "quickly turns";
                    break;
                default:
                    spinText = "spins";
                    break;
            }

            int faceTextNumber = r.Next(6);
            string faceText = "";
            switch (faceTextNumber)
            {
                case 0:
                    faceText = "faces";
                    break;
                case 1:
                    faceText = "points close to";
                    break;
                case 2:
                    faceText = "points right at";
                    break;
                case 3:
                    faceText = "seems closest to";
                    break;
                case 4:
                    faceText = "chooses";
                    break;
                case 5:
                    faceText = "stops in front of";
                    break;
                default:
                    faceText = "points at";
                    break;
            }

            string outputString = "[color=yellow]Spinning...[/color]\nThe bottle " + spinText + " and " + faceText + " " + Utils.GetCharacterUserTags(selectedPlayer) + "!";

            session.State = DiceFunctions.GameState.Finished;
            
            return outputString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public string GetLastPlayerSpun(GameSession session)
        {
            return string.IsNullOrEmpty(session.RandomPlayerQueueData.LastPlayerSpun) ? "No players have been spun yet." : "The last player spun was " + Utils.GetCharacterUserTags(session.RandomPlayerQueueData.LastPlayerSpun) + ".";
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            string returnString = "";
            if (terms.Contains("lastspin") || terms.Contains("lastspun"))
                returnString = GetLastPlayerSpun(session);
            else { returnString += "Failed: No such command exists"; }

            return returnString;
        }
    }
}
