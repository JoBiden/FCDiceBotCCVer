using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using WebSocketSharp;
using FChatDicebot.Model;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;
using System.IO;

namespace FChatDicebot
{
    public class BotMain
    {
        public string CurrentApiKey = "";
        public const string FListChatUri = "wss://chat.f-list.net/chat2";
        public const bool _debug = false;
        public const bool _returnAllRecievedChatMessagesFromChannels = false;
        public const bool _testVersion = false;
        public const string Version = "0.15";

        public const string FileFolder = "C:\\BotData\\DiceBot";
        public const string LogsFolder = "C:\\BotData\\DiceBot\\logs";
        public const string BackupFolder = "C:\\BotData\\DiceBot\\ImmediateBackup";
        public const string TestFilePrefix = "_test_";
        public const string AccountSettingsFileName = "account_settings.txt";
        public const string SavedTablesFileName = "saved_tables.txt";
        public const string SavedSlotsFileName = "saved_slots.txt";
        public const string SavedDecksFileName = "saved_decks.txt";
        public const string SavedPotionsFileName = "saved_potions.txt";
        public const string ChannelSettingsFileName = "channel_settings.txt";
        public const string CharacterDataFileName = "character_data.txt";
        public const string VcChipOrdersFileName = "vc_chiporder_data.txt";
        public const string LogFileName = "outputlog.txt";
        public const string SavedChipsFileName = "saved_chipPiles.txt";
        public const string ChipsCouponsFileName = "coupons_active.txt";
        public const string ChipsCouponsInactiveFileName = "coupons_inactive.txt";
        public const string PotionGenerationFileName = "potionGeneration_data.txt";

        public const string BotTestingStatus = "Chateau Contract is currently undergoing [color=orange]testing[/color]. Please excuse the growing pains!";
        public const string BotOnlineStatus = "[color=purple]Chateau Contract Bot v" + Version + " Online![/color] Check the [user]Chateau Contract[/user] profile for Chateau Exclusive commands. Use '!help' and check out the [user]Dice Bot[/user] profile for dice commands (Many still work!)";

        public const string HandCollectionName = "hand";
        public const string DeckCollectionName = "deck";
        public const string InPlayCollectionName = "cards in play";
        public const string HiddenInPlayCollectionName = "hidden cards in play";
        public const string PileCollectionName = "pile";
        public const string DiscardCollectionName = "discard";
        public const string BurnCollectionName = "burned";

        public const string CasinoChannelId = "adh-3fe0682b9b6bbe0acb62";
        public const string ChessClubChannelId = "adh-eb72b02eb39b6cf9ea84";
        public const string TestDicebotChannelId = "adh-42881ecd2337a801ce32";
        public const string BreakerWorldChannelId = "adh-c3a7c030da9f2bf4fb24";
        public const string KowloonChannelId = "adh-62144c7343711c3b838f";
        public const string SevenMinutesFateRoomId = "adh-0d4edcedd6eb4839f03b";
        public const string VelvetCuffBotName = "VelvetCuff";

        public BotWebRequests WebRequests;
        public VelvetcuffConnection VelvetcuffConnection;
        public BotMessageQueue MessageQueue;
        public List<BotFutureMessage> FutureMessages;
        public BotCommandController BotCommandController;
        public DiceBot DiceBot;
        public AccountSettings AccountSettings;
        public List<SavedRollTable> SavedTables;
        public List<SavedSlotsSetting> SavedSlots;
        public List<SavedDeck> SavedDecks;
        public List<SavedPotion> SavedPotions;
        public List<ChannelSettings> SavedChannelSettings;
        public List<ChipsCoupon> ChipsCoupons;

        public const int StartingChipsInPile = 500;

        public const int MinimumCharactersTableId = 2;
        public const int MaximumCharactersTableName = 30;
        public const int MaximumCharactersTableDescription = 300;
        public const int MaximumCharactersTableEntryDescription = 200;
        public const int MaximumRollTriggersPerEntry = 4;
        public const int MaximumSavedTablesPerCharacter = 4;
        public const int MaximumSavedPotionsPerCharacter = 6;
        public const int MaximumCharactersPotionDescription = 500;
        public const int MaximumCharactersPotionName = 50;
        public const int MaximumSavedEntriesPerTable = 50;
        public const int MaximumCardsInDeck = 200;
        public const int ReconnectTimeMs = 300000; //5 minutes reconnect time
        public const int InitialWaitTime = 5000;
        public const int SlotsSpinCooldownSeconds = 300; //5 minutes
        public const int LuckForecastCooldownSeconds = 3600; //60 minutes

