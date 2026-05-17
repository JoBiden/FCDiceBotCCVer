using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public interface IGame
    {
        string GetGameName();
        int GetMaxPlayers();
        int GetMinPlayers();
        bool AllowAnte();
        bool UsesFlatAnte();
        bool KeepSessionDefault();
        int GetMinimumMsBetweenGames();

        string GetGameHelp();
        string GetStartingDisplay();
        string GetEndingDisplay();
        void Update(BotMain botMain, GameSession session, double currentTime);

        string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session); //run the game and get output

        string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms);

        string PlayerLeftGame(BotMain botMain, GameSession session, string characterName);

        string GameStatus(GameSession session);

        bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString);
    }
}
