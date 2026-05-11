using FChatDicebot.BotCommands.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    //public class Pokergame : IGame
    //{
    //    public string GetGameName()
    //    {
    //        return "PokerGame";
    //    }

    //    public int GetMaxPlayers()
    //    {
    //        return 8;
    //    }

    //    public int GetMinPlayers()
    //    {
    //        return 3;
    //    }

    //    public bool AllowAnte()
    //    {
    //        return false;
    //    }

    //    public bool UsesFlatAnte()
    //    {
    //        return false;
    //    }

    //    public bool KeepSessionDefault()
    //    {
    //        return true;
    //    }

    //    public int GetMinimumMsBetweenGames()
    //    {
    //        return 0;
    //    }

    //    public string GetStartingDisplay()
    //    {
    //        return "[eicon]dbpoker1[/eicon][eicon]dbpoker2[/eicon][eicon]dbpoker3[/eicon]";
    //    }

    //    public string GetEndingDisplay()
    //    {
    //        return "";
    //    }

    //    public string GameStatus(GameSession session)
    //    {
    //        if(session.PokerGameData != null && !string.IsNullOrEmpty(session.PokerGameData.lastDeclaredTurnOrder))
    //        {
    //            return "Current turn order: " + session.PokerGameData.lastDeclaredTurnOrder;//.turnOrderList);
    //        }
    //        else
    //            return "Turn order not yet assigned.";
    //    }

    //    public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
    //    {
    //        messageString = "";
    //        return true;
    //    }

    //    public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
    //    {
    //        if (session.PokerGameData.turnPlayer == null) { session.PokerGameData.turnPlayer = playerNames[0]; }
    //        this.SortList(playerNames, session);

    //        string outputmessage = "Turn order is: ";

    //        for (int i = 0; i < session.PokerGameData.sortedPlayerList.Count; ++i)
    //        {
    //            string trueDraw = "";
    //            diceBot.DrawCards(2, false, true, session.ChannelId, DeckType.Playing, null, session.PokerGameData.sortedPlayerList[i], true, DeckType.NONE, null, out trueDraw);

    //            string playerMessageOutput = "[i]Hand for poker in " + session.ChannelId + ": 2 cards drawn:[/i] " + trueDraw;
    //            botMain.SendPrivateMessage(playerMessageOutput, session.PokerGameData.sortedPlayerList[i]);

    //            outputmessage += session.PokerGameData.sortedPlayerList[i];
    //            if (i == 0) { outputmessage += " (small)"; };
    //            if (i == 1) { outputmessage += " (big)"; };
    //            if (i != session.PokerGameData.sortedPlayerList.Count - 1) { outputmessage += ", "; }
    //        }

    //        session.PokerGameData.lastDeclaredTurnOrder = outputmessage;

    //        session.PokerGameData.turnPlayer = session.PokerGameData.sortedPlayerList[1];
    //        session.PokerGameData.dealRound = 0;

    //        session.PokerGameData.sortedPlayerList.Add(session.PokerGameData.sortedPlayerList[0]);
    //        session.PokerGameData.sortedPlayerList.RemoveAt(0);

    //        session.PokerGameData.turnOrderList = new List<string>(session.PokerGameData.sortedPlayerList);
    //        session.PokerGameData.turnOrderList.Add(session.PokerGameData.turnOrderList[0]);
    //        session.PokerGameData.turnOrderList.RemoveAt(0);
    //        session.State = GameState.GameInProgress;

    //        return outputmessage;
    //    }

    //    public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
    //    {
    //        if (session.PokerGameData.sortedPlayerList.Contains(characterName))
    //        {
    //            session.PokerGameData.sortedPlayerList.Remove(characterName);
    //        }
    //        if (session.PokerGameData.turnPlayer == characterName)
    //        {
    //            session.PokerGameData.turnPlayer = session.PokerGameData.sortedPlayerList[0];
    //        }
    //        string outputstring = "Goodbye " + characterName;
    //        return outputstring;
    //    }

    //    public void SortList(List<String> playerNames, GameSession session)
    //    {
    //        session.PokerGameData.sortedPlayerList = new List<string>(playerNames);
    //        session.PokerGameData.sortedPlayerList.Sort();
    //        while (true)
    //        {
    //            if (session.PokerGameData.sortedPlayerList[0] != session.PokerGameData.turnPlayer)
    //            {
    //                session.PokerGameData.sortedPlayerList.Insert(0, session.PokerGameData.sortedPlayerList[session.PokerGameData.sortedPlayerList.Count - 1]);
    //                session.PokerGameData.sortedPlayerList.RemoveAt(session.PokerGameData.sortedPlayerList.Count - 1);
    //            }
    //            else { break; }
    //        }
    //    }

    //    public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
    //    {
    //        string returnString = "";

    //        if (terms.Contains("deal"))
    //        {
    //            if (session.State == GameState.GameInProgress)
    //            {
    //                if (session.PokerGameData.dealRound < 3)
    //                {
    //                    if (session.PokerGameData.dealRound == 0) { diceBot.DrawCards(3, false, true, session.ChannelId, DeckType.Playing, null, "dealer", false, DeckType.NONE, null, out returnString); }
    //                    if (session.PokerGameData.dealRound == 1) { diceBot.DrawCards(1, false, true, session.ChannelId, DeckType.Playing, null, "dealer", false, DeckType.NONE, null, out returnString); }
    //                    if (session.PokerGameData.dealRound == 2)
    //                    {
    //                        diceBot.DrawCards(1, false, true, session.ChannelId, DeckType.Playing, null, "dealer", false, DeckType.NONE, null, out returnString);
    //                        session.State = GameState.Unstarted;
    //                    }
    //                    session.PokerGameData.dealRound++;
    //                    returnString += "\nTurn order is: ";
    //                    for (int i = 0; i < session.PokerGameData.turnOrderList.Count; ++i)
    //                    {
    //                        returnString += session.PokerGameData.turnOrderList[i];
    //                        if (i != session.PokerGameData.turnOrderList.Count - 1) { returnString += ", "; }
    //                    }
    //                }
    //                else { returnString += "Round is over already"; }
    //            }
    //        }
    //        else if (terms.Contains("showhands"))
    //        {
    //            if (session.PokerGameData.turnOrderList != null && session.PokerGameData.turnOrderList.Count > 0)
    //            {
    //                returnString += "Everyone's hands are:\n";

    //                for (int i = 0; i < session.PokerGameData.turnOrderList.Count; ++i)
    //                {
    //                    returnString += session.PokerGameData.turnOrderList[i] + "'s hand is: ";
    //                    Hand h = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, session.PokerGameData.turnOrderList[i]);
    //                    returnString += h.ToString();
    //                    if (i != session.PokerGameData.turnOrderList.Count - 1) { returnString += "\n"; }
    //                }
    //            }
    //            else
    //            {
    //                returnString += "There are no hands to show.";
    //            }
    //        }

    //        else if (terms.Contains("endround"))
    //        {
    //            if (session.State == GameState.GameInProgress)
    //            {
    //                returnString += "This round is over";
    //                session.State = GameState.Unstarted;
    //                session.PokerGameData.dealRound = 4;
    //            }
    //        }

    //        else if (terms.Contains("fold"))
    //        {
    //            if (session.PokerGameData.turnOrderList != null && session.PokerGameData.turnOrderList.Count > 0)
    //            {
    //                if(session.PokerGameData.turnOrderList.Contains(character))
    //                {
    //                    session.PokerGameData.turnOrderList.Remove(character);

    //                    int numberDiscards = 0;
    //                    string actionString = diceBot.DiscardCards(null, true, channel, DeckType.Playing, null, character, out numberDiscards);

    //                    returnString += Utils.GetCharacterUserTags(character) + " has folded and discarded " + numberDiscards + " cards.";
    //                }
    //                else
    //                {
    //                    returnString += "Error: player was not active in this hand to fold.";
    //                }
    //            }
    //            else
    //            {
    //                returnString += "The game has not yet started.";
    //            }
    //        }

    //        else { returnString += "No such command exists"; }

    //        return returnString;
    //    }
    //}

    //public class PokerGameData
    //{
    //    public string turnPlayer;
    //    public List<string> sortedPlayerList;
    //    public List<string> turnOrderList;
    //    public int dealRound = 0;

    //    public string lastDeclaredTurnOrder;
    //}
}