        public const int GreetCharacterCooldownMinutes = 60;

        public const int MaximumCharsInMessage = 11000;

        public TimeSpan Uptime;
        public TimeSpan LastCheckin;
        public DateTime LoginTime;
        public const int TickTimeMiliseconds = 200;
        public int MinimumTimeBetweenMessages = 1500; //minimum 1 second between messages for FList bot rules
        public int ReconnectTimer = 0;
        public TimeSpan CheckinInterval = new TimeSpan(0, 0, 20, 0, 0);
        public List<string> ChannelsJoined = new List<string>();
        public List<UserGeneratedCommand> WaitingChannelOpRequests = new List<UserGeneratedCommand>();
        public List<BuyCommand> WaitingBuyCommands = new List<BuyCommand>();

        public string LastMessageRecieved1 = "";
        public string LastMessageRecieved2 = "";

        public delegate void BotCommandDelegate();

        public void Run()
        {
            BotCommandController = new BotCommandController(this);

            BackupAllData();

            LoadAccountSettings();

            LoadChannelSettings();

            LoadSavedTables();

            LoadSavedSlots();

            LoadChipsCoupons();

            LoadSavedDecks();

            MessageQueue = new BotMessageQueue();
            FutureMessages = new List<BotFutureMessage>();
            WebRequests = new BotWebRequests();
            DiceBot = new DiceFunctions.DiceBot(this);
            VelvetcuffConnection = new FChatDicebot.VelvetcuffConnection(WebRequests, AccountSettings);

            System.Threading.Thread th = new Thread(RunLoop);
            th.IsBackground = true;
            th.Start();
        }

        private void LoadAccountSettings()
        {
            string settingsFile = AccountSettingsFileName;
            
            string path = Utils.GetTotalFileName(FileFolder, settingsFile);
            try
            {
                VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if(_debug)
                        Console.WriteLine("read " + path);

                    AccountSettings = JsonConvert.DeserializeObject<AccountSettings>(fileText);

                    if (_debug)
                        Console.WriteLine("loaded LoadAccountSettings successfully.");
                }
                else
                {
                    Console.WriteLine("LoadAccountSettings file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadAccountSettings for " + path + "\n" + exc.ToString());
            }
        }

        private void LoadChannelSettings()
        {
            string path = Utils.GetTotalFileName(FileFolder, ChannelSettingsFileName);
            try
            {
                VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if (_debug)
                        Console.WriteLine("read " + path);

                    SavedChannelSettings = JsonConvert.DeserializeObject<List<ChannelSettings>>(fileText);

                    if (_debug)
                        Console.WriteLine("loaded LoadChannelSettings successfully.");
                }
                else
                {
                    SavedChannelSettings = new List<ChannelSettings>();
                    Console.WriteLine("LoadChannelSettings file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadChannelSettings for " + path + "\n" + exc.ToString());
            }
        }

        private void LoadSavedTables()
        {
            string path = Utils.GetTotalFileName(FileFolder, SavedTablesFileName);
            try
            {
                VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if (_debug)
                        Console.WriteLine("read " + path);

                    SavedTables = JsonConvert.DeserializeObject<List<SavedRollTable>>(fileText);

                    if (_debug)
                        Console.WriteLine("loaded SavedTables successfully.");
                }
                else
                {
                    Console.WriteLine("LoadSavedTables file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadSavedTables for " + path + "\n" + exc.ToString());
            }
        }

        private void BackupAllData()
        {
            VerifyDirectoryExists(FileFolder);
            VerifyDirectoryExists(LogsFolder);
            VerifyDirectoryExists(BackupFolder);

            Utils.CopyFile(SavedChipsFileName, FileFolder, BackupFolder);
            Utils.CopyFile(CharacterDataFileName, FileFolder, BackupFolder);
            Utils.CopyFile(ChannelSettingsFileName, FileFolder, BackupFolder);
            Utils.CopyFile(ChipsCouponsFileName, FileFolder, BackupFolder);
            Utils.CopyFile(AccountSettingsFileName, FileFolder, BackupFolder);
            Utils.CopyFile(SavedDecksFileName, FileFolder, BackupFolder);
            Utils.CopyFile(SavedSlotsFileName, FileFolder, BackupFolder);
            Utils.CopyFile(SavedTablesFileName, FileFolder, BackupFolder);
        }

