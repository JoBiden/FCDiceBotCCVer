﻿using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

//This class is a dumping ground for old tests used to make parts of the connection work. Safe to ignore it really.
namespace FChatDicebot
{
    class Tests
    {
        public void teststuff()
        {
            //10/11/2020 I'm not sure what this is 
            using (var ws = new WebSocket("ws://dragonsnest.far/Laputa"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("Laputa says: " + e.Data);

                ws.OnOpen += (sender, e) =>
                {
                    Console.WriteLine("triggered onopen");
                    ws.Send("BALUS");// ("Hi, there dragonsnest.far!");
                };
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);

                ws.Connect();
                Thread.Sleep(2000);
                ws.Send("BALUS");
                Console.ReadKey(true);
            }
        }

        public void teststuff2()
        {
            using (var ws = new WebSocket("ws://echo.websocket.org"))
            //using (var ws = new WebSocket ("wss://echo.websocket.org"))
            //using (var ws = new WebSocket ("ws://localhost:4649/Echo"))
            //using (var ws = new WebSocket ("wss://localhost:5963/Echo"))
            //using (var ws = new WebSocket ("ws://localhost:4649/Echo?name=nobita"))
            //using (var ws = new WebSocket ("wss://localhost:5963/Echo?name=nobita"))
            //using (var ws = new WebSocket ("ws://localhost:4649/Chat"))
            //using (var ws = new WebSocket ("wss://localhost:5963/Chat"))
            //using (var ws = new WebSocket ("ws://localhost:4649/Chat?name=nobita"))
            //using (var ws = new WebSocket ("wss://localhost:5963/Chat?name=nobita"))
            {
                // Set the WebSocket events.

                ws.OnOpen += (sender, e) => ws.Send("Hi, there!");

                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping" + e.Data);

                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);

                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                // To send the Origin header.
                //ws.Origin = "http://localhost:4649";

                // To send the cookies.
                //ws.SetCookie (new Cookie ("name", "nobita"));
                //ws.SetCookie (new Cookie ("roles", "\"idiot, gunfighter\""));

                // To connect through the HTTP Proxy server.
                //ws.SetProxy ("http://localhost:3128", "nobita", "password");

                // To enable the redirection.
                //ws.EnableRedirection = true;

                //ws.Protocol = 

                // Connect to the server.
                ws.Connect();

                // Connect to the server asynchronously.
                //ws.ConnectAsync ();

                Console.WriteLine("\nType 'exit' to exit.\n");

                Thread.Sleep(1000);
                ws.Send("Hi, there2!");
                Thread.Sleep(1000);
                ws.Send("Hi, there3!");
                Thread.Sleep(1000);
                //while (true)
                //{
                //    Thread.Sleep(1000);
                //    Console.Write("> ");
                //    var msg = Console.ReadLine();
                //    if (msg == "exit")
                //        break;

                //    // Send a text message.
                //    ws.Send(msg);
                //}
            }
        }