        public static void VerifyDirectoryExists(string fileFolder = FileFolder)
        {
            if (!Directory.Exists(FileFolder))
            {
                Directory.CreateDirectory(FileFolder);
            }
        }

        private void LoadSavedSlots()
        {
            string path = Utils.GetTotalFileName(FileFolder, SavedSlotsFileName);
            try
            {
                VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if (_debug)
                        Console.WriteLine("read " + path);

                    SavedSlots = JsonConvert.DeserializeObject<List<SavedSlotsSetting>>(fileText);

                    if (_debug)
                        Console.WriteLine("loaded LoadSavedSlots successfully.");
                }
                else
                {
                    Console.WriteLine("LoadSavedSlots file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadSavedSlots for " + path + "\n" + exc.ToString());
            }
        }

        private void LoadSavedDecks()
        {
            string path = Utils.GetTotalFileName(FileFolder, SavedDecksFileName);
            try
            {
                VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if (_debug)
                        Console.WriteLine("read " + path);

                    SavedDecks = JsonConvert.DeserializeObject<List<SavedDeck>>(fileText);

                    if (_debug)
                        Console.WriteLine("loaded SavedDecks successfully.");

                    if (SavedDecks == null)
                        SavedDecks = new List<SavedDeck>();
                }
                else
                {
                    Console.WriteLine("LoadSavedDecks file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadSavedTables for " + path + "\n" + exc.ToString());
            }
        }

        private void LoadChipsCoupons()
        {
            ChipsCoupons = new List<ChipsCoupon>();

            string path = Utils.GetTotalFileName(FileFolder, ChipsCouponsFileName);
            try
            {
                VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if (_debug)
                        Console.WriteLine("read " + path);

                    ChipsCoupons =  JsonConvert.DeserializeObject<List<ChipsCoupon>>(fileText);

                    if (_debug)
                        Console.WriteLine("loaded ChipsCoupons successfully.");
                }
                else
                {
                    Console.WriteLine("ChipsCoupons file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load ChipsCoupons for " + path + "\n" + exc.ToString());
            }
        }

        public int PinMessageSent = 0;
        public void RunLoop()
        {
            if(_debug)
                Console.WriteLine("Runloop started");

            Uptime = new TimeSpan();
            LastCheckin = new TimeSpan();

            int LastMessageSent = 0;

            using (var ws = new WebSocket(FListChatUri))
            {
                BotMessage firstIdnRequest = GetIdentificationTicket();

                //ws.OnOpen += (sender, e) =>
                //ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None;

                ws.OnMessage += (sender, e) =>
                    OnMessage(e.Data);

                ws.OnError += (sender, e) =>
                {
                    Console.WriteLine("WebSocket Error ({0}) ({1})", e.Message, e.Exception);

                    Utils.AddToLog(string.Format("WebSocket Error ({0}) ({1})", e.Message, e.Exception), e);
                };

                ws.OnClose += (sender, e) =>
                {
                    Console.WriteLine("WebSocket Close ({0}) ({1})", e.Code, e.Reason);
                    Utils.AddToLog(string.Format("WebSocket Close (code: {0}) (reason: {1}) (sender: {2}) (Last 2 Messages: {3})", e.Code, e.Reason, sender.ToString(), LastMessageRecieved2 + ", " + LastMessageRecieved1), e);

                    var sslProtocolHack = (System.Security.Authentication.SslProtocols)(SslProtocolsHack.Tls12 | SslProtocolsHack.Tls11 | SslProtocolsHack.Tls);
                    //TlsHandshakeFailure
                    if (e.Code == 1015 && ws.SslConfiguration.EnabledSslProtocols != sslProtocolHack)
                    {
                        Console.WriteLine("activating ssl protocol change");

                        ws.SslConfiguration.EnabledSslProtocols = sslProtocolHack;
                        Thread.Sleep(1000);
                        ws.Connect();
                    }
                };


                ws.Origin = "http://localhost:4649";

                ws.EnableRedirection = true;

                PerformStartupClientCommands(ws, firstIdnRequest, false);

                int totalTicks = 0;

                while(true)
                {
                    if (ws.IsAlive)
                    {
                        try
                        {
                            if(totalTicks % 20 == 0) //every 4 seconds
                            {
                                VelvetcuffConnection.CheckVelvetcuffOrders(this);
                            }
                        }catch(Exception exc)
                        {
                            Console.WriteLine("exception velvetcuff check orders " + exc.ToString());
                        }
                        try
                        {
                            LastMessageSent += TickTimeMiliseconds;
                            totalTicks++;

                            if (MessageQueue.HasMessages())
                            {
                                BotMessage nextMessagePeek = MessageQueue.Peek();

                                if (nextMessagePeek != null && !Utils.BotMessageIsChatMessage(nextMessagePeek) || LastMessageSent >= MinimumTimeBetweenMessages)
                                {
                                    BotMessage nextMessageToSend = MessageQueue.GetNextMessage();

                                    if (nextMessageToSend != null)
                                    {
                                        if (nextMessageToSend.messageType == "PIN")
                                        {
                                            PinMessageSent++;
                                            if (PinMessageSent % 10 == 0)
                                            {
                                                Console.WriteLine("sending: " + nextMessageToSend.PrintedCommand() + " (#" + PinMessageSent + ")");
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("sending: " + nextMessageToSend.PrintedCommand());
                                            Utils.AddToLog("sending: " + nextMessageToSend.PrintedCommand(), nextMessageToSend);
                                        }

                                        ws.Send(nextMessageToSend.PrintedCommand());
                                    }
                                    if (Utils.BotMessageIsChatMessage(nextMessageToSend))
                                    {
                                        LastMessageSent = 0;
                                    }
                                }
                            }
                            if (WaitingChannelOpRequests.Count > 0)
                            {
                                if (_testVersion)
                                    Console.WriteLine("Channeloprequest found in queue");

                                List<UserGeneratedCommand> finished = new List<UserGeneratedCommand>();
                                foreach (UserGeneratedCommand req in WaitingChannelOpRequests)
                                {
                                    if (req.ops != null)
                                    {
                                        BotCommandController.RunChatBotCommand(req);
                                        finished.Add(req);
                                    }
                                }
                                if (finished.Count > 0)
                                {
                                    foreach (UserGeneratedCommand rFin in finished)
                                    {
                                        WaitingChannelOpRequests.Remove(rFin);
                                    }
                                }
                            }
                            HandleWaitingBuyCommands();

                            Uptime.Add(new TimeSpan(0, 0, 0, 0, TickTimeMiliseconds));

                            PerformCheckinIfNecessary(ref Uptime, ref LastCheckin);

                        }catch(Exception exc)
                        {
                            Console.WriteLine("Exception inside ws.IsAlive: " + exc.ToString());
                            Utils.AddToLog("Exception inside ws.IsAlive: " + exc.ToString(), exc.StackTrace);
                        }
                    }
                    else
                    {
                        if(ReconnectTimer == 0)
                        {
                            Console.WriteLine("Connection was lost.");
                            Console.WriteLine("Waiting for " + (ReconnectTimeMs / 1000) + " seconds to reconnect.");
                            Utils.AddToLog("Waiting for " + (ReconnectTimeMs / 1000) + " seconds to reconnect.", null);
                        }

                        ReconnectTimer += TickTimeMiliseconds;

                        if (ReconnectTimer > ReconnectTimeMs)
                        {
                            BotMessage reconnectIdnRequest = GetIdentificationTicket();
                            PerformStartupClientCommands(ws, reconnectIdnRequest, true);
                            ReconnectTimer = 0;
                        }
                    }
                    HandleFutureMessagesTick(TickTimeMiliseconds);

                    Thread.Sleep(TickTimeMiliseconds);
                }
            }
        }

        private BotMessage GetIdentificationTicket()
        {
            if(GetNewApiTicket())
            {
                return GetNewIDNRequest();
            }
            else
            {
                return null;
            }
        }

        private void HandleFutureMessagesTick(int tickMs)
        {
            if (FutureMessages.Count > 0)
            {
                foreach (BotFutureMessage mes in FutureMessages)
                {
                    if (mes.MsWait > 0)
                        mes.MsWait -= tickMs;
                    else if (!mes.Sent)
                    {
                        if (mes.ChannelMessage)
                            SendMessageInChannel(mes.MessageContent, mes.Channel);
                        else
                            SendPrivateMessage(mes.MessageContent, mes.Character);

                        mes.Sent = true;
                    }
                }

                FutureMessages.RemoveAll(b => b.Sent);
            }
        }

        private void PerformStartupClientCommands(WebSocket sock, BotMessage firstIdnRequest, bool reconnect)
        {
            if (firstIdnRequest == null)
                return;

            Console.WriteLine("connecting...");
            sock.Connect();

            if (!sock.IsAlive)
                return;

            sock.Send(firstIdnRequest.PrintedCommand());

            LoginTime = DateTime.UtcNow;

            //required to wait for the server to stop sending startup messages, otherwise overflow occurs and ws closes
            Console.WriteLine("waiting for " + InitialWaitTime + "ms...");
            Thread.Sleep(InitialWaitTime);

            JoinStartingChannels();

            Thread.Sleep(1000);

            string botStatus = BotOnlineStatus;
            if (_testVersion)
                botStatus = BotTestingStatus;

            SetStatus(STAStatus.Online, botStatus);
        }

        private void JoinStartingChannels()
        {
            ChannelsJoined = new List<string>();

            if (SavedChannelSettings.Count > 0)
            {
                foreach (ChannelSettings channel in SavedChannelSettings)
                {
                    if(channel.StartupChannel)
                        JoinChannel(channel.Name);
                }
            }
        }

        
        private bool GetNewApiTicket()
        {
            CurrentApiKey = null;
            WebRequests.LoginToServerPublic(AccountSettings);

            int timeout = 100000;
            while (WebRequests.ApiTicketResult == null)
            {
                timeout -= TickTimeMiliseconds;
                Thread.Sleep(TickTimeMiliseconds);
                if (TickTimeMiliseconds <= 0)
                {
                    Console.WriteLine("Timeout on acquiring new API ticket");
                    return false;
                }
            }

            Console.WriteLine("Ticket acquired: " + WebRequests.ApiTicketResult.ticket);
            CurrentApiKey = WebRequests.ApiTicketResult.ticket;
            return true;
        }

        private BotMessage GetNewIDNRequest()
        {
            var idn = new IDNclient()
            {
                account = AccountSettings.AccountName,
                ticket = CurrentApiKey,
                character = AccountSettings.CharacterName,
                cname = AccountSettings.CName,
                cversion = Version,
                method = "ticket"
            };
            Utils.AddToLog("idn request created", idn);
            Console.WriteLine("idn request created: " + JsonConvert.SerializeObject(idn));

            return new BotMessage() { MessageDataFormat = idn, messageType = "IDN" };
        }

        private void PerformCheckinIfNecessary(ref TimeSpan uptime, ref TimeSpan lastCheckin)
        {
            if (Uptime - LastCheckin > CheckinInterval)
            {
                bool newTicket = GetNewApiTicket();
                Utils.AddToLog("performing checkin, newticket? " + newTicket, AccountSettings);
                Console.WriteLine("performing checkin, newticket? " + newTicket);
                if(newTicket)
                {
                    MessageQueue.AddMessage(GetNewIDNRequest());
                }
                LastCheckin += (Uptime - LastCheckin);
            }
        }

        private void HandleWaitingBuyCommands()
        {
            if (WaitingBuyCommands.Count > 0)
            {
                bool removeOne = false;
                foreach (BuyCommand command in WaitingBuyCommands)
                {
                    if (PinMessageSent > command.PingCount + BuyCommand.PingCountAmount)
                    {
                        command.Remove = true;
                        removeOne = true;
                        Console.WriteLine("removing waitingbuycommand for pings timeout");
                    }
                    if (command.Confirmed)
                    {
                        //add chips in casino
                        int amount = command.GetChipsAmount();
                        string chipsAdded = DiceBot.AddChips(command.CharacterName, CasinoChannelId, amount, false);
                        //send confirm message to casino
                        SendMessageInChannel("Chips recorded in the VC Casino Channel: " + chipsAdded, command.ChannelName);
                        //remove from waiting commands
                        command.Remove = true;
                        removeOne = true;
                        Console.WriteLine("confirmed waitingbuycommand and added chips (" + amount + ")");
                    }
                }

                if (removeOne)
                    WaitingBuyCommands = WaitingBuyCommands.Where(a => !a.Remove).ToList();
            }
        }

        private void StackLastMessage(string messageData)
        {
            LastMessageRecieved2 = LastMessageRecieved1;
            LastMessageRecieved1 = messageData;
        }

        public void OnMessage(string data)
        {
            try
            {
                StackLastMessage(data);
                string[] pieces = data.Split(' ');
                if (pieces == null || pieces.Length < 1)
                {
                    Console.WriteLine("Error OnMessage, data did not parse correctly.");
                    return;
                }
                string messageType = pieces[0];

                string trimmedChatCommand = data.Substring(3).Trim();

                switch (messageType)
                {
                    case "NLN": //a user connected
                    case "STA": //status change for character
                    case "FLN": //a user disconnected
                    case "LRP": //a (friend?) user is setting status to looking for RP
                        break;
                    case "RLL": //a user generated /roll using FChat's roll
                        break;
                    case "ICH": //initial channel users
                    case "LIS": //all online characters, gender, status
                        break;
                    case "COL": //sent in response to join channel
                        COLserver colinfo = JsonConvert.DeserializeObject<COLserver>(trimmedChatCommand);

                        Console.WriteLine("Recieved: " + (data.Length > 300 ? data.Substring(0, 300) : data));
                        UserGeneratedCommand req = WaitingChannelOpRequests.FirstOrDefault(b => b.channel == colinfo.channel);

                        if (req != null)
                        {
                            Console.WriteLine("channelops returned COL, channel ops returned");
                            req.ops = colinfo.oplist;
                        }
                        else
                        {
                            ChannelsJoined.Add(colinfo.channel);
                        }
                        break;
                    case "JCH": //someone joined a channel the bot is in
                        JCHserver jchInfo = JsonConvert.DeserializeObject<JCHserver>(trimmedChatCommand);

                        ChannelSettings s = GetChannelSettings(jchInfo.channel);

                        if (s.GreetNewUsers)
                        {
                            CountdownTimer timer = DiceBot.GetCountdownTimer(jchInfo.channel, jchInfo.character.identity);
                            if(timer == null || timer.TimerFinished())
                            {
                                DiceBot.StartCountdownTimer(jchInfo.channel, jchInfo.character.identity, jchInfo.character.identity, GreetCharacterCooldownMinutes * 60 * 1000);//cd on greeting each character
                                timer = DiceBot.GetCountdownTimer(jchInfo.channel, jchInfo.character.identity);
                                string timerReport = "";
                                SendMessageInChannel("Welcome to the channel, " + Utils.GetCharacterUserTags(jchInfo.character.identity) + "!" + timerReport, jchInfo.channel);
                            }
                            else
                            {
                                if(_debug)
                                    SendMessageInChannel("Welcome to the channel, " + Utils.GetCharacterUserTags(jchInfo.character.identity) + "! " + timer.GetSecondsRemaining() + " seconds on timer.", jchInfo.channel);
                            }
                        }
                        break;
                    case "LCH"://someone left a channel the bot is in
                        break;
                    case "PIN": //ping from server. reply with ping
                        MessageQueue.AddMessage(new BotMessage() { MessageDataFormat = null, messageType = "PIN" });
                        break;
                    case "PRI": //private message from a user
                        PRICmd msgContentPri = JsonConvert.DeserializeObject<PRICmd>(trimmedChatCommand);
                        InterpretChatCommand(new MSGserver() { character = msgContentPri.character, message = msgContentPri.message, channel = null });
                        break;
                    case "MSG": //message sent in channel
                        MSGserver msgContent = JsonConvert.DeserializeObject<MSGserver>(trimmedChatCommand);
                        InterpretChatCommand(msgContent);
                        break;
                    default:
                        Console.WriteLine("Recieved: " + (data.Length > 300 ? data.Substring(0, 300) : data));
                        break;
                }
            }catch(Exception exc)
            {
                Console.WriteLine("Exception inside OnMessage: " + exc.ToString());
                Utils.AddToLog("Exception inside OnMessage: " + exc.ToString(), exc.StackTrace);
            }
        }

        public void InterpretChatCommand(MSGserver messageContent)
        {
            string prefixChar = "!";
            if(!string.IsNullOrEmpty(messageContent.channel))
            {
                ChannelSettings set = GetChannelSettings(messageContent.channel);
                prefixChar = set.CommandPrefix.ToString();
            }

            //bot commands in chat all start with '!'
            if (messageContent.message.StartsWith(prefixChar) && messageContent.message.Length > 1)
            {
                string commandName = "";
                string[] commandTerms = SeparateCommandTerms(messageContent.message, out commandName);

                BotCommandController.RunChatBotCommand(new UserGeneratedCommand(){
                    rawTerms = commandTerms, 
                    channel = messageContent.channel,
                    characterName = messageContent.character,
                    commandName = commandName,
                    ops = null,
                    terms = null
                });
            }

            if (WaitingBuyCommands.Count > 0 && messageContent.character == VelvetCuffBotName)
            {
                string z = messageContent.message;
                if(z.Contains("Chips") && z.Contains("has been purchased for"))
                {
                    string starting = z.Substring(0, z.IndexOf("has been purchased") - 1).Trim();
                    string findNumber = starting.ToLower().Replace("chips", "").Trim();
                    int numberOut = 0;
                    int.TryParse(findNumber, out numberOut);

                    BuyCommand foundCommand = WaitingBuyCommands.FirstOrDefault(a => a.GetChipsAmount() == numberOut);
                    foundCommand.Confirmed = true;

                    Console.WriteLine("confirmed waitingbuycommand");
                }
            }

            if (_returnAllRecievedChatMessagesFromChannels && !string.IsNullOrEmpty(messageContent.channel))
            {
                MessageQueue.AddMessage(new BotMessage()
                {
                    messageType = BotMessageFactory.MSG,
                    MessageDataFormat = new MSGclient()
                    {
                        channel = messageContent.channel,
                        message = messageContent.channel + " " + messageContent.character + " " + messageContent.message
                    }
                });
            }
        }

        public string[] SeparateCommandTerms(string rawCommandMessage, out string commandName)
        {
            string[] splitSpace = rawCommandMessage.Split(' ');
            commandName = splitSpace[0].Substring(1).ToLower();
            string[] commandTerms = splitSpace.Skip(1).ToArray();
            return commandTerms;
        }

        public void SendMessageInChannel(string message, string channel)
        {
            MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.MSG, new MSGclient() { channel = channel, message = message }));
        }