        public void testconns3()
        {

            using (var ws = new WebSocket("ws://chat.f-list.net:8722"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }
            using (var ws = new WebSocket("ws://chat.f-list.net:8799"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);

                // Connect to the server.
                Console.WriteLine("\nattempting connection 2.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }

            using (var ws = new WebSocket("wss://www.chat.f-list.net"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1B.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }
            using (var ws = new WebSocket("ws://www.chat.f-list.net"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1B2.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }
            using (var ws = new WebSocket("wss://www.chat.f-list.net:8722"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1B3.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }
            using (var ws = new WebSocket("ws://www.chat.f-list.net:8722"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1B4.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }
            using (var ws = new WebSocket("wss://chat.f-list.net:8799"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1C.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }
            using (var ws = new WebSocket("wss://f-list.net"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1D.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }
            using (var ws = new WebSocket("ws://chat.f-list.net:9799"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);

                // Connect to the server.
                ws.Connect();

                Console.WriteLine("\nattempting connection 3.\n");
                Thread.Sleep(2000);
            }
            using (var ws = new WebSocket("ws://chat.f-list.net:9722"))
            {
                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);

                // Connect to the server.
                ws.Connect();

                Console.WriteLine("\nattempting connection 4.\n");
                Thread.Sleep(2000);
            }
        }

        public void testconns4()
        {
            using (var ws = new WebSocket("ws://chat.f-list.net:8722"))
            {
                ws.OnOpen += (sender, e) =>
                {
                    Console.WriteLine("triggered onopen");
                    ws.Send("Hi, there!");
                };
                //ws.Credentials = 

                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1.\n");
                ws.Connect();

                Thread.Sleep(2000);
            }
        }

        public void testconns5echoWebsocket()
        {
            using (var ws = new WebSocket("ws://echo.websocket.org/"))
            {
                ws.OnOpen += (sender, e) =>
                {
                    Console.WriteLine("triggered onopen");
                    ws.Send("Hi, there! this message was sent to echo websocket.");
                };
                //ws.Credentials = 

                ws.OnMessage += (sender, e) =>
                    Console.WriteLine("websocket message: Recieved a ping :" + e.Data);
                ws.OnError += (sender, e) =>
                    Console.WriteLine("websocket Error: " + e.Message);
                ws.OnClose += (sender, e) =>
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                //ws.Credentials = 
                //ws.IsSecure = false;

                // Connect to the server.
                Console.WriteLine("\nattempting connection 1.\n");
                ws.Connect();
                Console.WriteLine("\nconnection finished 1.\n");

                Thread.Sleep(2000);
            }
        }

        //public void testconns6chat2()
        //{
        //    using (var ws = new WebSocket("wss://chat.f-list.net/chat2"))
        //    {
        //        //IDNSocket identifyRequest = GetNewIDNRequest();

        //        //Thread.Sleep(2000);

        //        //string idString = JsonConvert.SerializeObject(identifyRequest);// identifyRequest string.Format("IDN {\"method\":\"ticket\",\"account\":{0},\"ticket\");
        //        string idRequestString = GetNewIDNRequest().PrintedCommand(); //"IDN " + idString;
        //        //ws.Send("IDN " + idString);

        //        ws.OnOpen += (sender, e) =>
        //        {
        //            Console.WriteLine("triggered onopen");
        //            //bad id test
        //            //ws.Send("IDN {\"method\":\"ticket\", \"account\":\"AccountName\",\"ticket\":\"4\",\"character\":\"Dice Bot\", \"cname\":\"Dice Bot\", \"cversion\":\"0.1\"}");
        //            ws.Send(idRequestString);
        //        };
        //        //ws.Credentials = 

        //        ws.OnMessage += (sender, e) =>
        //            OnMessage(e.Data);// Console.WriteLine("websocket message: Recieved a ping /" + e.Data);
        //        ws.OnError += (sender, e) =>
        //            Console.WriteLine("websocket Error: " + e.Message);
        //        ws.OnClose += (sender, e) =>
        //            Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
        //        //ws.Credentials = 
        //        //ws.IsSecure = false;

        //        // Connect to the server.
        //        Console.WriteLine("\nattempting connection test6.\n");
        //        ws.Connect();

        //        Console.WriteLine("\finished connection test6.\n");

        //        Thread.Sleep(2000);
        //        Thread.Sleep(2000);

        //        JCHSocket jch = new JCHSocket() { channel = "adh-fb744504ecce0909b18b" };
        //        string joinchanneltest = "JCH " + JsonConvert.SerializeObject(jch);
        //        ws.Send(joinchanneltest);

        //        Thread.Sleep(2000);
        //        Thread.Sleep(2000);

        //        MSGSocket msg = new MSGSocket() { channel = "adh-fb744504ecce0909b18b", message = "hello text message" };
        //        string msgtext = "MSG " + JsonConvert.SerializeObject(msg);
        //        ws.Send(msgtext);
        //        Thread.Sleep(2000);
        //        Thread.Sleep(2000);
        //    }
        //}

        private void SaveStartingChannels()
        {
            List<StartingChannel> chans = new List<StartingChannel>();
            chans.Add(new StartingChannel()
            {
                CharacterInvitedName = "Darkness Syndra",
                Essential = true,
                Name = "DiceBotTests",
                Code = "adh-203298ce913d0544dd44"
            });
            chans.Add(new StartingChannel()
            {
                CharacterInvitedName = "Elise Pariat",
                Essential = false,
                Name = "VelvetCuff - Casino",
                Code = "adh-3fe0682b9b6bbe0acb62"
            });
            chans.Add(new StartingChannel()
            {
                CharacterInvitedName = "Darkness Syndra",
                Essential = false,
                Name = "TestChannel55",
                Code = "adh-fb744504ecce0909b18b"
            });

            Utils.WriteToFileAsData(chans, Utils.GetTotalFileName("filefolder", "filename"));
        }

        private void savenewtables()
        {
            RollTable table = new RollTable()
            {
                DieSides = 8,
                RollBonus = 0,
                Name = "Cheeses",
                Description = "Test table of different cheese types.",
                TableEntries = new List<TableEntry>() {
                    new TableEntry() { 
                        Description = "Some classic cheese",
                        Name = "Cheddar",
                        Roll = 1
                    },
                    new TableEntry() { 
                        Description = "blue lines all over this cheese...",
                        Name = "Stilton",
                        Roll = 2
                    },
                    new TableEntry() { 
                        Description = "just as plastic as it sounds",
                        Name = "American",
                        Roll = 3
                    },
                    new TableEntry() { 
                        Description = "white and creamy",
                        Name = "Monzarella",
                        Roll = 4
                    },
                    new TableEntry() { 
                        Description = "bad on anything but spaghetti",
                        Name = "Parmezan",
                        Roll = 5
                    },
                    new TableEntry() { 
                        Description = "that cheese you wish you found out about earlier in life",
                        Name = "Provalone",
                        Roll = 6
                    },
                    new TableEntry() { 
                        Description = "I'm running out of cheese comments",
                        Name = "Feta",
                        Roll = 7
                    },
                    new TableEntry() { 
                        Description = "it comes in a tub",
                        Name = "Cottage",
                        Roll = 8
                    },
                }
            };

            SavedRollTable saved1 = new SavedRollTable()
            {
                DefaultTable = true,
                OriginCharacter = "Darkness Syndra",
                Table = table,
                TableId = "Cheeses"
            };

            //SavedTables = new List<SavedRollTable>();
            //SavedTables.Add(saved1);
            //Utils.WriteToFileAsData(SavedTables, Utils.GetTotalFileName("filefolder", "filename"));
        }

    }
}


//notes:

//this did not work because T needs to be known at compile time, not runtime and 'thisType' was not known until runtime
//AddToBotCommands<thisType>(thisType);

//private void AddToBotCommands<T>(Type originalType) where T : ChatBotCommand  //T original)
//{
//    T newObject = (T)Activator.CreateInstance(originalType);

//    BotCommands.Add(newObject);

//    //return newObject;
//}



///////////////games graveyard
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace FChatDicebot.DiceFunctions
//{
//    //NOTE: this game was never finished. The intent is to assign a referee like the non-automated version of kings' game to watch over player numbers
//    public class KingsGameREFEREE : IGame
//    {
//        public string GetGameName()
//        {
//            return "KingsGameREFEREE";
//        }

//        public int GetMaxPlayers()
//        {
//            return 20;
//        }

//        public int GetMinPlayers()
//        {
//            return 1;
//        }

//        public bool AllowAnte()
//        {
//            return false;
//        }

//        public bool UsesFlatAnte()
//        {
//            return false;
//        }

//        public bool KeepSessionDefault()
//        {
//            return true;
//        }

//        public string GetStartingDisplay()
//        {
//            return "Kings Game with Referee";
//        }

//        public string GetEndingDisplay()
//        {
//            return "";
//        }

//        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, string[] terms, out string messageString)
//        {
//            messageString = "";
//            return true;
//        }

//        public string RunGame(System.Random r, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
//        {
//            //TESTING
//            playerNames.Add("testplayer1");
//            playerNames.Add("testplayer2");
//            playerNames.Add("testplayer3");
//            playerNames.Add("testplayer4");
//            playerNames.Add("testplayer5");

//            int index = r.Next(playerNames.Count);

//            string selectedKing = playerNames[index];

//            int index2 = r.Next(playerNames.Count - 1);

//            if (index2 >= index)
//                index2++;

//            string selectedReferee = playerNames[index2];

//            int kingTextNumber = r.Next(7);
//            string spinText = "";
//            switch(kingTextNumber)
//            {
//                case 0:
//                    spinText = "All hail king ";
//                    break;
//                case 1:
//                    spinText = "The new king is ";
//                    break;
//                case 2:
//                    spinText = "Your king is ";
//                    break;
//                case 3:
//                    spinText = "Behold king ";
//                    break;
//                case 4:
//                    spinText = "A new king has been chosen, ";
//                    break;
//                case 5:
//                    spinText = "This round's king is ";
//                    break;
//                case 6:
//                    spinText = "The king is ";
//                    break;
//                default:
//                    spinText = "The new king is ";
//                    break;
//            }

//            int[] assignedNumbers = new int[playerNames.Count - 2];

//            int createdNumbers = 0;

//            List<int> includedInts = new List<int>();
//            //assign a number to every remaining player
//            for (int i = 0; i < assignedNumbers.Length; i++)
//            {
//                int assignedNumber = r.Next(assignedNumbers.Length - createdNumbers) + 1;

//                int counter = 0;
//                for (int q = 0; q < assignedNumber; q++ )
//                {
//                    counter += 1;
//                    while (includedInts.Contains(counter))
//                        counter += 1;
//                }

//                includedInts.Add(counter);
//                assignedNumbers[i] = assignedNumber;
//                createdNumbers++;
//            }

//            int createdNumberIndex = 0;
//            int playerNumber = 0;

//            string refereeReportString = "";
//            foreach (string player in playerNames)
//            {
//                if(!string.IsNullOrEmpty(refereeReportString))
//                {
//                    refereeReportString += "\n";
//                }
//                if (playerNumber != index && playerNumber != index2)
//                {
//                    int playerAssignedNumber = assignedNumbers[createdNumberIndex];
//                    string messageToPlayer = "Your number is " + playerAssignedNumber + " for this round.";
//                    createdNumberIndex++;
//                    refereeReportString += Utils.GetCharacterUserTags(player) + " is #" + playerAssignedNumber;

//                    //send the message
//                    botMain.SendPrivateMessage(messageToPlayer, player);
//                }
//                else
//                {
//                    if(playerNumber == index)
//                        refereeReportString += Utils.GetCharacterUserTags(player) + " is the [b]king[/b]";
//                    else
//                        refereeReportString += Utils.GetCharacterUserTags(player) + " is the [b]referee[/b].";
//                }
//                playerNumber++;
//            }

//            botMain.SendPrivateMessage(refereeReportString, selectedReferee);
//            botMain.SendPrivateMessage("You are the king this round!", selectedKing);

//            string outputString = "[color=yellow]Rolling...[/color]\n" + kingTextNumber + Utils.GetCharacterUserTags(selectedKing) +
//                "!\nThe referee is " + Utils.GetCharacterUserTags(selectedReferee) + ".\nEveryone has been assigned their numbers and the king may make decrees using the numbers 1 - " + assignedNumbers.Length + ".";

//            session.State = DiceFunctions.GameState.Finished;
            
//            return outputString;
//        }

//        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms)
//        {
//            return GetGameName() + " has no valid GameCommands";
//        }
//    }
//}