        public void SendFutureMessage(string message, string channel, string recipient, bool channelMessage, int waitMs)
        {
            FutureMessages.Add(new BotFutureMessage()
            {
                Channel = channel,
                Character = recipient,
                MessageContent = message,
                ChannelMessage = channelMessage,
                MsWait = waitMs
            });
        }

        public void SendPrivateMessage(string message, string recipient)
        {
            MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.PRI, new PRIclient() { recipient = recipient, message = message }));
        }

        public void JoinChannel(string channel)
        {
            MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.JCH, new JCHclient() { channel = channel }));
        }

        public void LeaveChannel(string channel)
        {
            MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.LCH, new LCHclient() { channel = channel }));
        }

        public void SetStatus(string status, string message)
        {
            MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.STA, new STAclient() { status = status, statusmsg = message }));
        }

        public void RequestChannelOpListAndQueueFurtherRequest(UserGeneratedCommand command)
        {
            WaitingChannelOpRequests.Add(command);
            MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.COL, new COLclient() { channel = command.channel } ));
        }

        public ChannelSettings GetChannelSettings(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return null;

            ChannelSettings set =  SavedChannelSettings.FirstOrDefault(a => a.Name.ToLower() == channel.ToLower());

            if(set == null)
            {
                set = new ChannelSettings()
                {
                    Name = channel,
                    AllowCustomTableRolls = true,
                    AllowTableRolls = true,
                    AllowTableInfo = true,
                    AllowChips = true,
                    AllowGames = true,
                    AllowSlots = true,
                    SlotsMultiplierLimit = 1000,
                    StartingChips = 0,
                    CommandPrefix = '!',
                    ChipsClearance = ChipsClearanceLevel.NONE
                };

                SavedChannelSettings.Add(set);
                BotCommandController.SaveChannelSettingsToDisk();
            }

            return set;
        }
    }

    public enum SslProtocolsHack
    {
        Tls = 192,
        Tls11 = 768,
        Tls12 = 3072
    }
}
