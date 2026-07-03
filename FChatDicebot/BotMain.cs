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
using Discord.Net;
using Discord.WebSocket;
using Discord;

namespace FChatDicebot
{
    public class BotMain
    {
        public string CurrentApiKey = "";
        public const string FListChatUri = "wss://chat.f-list.net/chat2";
#if DEBUG
        public const bool _debug = true;
        public const bool _testVersion = true;
#else
    public const bool _debug = false;
    public const bool _testVersion = false;
#endif
        public const bool _returnAllRecievedChatMessagesFromChannels = false;
        public const string Version = "1.62b";
        public RunMode RunMode = RunMode.FlistPlusDiscord;// RunMode.FlistPlusDiscord; //RunMode.FlistPlusDiscord;// RunMode.FListOnly;// RunMode.DiscordOnly;

        public const ChatRoom ChatRoom = FChatDicebot.ChatRoom.FChat; //currently little effect

        public const string FileFolder = "C:\\BotData\\DiceBot";
        public const string LogsFolder = "C:\\BotData\\DiceBot\\logs";
        public const string BackupFolder = "C:\\BotData\\DiceBot\\ImmediateBackup";
        public const string TestFilePrefix = "_test_";
        public const string AccountSettingsFileName = "account_settings.txt";
        public const string SavedTablesFileName = "saved_tables.txt";
        public const string SavedSlotsFileName = "saved_slots.txt";
        public const string SavedDecksFileName = "saved_decks.txt";
        public const string SavedPotionsFileName = "saved_potions.txt";
        public const string SavedJobsListFileName = "saved_jobslists.txt";
        public const string ChannelSettingsFileName = "channel_settings.txt";
        public const string CharacterDataFileName = "character_data.txt";
        public const string VcChipOrdersFileName = "vc_chiporder_data.txt";
        public const string LogFileName = "outputlog.txt";
        public const string ChannelAuditLogFileName = "channelauditlog.txt";
        public const string SavedChipsFileName = "saved_chipPiles.txt";
        public const string ChipsCouponsFileName = "coupons_active.txt";
        public const string ChipsCouponsInactiveFileName = "coupons_inactive.txt";
        public const string PotionGenerationFileName = "potionGeneration_data.txt";

        public const string BotTestingStatus = "Chateau Contract is currently undergoing [color=orange]testing[/color]. Please excuse the growing pains!";
        public const string BotOnlineStatus = "[color=purple]Chateau Contract Bot v" + Version + " Online![/color] Use '!help' and check the [user]Chateau Contract[/user] profile for commands.\n[session=Château Contract]adh-ac1885cd73f31adfaefb[/session]";

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
        public const string SuitenGuFateRoomId = "adh-f775cee8ae43d382eca8";
        public const string SonicHeroesDarkestHourRoomId = "adh-050daa842918c249d1b4";
        public const string VelvetCuffBotName = "VelvetCuff";
        public const string DiscordPmGuild = "_discordpm";
        public const string CurrencyPlaceholder = "[CURRENCY]";
        public const string CurrencyPlaceholderCapital = "[CURRENCYCAP]";
        public const string DefaultCurrencyName = "chip";
        public const string DefaultCurrencyNamePlural = "chips";

        private static DiscordSocketClient _client;
        private static List<SocketUser> _discordUsers;
        public BotWebRequests WebRequests;
        public VelvetcuffConnection VelvetcuffConnection;
        public BotMessageQueue MessageQueue;
        public List<BotFutureMessage> FutureMessages;
        public BotCommandController BotCommandController;
        public DiceBot DiceBot;
        // Ambient "random events" scheduler/resolver (B12). In-memory per-channel state; created
        // in Run() once the database is initialized. Profile reads/writes go through MonDB.
        public BotCommands.Support.RandomEventEngine RandomEventEngine;
        public AccountSettings AccountSettings;
        public List<SavedRollTable> SavedTables;
        public List<SavedSlotsSetting> SavedSlots;
        public List<SavedDeck> SavedDecks;
        public List<SavedPotion> SavedPotions;
        public List<SavedJobsList> SavedJobsLists;
        public List<ChannelSettings> SavedChannelSettings;
        public List<ChipsCoupon> ChipsCoupons;

        public const int StartingChipsInPile = 500;

        public const int MinimumCharactersTableId = 2;
        public const int MaximumCharactersTableName = 80; //30; 1.42c
        public const int MaximumCharactersTableDescription = 1000; //300; 1.42c
        public const int MaximumCharactersTableEntryDescription = 500; //200; 1.42c
        public const int MaximumRollTriggersPerEntry = 4;
        public const int MaximumSavedTablesPerCharacter = 4;
        public const int MaximumSavedPotionsPerCharacter = 40;//6;
        public const int MaximumSavedJobListsPerCharacter = 3;//6;
        public const int MaximumJobTiers = 10;
        public const int MaximumSavedJobEntries = 100;
        public const int MaximumCharactersPotionDescription = 1000; //500 1.42c
        public const int MaximumCharactersPotionName = 100; //50 1.42c
        public const int MaximumCharactersEiconName = 50; //50 1.42c
        public const int MaximumSavedEntriesPerTable = 50;
        public const int MaximumCardsInDeck = 200;
        public const int ReconnectTimeMs = 120000; //2 minutes reconnect time, was 5 in 1.42c
        public const int InitialWaitTime = 5000;
        public const int SlotsSpinCooldownSeconds = 300; //5 minutes
        public const int LuckForecastCooldownSeconds = 3600; //60 minutes
        public const int WorkCooldownSeconds = 3600 * 20; //20 hours
        public const int DungeonDelveCooldownSeconds = 60 * 30; //30 minutes
        public const int MaximumWorkMultiplier = 100000;
        public const int MaximumPotionCost = 100000000;
        public const int MaximumWorkTierRange = 1000;
        public const int MaximumWorkBaseAmount = 1000;
        public const int MaximumSettingStringCharacters = 1000;

        public const int GreetCharacterCooldownSeconds = 7200;// 60 * 60 * 2;// Minutes = 120;

        public const int MaximumCharsInMessageChannel = 4050; //maximum for channel message (4096 didn't work) (doesn't seem to count special characters like ♥♦♣ properly)
        public const int MaximumCharsInMessagePrivate = 49050;

        public double FListLoginTime;
        public double DiscordLoginTime;
        public const int TickTimeMiliseconds = 200;
        public int MinimumTimeBetweenMessages = 1500; //minimum 1 second between messages for FList bot rules
        public int ReconnectTimer = 0;
        public const double CheckinIntervalSeconds = 20;
        public const double ChannelOpsRefreshSeconds = 60 * 60;
        // How often the random-events scheduler scans eligible channels (seconds). Events are
        // hours apart, so a coarse scan is plenty and keeps the 200ms tick loop cheap.
        public const double RandomEventScanIntervalSeconds = 10;
        public List<string> ChannelsJoined = new List<string>();
        public List<UserGeneratedCommand> WaitingChannelOpRequests = new List<UserGeneratedCommand>();
        public List<BuyCommand> WaitingBuyCommands = new List<BuyCommand>();

        public object MessageQueueLock;

        public string LastMessageRecieved1 = "";
        public string LastMessageRecieved2 = "";
        ChannelsAuditSession CurrentChannelsAuditSession;

        public delegate void BotCommandDelegate();

        public void Run(RunMode runMode)
        {
            if(runMode != RunMode.Default)
                RunMode = runMode;

            BotCommandController = new BotCommandController(this);

            MonDB.Initialize("mongodb://localhost:27017", "ChateauDb");

            BackupAllData();

            LoadAccountSettings();

            LoadChannelSettings();

            LoadSavedTables();

            LoadSavedPotions();

            LoadSavedSlots();

            LoadChipsCoupons();

            LoadSavedDecks();

            LoadJobsLists();

            MessageQueue = new BotMessageQueue();
            FutureMessages = new List<BotFutureMessage>();
            MessageQueueLock = new object();
            WebRequests = new BotWebRequests();
            _discordUsers = new List<SocketUser>();
            DiceBot = new DiceFunctions.DiceBot(this);
            RandomEventEngine = new BotCommands.Support.RandomEventEngine(MonDB.getProfile, MonDB.setProfile);
            VelvetcuffConnection = new FChatDicebot.VelvetcuffConnection(WebRequests, AccountSettings);

            if(RunMode == RunMode.FListOnly || RunMode == RunMode.FlistPlusDiscord)
            {
                System.Threading.Thread th = new Thread(RunLoopFList);
                th.IsBackground = true;
                th.Start();
            }
            if (RunMode == RunMode.DiscordOnly || RunMode == RunMode.FlistPlusDiscord)
            {
                System.Threading.Thread th2 = new Thread(RunLoopDiscord);
                th2.IsBackground = true;
                th2.Start();
            }
        }
        #region load data
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

                    try
                    {
                        SavedChannelSettings = JsonConvert.DeserializeObject<List<ChannelSettings>>(fileText);
                    }
                    catch (System.Exception parseExc)
                    {
                        // A corrupt/truncated file used to leave SavedChannelSettings null,
                        // which crash-loops the bot the moment any code touches the list
                        // (e.g. the random-events tick, or the next channel join). Back up
                        // the bad file so it isn't silently overwritten by the next save,
                        // and fall back to an empty list so startup can proceed.
                        Console.WriteLine("Exception: Failed to parse " + path + " — starting with an empty channel settings list.\n" + parseExc.ToString());
                        try
                        {
                            File.Copy(path, path + ".corrupt-" + DateTime.UtcNow.Ticks, overwrite: false);
                        }
                        catch (System.Exception backupExc)
                        {
                            Console.WriteLine("Exception: Failed to back up corrupt " + path + "\n" + backupExc.ToString());
                        }
                    }

                    if (SavedChannelSettings == null)
                    {
                        SavedChannelSettings = new List<ChannelSettings>();
                    }

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
                if (SavedChannelSettings == null)
                {
                    SavedChannelSettings = new List<ChannelSettings>();
                }
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

        private void LoadJobsLists()
        {
            SavedJobsLists = new List<SavedJobsList>();
            string path = Utils.GetTotalFileName(FileFolder, SavedJobsListFileName);
            try
            {
                VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if (_debug)
                        Console.WriteLine("read " + path);

                    SavedJobsLists = JsonConvert.DeserializeObject<List<SavedJobsList>>(fileText);

                    if (_debug)
                        Console.WriteLine("loaded JobsLists successfully.");
                }
                else
                {
                    Console.WriteLine("LoadJobsLists file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadJobsLists for " + path + "\n" + exc.ToString());
            }
        }

        private void LoadSavedPotions()
        {
            SavedPotions = new List<SavedPotion>();
            string path = Utils.GetTotalFileName(FileFolder, SavedPotionsFileName);
            try
            {
                VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if (_debug)
                        Console.WriteLine("read " + path);

                    SavedPotions = JsonConvert.DeserializeObject<List<SavedPotion>>(fileText);

                    if (_debug)
                        Console.WriteLine("loaded SavedPotions successfully.");
                }
                else
                {
                    Console.WriteLine("LoadSavedPotions file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadSavedPotions for " + path + "\n" + exc.ToString());
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
            Utils.CopyFile(SavedPotionsFileName, FileFolder, BackupFolder);
            Utils.CopyFile(PotionGenerationFileName, FileFolder, BackupFolder);
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

#endregion
        public int PinMessageSent = 0;
        public void RunLoopFList()
        {
            if(_debug)
                Console.WriteLine("RunloopFlist started");

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
                            DiceBot.UpdateAllGames();
                        }catch(Exception exc)
                        {
                            Console.WriteLine("exception on UpdateAllGames " + exc.ToString());
                        }
                        try
                        {
                            LastMessageSent += TickTimeMiliseconds;
                            totalTicks++;

                            lock (MessageQueueLock)
                            {
                                if (MessageQueue.HasMessages())
                                {
                                    BotMessage nextMessagePeek = MessageQueue.Peek();

                                    if (nextMessagePeek != null && !nextMessagePeek.IsDiscordmessage() && (!Utils.BotMessageIsFListChatMessage(nextMessagePeek) || LastMessageSent >= MinimumTimeBetweenMessages))
                                    {
                                        BotMessage nextMessageToSend = MessageQueue.GetNextMessage();

                                        if (nextMessageToSend != null)
                                        //&& nextMessageToSend.messageType != BotMessageFactory.DiscordChannelMessage && nextMessageToSend.messageType != BotMessageFactory.DiscordPrivateMessage)
                                        {
                                            string printedCommand = nextMessageToSend.PrintedCommand();
                                            if (nextMessageToSend.messageType == "PIN")
                                            {
                                                PinMessageSent++;
                                                if (PinMessageSent % 10 == 0)
                                                {
                                                    Console.WriteLine("sending: " + printedCommand + " (#" + PinMessageSent + ")");
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("sending: " + printedCommand);
                                                Utils.AddToLog("sending: " + printedCommand, nextMessageToSend);
                                            }

                                            ws.Send(printedCommand);
                                        }
                                        if (Utils.BotMessageIsFListChatMessage(nextMessageToSend))
                                        {
                                            LastMessageSent = 0;
                                        }
                                    }
                                }
                            }

                            if (WaitingChannelOpRequests.Count > 0)
                            {
                                if (_testVersion)
                                    Console.WriteLine("Channeloprequest found in queue");

                                lock (BotCommandController.ModeratorRequestLock)
                                {
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

                            }

                            HandleWaitingBuyCommands();

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
                    HandleRandomEventsTick(TickTimeMiliseconds);

                    Thread.Sleep(TickTimeMiliseconds);
                }
            }
        }

        public void RunLoopDiscord()
        {
            if (_debug)
                Console.WriteLine("RunloopDiscord started");

            int LastMessageSent = 0;

            // When working with events that have Cacheable<IMessage, ulong> parameters,
            // you must enable the message cache in your config settings if you plan to
            // use the cached message entity.
            var _config = new DiscordSocketConfig { MessageCacheSize = 100 };
            _config.GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent;
            // (Discord.GatewayIntents) 377957493824;

            _client = new DiscordSocketClient(_config);

            _client.Log += DiscordLog;

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            var token = "";
            if (AccountSettings != null && !string.IsNullOrEmpty(AccountSettings.DiscordBotToken))
                token = AccountSettings.DiscordBotToken;

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            _client.LoginAsync(TokenType.Bot, token);
            _client.StartAsync();

            _client.MessageUpdated += MessageUpdated;
            _client.Ready += () =>
            {
                Console.WriteLine("Bot is connected!");
                return Task.CompletedTask;
            };
            _client.ChannelUpdated += (SocketChannel chan, SocketChannel chan2) =>
            {
                Console.WriteLine("Bot observed ChannelUpdated change to " + chan.ToString() + " " + chan.Id + " _ " + chan2.ToString() + " " + chan2.Id);
                return Task.CompletedTask;
            };
            _client.UserUpdated += (SocketUser chan, SocketUser chan2) =>
            {
                Console.WriteLine("Bot observed UserUpdated change to " + chan.ToString() + " " + chan.Id + " _ " + chan2.ToString() + " " + chan2.Id);
                return Task.CompletedTask;
            };
            _client.MessageReceived += RecieveDiscordMessage;

            DiscordLoginTime = DoubleTime.GetCurrentTimestampSeconds();
            int totalTicks = 0;

            while (true)
            {
                //no VC orders
                //no PIN(g) messages

                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    try
                    {
                        DiceBot.UpdateAllGames();
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("exception on UpdateAllGames " + exc.ToString());
                    }
                    try
                    {
                        LastMessageSent += TickTimeMiliseconds;
                        totalTicks++;

                        lock (MessageQueueLock)
                        {
                            if (MessageQueue.HasMessages())
                            {
                                BotMessage nextMessagePeek = MessageQueue.Peek();

                                if (nextMessagePeek != null && nextMessagePeek.IsDiscordmessage() && (!Utils.BotMessageIsFListChatMessage(nextMessagePeek) || LastMessageSent >= MinimumTimeBetweenMessages))
                                {
                                    BotMessage nextMessageToSend = MessageQueue.GetNextMessage();

                                    if (nextMessageToSend != null)
                                    {
                                        string printedCommand = nextMessageToSend.PrintedCommand();

                                        Console.WriteLine("sending to DISCORD: " + printedCommand);
                                        Utils.AddToLog("sending to DISCORD: " + printedCommand, nextMessageToSend);

                                        if (nextMessageToSend.MessageDataFormat != null && typeof(DiscordSocketMessage) == nextMessageToSend.MessageDataFormat.GetType())
                                        {
                                            DiscordSocketMessage discordMessageToSend = (DiscordSocketMessage)nextMessageToSend.MessageDataFormat;

                                            //discord send raw here
                                            ulong cid = 0;
                                            ulong.TryParse(discordMessageToSend.channel, out cid);
                                            ISocketMessageChannel discordChannel = _client.GetChannel(cid) as ISocketMessageChannel;// nextMessageToSend.MessageDataFormat..chan)

                                            //find USER to send to
                                            IUser user = GetDiscordUser(discordMessageToSend.character);

                                            string convertedMessage = TextFormat.ChangeToDiscordText(discordMessageToSend.message, user);

                                            if (nextMessageToSend.messageType == BotMessageFactory.DiscordChannelMessage)
                                            {
                                                discordChannel.SendMessageAsync(convertedMessage);
                                            }
                                            else if (nextMessageToSend.messageType == BotMessageFactory.DiscordPrivateMessage)
                                            {

                                                if (user != null)
                                                {
                                                    try
                                                    {
                                                        user.SendMessageAsync(convertedMessage);

                                                        Console.WriteLine($"Sent a direct message to {user.Username} : {convertedMessage}");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine($"Failed to send a direct message: {ex.Message}");
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"User {discordMessageToSend.character} was not found.");
                                                }
                                            }


                                            //message.Channel.SendMessageAsync("I heard your message!");
                                            //ws.Send(nextMessageToSend.PrintedCommand());

                                        }
                                    }
                                    if (Utils.BotMessageIsFListChatMessage(nextMessageToSend))
                                    {
                                        LastMessageSent = 0;
                                    }
                                }
                            }
                        }

                        if (WaitingChannelOpRequests.Count > 0)
                        {
                            if (_testVersion)
                                Console.WriteLine("Channeloprequest found in queue");

                            lock (BotCommandController.ModeratorRequestLock)
                            {
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
                        }
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("Exception inside ws.IsAlive: " + exc.ToString());
                        Utils.AddToLog("Exception inside ws.IsAlive: " + exc.ToString(), exc.StackTrace);
                    }
                }
                else
                {
                    //if (ReconnectTimer == 0)
                    //{
                    //    Console.WriteLine("Connection was lost.");
                    //    Console.WriteLine("Waiting for " + (ReconnectTimeMs / 1000) + " seconds to reconnect.");
                    //    Utils.AddToLog("Waiting for " + (ReconnectTimeMs / 1000) + " seconds to reconnect.", null);
                    //}

                    //ReconnectTimer += TickTimeMiliseconds;

                    //if (ReconnectTimer > ReconnectTimeMs)
                    //{
                    //    BotMessage reconnectIdnRequest = GetIdentificationTicket();
                    //    PerformStartupClientCommands(ws, reconnectIdnRequest, true);
                    //    ReconnectTimer = 0;
                    //}
                }
                HandleFutureMessagesTick(TickTimeMiliseconds);

                Thread.Sleep(TickTimeMiliseconds);
            }
        }

        public static IUser GetDiscordUser(string userName)
        {
            IUser user = _client.Guilds
                .SelectMany(g => g.Users)
                .FirstOrDefault(u => u.Username == userName);// _username && u.Discriminator == _discriminator);

            if (user == null) //for messages that do not come from guilds, they are not stored in _client.Guilds but caught here
                user = _discordUsers.FirstOrDefault(a => a.Username == userName);

            return user;
        }

        private async Task RecieveDiscordMessage(SocketMessage message)
        {
            // Assuming 'message' is a SocketMessage object
            // First, make sure the message was sent in a guild (server)
            string guildName = "";
            string guildId = "";
            if (message.Channel is SocketGuildChannel guildChannel)
            {
                // Access the guild (server) associated with the channel
                var guild = guildChannel.Guild;

                // Now you can work with the guild object
                // For example, you can get the guild's name:
                //guildName = guild.Name;
                guildId = guild.Id.ToString();
                // Or any other properties or methods available in the IGuild interface
            }
            else
            {
                if(_discordUsers.Count(a => a.Username == message.Author.Username) == 0)
                {
                    _discordUsers.Add(message.Author);
                }

                guildId = DiscordPmGuild;
            }

            if(_debug)
            Console.WriteLine("Bot observed MessageReceived " + message.Id
                + "\nContent: " + message.Content + "\nChannel: " + message.Channel.Name
                + "\nGuild Name: " + guildName + " " + guildId
                + "\nAuthor: " + message.Author.Username + " global " + message.Author.GlobalName
                + "\nApplication: " + message.Application?.Name + "\nSource: " + message.Source.ToString());

                //MSGserver msgContent = JsonConvert.DeserializeObject<MSGserver>(trimmedChatCommand);
                InterpretChatCommand(new ChatMessage(message, guildId));

            //ISocketMessageChannel channel =
            //message.Channel

            if(_debug)
                await message.Channel.SendMessageAsync("I heard your message!");

        }

        private static Task DiscordLog(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            if(_debug)
                Console.WriteLine($"MessageUpdated recieved");
            // If the message was not in the cache, downloading it will result in getting a copy of `after`.
            var message = await before.GetOrDownloadAsync();
            if(_debug)
                Console.WriteLine($"{message} -> {after}");
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
                        string chipsAdded = DiceBot.AddChips(new MessageAddress() { character = command.CharacterName, channel = CasinoChannelId }, amount, false);
                        //send confirm message to casino
                        SendMessageInChannel("Chips recorded in the VC Casino Channel: " + chipsAdded, new MessageAddress() { channel = command.ChannelName });
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

        private void HandleFutureMessagesTick(int tickMs)
        {
            lock (MessageQueueLock)
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
                                SendMessageInChannel(mes.MessageContent, mes.Address);// mes.Channel);
                            else
                                SendPrivateMessage(mes.MessageContent, mes.Address);

                            mes.Sent = true;
                        }
                    }

                    FutureMessages.RemoveAll(b => b.Sent);
                }
            }
        }

        // Heartbeat for the ambient random-events scheduler (B12), sibling to
        // HandleFutureMessagesTick. The per-channel scan is throttled to RandomEventScanIntervalSeconds
        // so we don't hammer the loop or Mongo: most ticks do nothing, and the event list is only
        // loaded when an event is actually about to fire. Only opted-in, currently-joined channels
        // are eligible. Wrapped per-channel so one bad channel can't abort the others.
        private double _lastRandomEventScanSeconds = 0;
        private void HandleRandomEventsTick(int tickMs)
        {
            if (RandomEventEngine == null || SavedChannelSettings == null)
                return;

            double nowSeconds = DoubleTime.GetCurrentTimestampSeconds();
            if (nowSeconds - _lastRandomEventScanSeconds < RandomEventScanIntervalSeconds)
                return;
            _lastRandomEventScanSeconds = nowSeconds;

            DateTime utcNow = DateTime.UtcNow;
            List<ChannelSettings> eligible = SavedChannelSettings
                .Where(cs => cs != null && cs.AllowRandomEvents && ChannelsJoined != null && ChannelsJoined.Contains(cs.Name))
                .ToList();

            foreach (ChannelSettings cs in eligible)
            {
                try
                {
                    List<string> messages = RandomEventEngine.Tick(cs.Name, utcNow, () => MonDB.getRandomEvents());
                    foreach (string m in messages)
                    {
                        if (!string.IsNullOrEmpty(m))
                            SendMessageInChannel(m, cs.Name);
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Exception in HandleRandomEventsTick for " + cs.Name + ": " + exc.ToString());
                    Utils.AddToLog("Exception in HandleRandomEventsTick for " + cs.Name + ": " + exc.ToString(), null);
                }
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

            FListLoginTime = DoubleTime.GetCurrentTimestampSeconds();

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
                List<MessageAddress> messageAddresses = new List<MessageAddress>();
                foreach (ChannelSettings channel in SavedChannelSettings)
                {
                    if (channel.StartupChannel)
                    {
                        MessageAddress m = new MessageAddress() { channel = channel.Name };
                        JoinChannel(m, true);
                        messageAddresses.Add(m);
                    }
                }
                //deactivated: it now uses the !auditchannels command to perform a full audit instead of justchannels joined
                //JoinChannelsSession jchSesh = new JoinChannelsSession(messageAddresses);
                //use CurrentChannelJoinSession until all join channel messages are resolved. track # sent.
                //////////// ^
                //CurrentChannelJoinSession = jchSesh;
            }
        }

        
        private bool GetNewApiTicket()
        {
            CurrentApiKey = null;
            // Clear any previous (now stale/expired) ticket before requesting a new one. The
            // loop below only waits while ApiTicketResult is null — on reconnect it would
            // otherwise still be holding the old ticket, so the wait loop skips entirely and
            // callers reuse the expired ticket until the async login response eventually
            // catches up on its own.
            WebRequests.ApiTicketResult = null;
            WebRequests.LoginToServerPublic(AccountSettings);

            int timeout = 100000;
            while (WebRequests.ApiTicketResult == null)
            {
                timeout -= TickTimeMiliseconds;
                Thread.Sleep(TickTimeMiliseconds);
                if (timeout <= 0)
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
                    case "COL": //channel OP list, sent in response to join channel and when op list requested
                        COLserver colinfo = JsonConvert.DeserializeObject<COLserver>(trimmedChatCommand);

                        Console.WriteLine("Recieved: " + (data.Length > 300 ? data.Substring(0, 300) : data));
                        Utils.AddToLog("Recieved: " + (data.Length > 300 ? data.Substring(0, 300) : data), null);
                        //UserGeneratedCommand req = WaitingChannelOpRequests.FirstOrDefault(b => b.channel == colinfo.channel);
                        UserGeneratedCommand req = WaitingChannelOpRequests.FirstOrDefault(b => b.channelOpRequested == colinfo.channel);

                        if (req != null)
                        {
                            Console.WriteLine("channelops returned COL, channel ops returned");
                            req.ops = colinfo.oplist;
                        }

                        if (colinfo != null && SavedChannelSettings != null && colinfo.channel != null)
                        {
                            var channelSettings = SavedChannelSettings.FirstOrDefault(a => a.Name.ToLower() == colinfo.channel.ToLower());
                            if (channelSettings != null)
                            {
                                channelSettings.SetChannelOps(colinfo.oplist);
                            }
                        }

                        //else
                        //{
                        //}
                        //join audit
                        if (colinfo == null)
                        {

                            Console.WriteLine("error: failed to parse colinfo: " + trimmedChatCommand);
                            Utils.AddToLog("error: failed to parse colinfo: " + trimmedChatCommand, null);
                        }
                        else
                        {
                            if (CurrentChannelsAuditSession != null)
                            {
                                //mark channel as a channel that exists in data
                                ResolveCompletedAudit(CurrentChannelsAuditSession.ChannelJoinResult(new MessageAddress() { channel = colinfo.channel, character = null, guild = null }, true));
                            }
                        }
                        break;
                    case "CDS": //channel description, sent in response to successful join channel
                        CDSserver cdsInfo = JsonConvert.DeserializeObject<CDSserver>(trimmedChatCommand);
                        if (cdsInfo != null)
                        {
                            if(ChannelsJoined.Count(a => a == cdsInfo.channel) == 0)
                                ChannelsJoined.Add(cdsInfo.channel);
                        }
                        //this is for the automatic channel join audit, which has been deactivated on startup. it requires the command to start. COL will work instead

                        //if (cdsInfo == null)
                        //{

                        //    Console.WriteLine("error: failed to parse cdsInfo: " + trimmedChatCommand);
                        //    Utils.AddToLog("error: failed to parse cdsInfo: " + trimmedChatCommand, null);
                        //}
                        //else
                        //{
                        //    if (CurrentChannelJoinSession != null)
                        //    {
                        //        //remove channel from data
                        //        ResolveChannelJoinsAudit(CurrentChannelJoinSession.ChannelJoinResult(new MessageAddress() { channel = cdsInfo.channel, character = null, guild = null }, true));
                        //    }
                        //}
                        break;
                    case "JCH": //someone joined a channel the bot is in
                        JCHserver jchInfo = JsonConvert.DeserializeObject<JCHserver>(trimmedChatCommand);

                        MessageAddress address = new MessageAddress() { character = jchInfo.character.identity, channel = jchInfo.channel, guild = null };
                        ChannelSettings s = GetChannelSettings( address );//jchInfo.channel, null);

                        if (s.GreetNewUsers)
                        {
                            CharacterData thisCharacterData = DiceBot.GetCharacterData(address, true);
                                //jchInfo.character.identity, jchInfo.channel, true);

                            double currentDoubleTime = DoubleTime.GetCurrentTimestampSeconds();
                            double timeSinceGreet = currentDoubleTime - thisCharacterData.LastGreeted;

                            if (timeSinceGreet < BotMain.GreetCharacterCooldownSeconds || (s.GreetUsersOnlyOnceEver && thisCharacterData.LastGreeted > 0))
                            {
                                if (_debug)
                                    SendMessageInChannel("DEBUG: Cooldown active for: Welcome to the channel, " + TextFormat.GetCharacterUserTags(jchInfo.character.identity) + "! " + timeSinceGreet + " seconds since last greet.", address);
                            }
                            else
                            {
                                thisCharacterData.LastGreeted = currentDoubleTime;
                                BotCommandController.SaveCharacterDataToDisk();
                                string greetingMessage = string.IsNullOrEmpty(s.GreetingMessage)? "Welcome to the channel, [CHARACTERNAME]!": s.GreetingMessage;
                                greetingMessage = greetingMessage.Replace("[CHARACTERNAME]", TextFormat.GetCharacterUserTags(jchInfo.character.identity));

                                SendMessageInChannel(greetingMessage, address);
                            }
                        }
                        break;
                    case "LCH"://someone left a channel the bot is in
                        break;
                    case "PIN": //ping from server. reply with ping
                        lock (MessageQueueLock)
                        {
                            MessageQueue.AddMessage(new BotMessage() { MessageDataFormat = null, messageType = "PIN" });
                        }
                        break;
                    case "ERR": //error from server. usually channel join issue
                        Console.WriteLine("Recieved: " + (data.Length > 300 ? data.Substring(0, 400) : data));
                        Utils.AddToLog("Recieved: " + (data.Length > 300 ? data.Substring(0, 400) : data), null);

                        ERRserver errInfo = JsonConvert.DeserializeObject<ERRserver>(trimmedChatCommand);

                        if (errInfo != null)
                        {
                            if (errInfo.number == 26) //channel not found
                            {
                                if (CurrentChannelsAuditSession != null)
                                {
                                    //mark failure to find channel
                                    ResolveCompletedAudit(CurrentChannelsAuditSession.ChannelJoinResult(null, false));
                                }
                            }
                            else if (errInfo.number == 28) //channel already joined
                            {
                                // This is a duplicate-join response for whichever channel join
                                // was just sent — it doesn't mean every other still-queued
                                // channel join is bad, so don't purge the whole queue over it
                                // (that used to silently drop every other pending startup/audit
                                // channel join whenever one channel was already joined).
                                CurrentChannelsAuditSession = null;
                            }
                        }
                        //if (CurrentChannelsAuditSession != null)
                        //{
                        //    if (data.Contains("Could not locate the requested channel."))
                        //    {
                        //    }
                        //    else if (data.Contains("Already"))
                        //    {
                        //    }
                        //}

                        break;
                    case "PRI": //private message from a user
                        PRICmd msgContentPri = JsonConvert.DeserializeObject<PRICmd>(trimmedChatCommand);
                        InterpretChatCommand(new ChatMessage() { character = msgContentPri.character, message = msgContentPri.message, channel = null, guild = null });
                        break;
                    case "MSG": //message sent in channel
                        MSGserver msgContent = JsonConvert.DeserializeObject<MSGserver>(trimmedChatCommand);
                        InterpretChatCommand(new ChatMessage(msgContent));
                        break;
                    default:
                        Utils.AddToLog("Recieved: " + (data.Length > 400 ? data.Substring(0, 300) : data), null);
                        Console.WriteLine("Recieved: " + (data.Length > 400 ? data.Substring(0, 300) : data));
                        break;
                }
            }catch(Exception exc)
            {
                Console.WriteLine("Exception inside OnMessage: " + exc.ToString());
                Utils.AddToLog("Exception inside OnMessage: " + exc.ToString(), exc.StackTrace);
            }
        }

        public void InterpretChatCommand(ChatMessage messageContent)
        {
            string prefixChar = "!";
            //set prefix char from settings if this is from a guild/ channel
            MessageAddress newAddress = new MessageAddress(messageContent);
            ChannelSettings channelSettings = null;
            if(BotCommandController.MessageCameFromChannel(newAddress))//.add !string.IsNullOrEmpty(messageContent.channel))
            {
                channelSettings = GetChannelSettings(new MessageAddress() { character = messageContent.character, channel = messageContent.channel, guild = messageContent.guild });
                prefixChar = channelSettings.CommandPrefix.ToString();
            }
            string[] channelOps = channelSettings != null ? channelSettings.GetChannelOps() : null;

            // Activity gate for ambient random events (B12): any human message in a channel keeps
            // it "awake" so the scheduler only fires into rooms with live chatter. The bot's own
            // posts are excluded so they can't keep a dead room artificially alive.
            if (RandomEventEngine != null
                && BotCommandController.MessageCameFromChannel(newAddress)
                && (AccountSettings == null || !string.Equals(messageContent.character, AccountSettings.CharacterName, StringComparison.OrdinalIgnoreCase)))
            {
                RandomEventEngine.RecordActivity(messageContent.channel, DateTime.UtcNow);
            }

            if (string.IsNullOrEmpty(messageContent.message))
            {
                Console.WriteLine("empty message " + messageContent.channel + ", " + messageContent.character + ": " + messageContent.message);
                return;
            }

            //remove leading white spaces
            while(messageContent.message.StartsWith(" ") && messageContent.message.Length > 2)
            {
                messageContent.message = messageContent.message.Substring(1);
            }

            //bot commands in chat all start with '!' (or prefix char)
            if (messageContent.message.StartsWith(prefixChar) && messageContent.message.Length > 1)
            {
                string commandName = "";
                string[] commandTerms = SeparateCommandTerms(messageContent.message, out commandName);

                BotCommandController.RunChatBotCommand(new UserGeneratedCommand(){
                    rawTerms = commandTerms,
                    channel = messageContent.channel,
                    characterName = messageContent.character,
                    guild = messageContent.guild,
                    commandName = commandName,
                    ops = channelOps,
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
                lock (MessageQueueLock)
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
        }

        public string[] SeparateCommandTerms(string rawCommandMessage, out string commandName)
        {
            string[] splitSpace = rawCommandMessage.Split(' ');
            commandName = splitSpace[0].Substring(1).ToLower();
            string[] commandTerms = splitSpace.Skip(1).ToArray();
            return commandTerms;
        }

        public void SendMessageInChannel(string message, MessageAddress address)
        {
            ChannelSettings settings = GetChannelSettings(address);
            message = TextFormat.ApplyNumberCommasIfNecessary(message, settings);
            message = TextFormat.SubstituteInCurrencyName(message, settings);
            message = TextFormat.SpoilerAllIfEnabled(message, settings);

            lock (MessageQueueLock)
            {
                if (!string.IsNullOrEmpty(address.guild)) //discord message -- but guild would be null on private messages... (priv message other method)
                {
                    MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.DiscordChannelMessage, new DiscordSocketMessage() { channel = address.channel, guild = address.guild, character = address.character, message = message }));
                }
                else //flist message
                {
                    MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.MSG, new MSGclient() { channel = address.channel, message = message }));
                }
            }

            settings.LastBotmessageToChannel = DoubleTime.GetCurrentTimestampSeconds();
            settings.TotalBotMessages++;
            BotCommandController.SaveChannelSettingsToDisk();
        }

        // Convenience overload for legacy string-channel callers (F-Chat Chateau commands).
        // Wraps the channel id into a MessageAddress; guild stays null => F-Chat path.
        public void SendMessageInChannel(string message, string channel)
        {
            SendMessageInChannel(message, new MessageAddress() { channel = channel });
        }

        public void SendFutureMessage(string message, MessageAddress address, bool channelMessage, int waitMs)
        {
            lock (MessageQueueLock)
            {
                FutureMessages.Add(new BotFutureMessage()
                {
                    Address = address,
                    //Channel = channel,
                    //Guild = guild,
                    //Character = recipient,
                    MessageContent = message,
                    ChannelMessage = channelMessage,
                    MsWait = waitMs
                });
            }
        }

        public void SendPrivateMessage(string message, MessageAddress address)// string recipient)
        {
            ChannelSettings settings = GetChannelSettings(address);
            message = TextFormat.ApplyNumberCommasIfNecessary(message, settings);
            message = TextFormat.SubstituteInCurrencyName(message, settings);
            message = TextFormat.SpoilerAllIfEnabled(message, settings);

            lock (MessageQueueLock)
            {
                if (Utils.IsDiscordMessage(address))
                {
                    MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.DiscordPrivateMessage, new DiscordSocketMessage() { channel = address.channel, guild = address.guild, character = address.character, message = message }));
                }
                else //flist message
                {
                    MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.PRI, new PRIclient() { recipient = address.character, message = message }));
                }
            }
        }

        // Convenience overload for legacy string-recipient callers (F-Chat Chateau commands).
        public void SendPrivateMessage(string message, string recipient)
        {
            SendPrivateMessage(message, new MessageAddress() { character = recipient });
        }

        public void JoinChannel(MessageAddress address, bool startupJoin) //FList only
        {
            lock (MessageQueueLock)
            {
                MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.JCH, new JCHclient() { channel = address.channel }));
                if (startupJoin && CurrentChannelsAuditSession != null)
                {
                    CurrentChannelsAuditSession.AttemptToJoin(address);
                }
            }
        }


        public void BeginChannelAudit()
        {
            lock (MessageQueueLock)
            {
                if (MessageQueue.HasMessages())
                {
                    Console.WriteLine("Error: message queue has values. cannot safely perform channel audit. try again when there are no queued messages");
                    return;
                }
            }
            ChannelsJoined = new List<string>();

            List<MessageAddress> messageAddresses = new List<MessageAddress>();

            foreach (ChannelSettings channel in SavedChannelSettings)
            {
                lock (MessageQueueLock)
                {
                    if (!channel.IsDiscordChannel())
                    {
                        MessageAddress address = new MessageAddress() { channel = channel.Name };
                        MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.COL, new COLclient() { channel = address.channel }));

                        messageAddresses.Add(address);
                    }
                }
            }
            ChannelsAuditSession jchSesh = new ChannelsAuditSession(messageAddresses);
            CurrentChannelsAuditSession = jchSesh;
        }


        private void ResolveCompletedAudit(List<MessageAddress> deleteableAddresses)
        {
            if (deleteableAddresses != null)
            {
                //careful about deleting ones that are discord servers...
                Console.WriteLine("Deletable addresses was not null, count " + deleteableAddresses.Count + "\n");
                Utils.AddToChannelAuditLog("Deletable addresses was not null, count " + deleteableAddresses.Count + "\n", deleteableAddresses);
                int countBefore = SavedChannelSettings.Count;
                List<ChannelSettings> removedChannelsSettings = new List<ChannelSettings>();
                foreach (MessageAddress address in deleteableAddresses)
                {
                    ChannelSettings s = GetChannelSettings(address);
                    if (!s.IsDiscordChannel())
                    {
                        removedChannelsSettings.Add(s);
                        SavedChannelSettings.RemoveAll(a => a.Name.ToLower() == address.channel.ToLower());
                    }
                }
                int countAfter = SavedChannelSettings.Count;
                string channelsList = Utils.PrintList(deleteableAddresses.Select(a => a.channel).ToList());

                string channelSettingsList = "";// Utils.PrintList(SavedChannelSettings.Select(a => a. deleteableAddresses.Select(a => a.channel).ToList());
                foreach (ChannelSettings thisSettings in removedChannelsSettings)
                {
                    channelSettingsList += "\n" + JsonConvert.SerializeObject(thisSettings);
                }
                string outputChannelsInfo = "ResolveChannelJoinsAudit Removed deletable channels from settings. Before: " + countBefore + " After: " + countAfter + " List: " + channelsList + " \nchannels Infos: " + channelSettingsList;
                Console.WriteLine(outputChannelsInfo);
                Utils.AddToChannelAuditLog(outputChannelsInfo, null);
                BotCommandController.SaveChannelSettingsToDisk();

                countBefore = DiceBot.ChipPiles.Count;
                List<ChipPile> removedChipPiles = new List<ChipPile>();
                foreach (MessageAddress addr in deleteableAddresses)
                {
                    List<ChipPile> channelPiles = DiceBot.ChipPiles.Where(a => a.ChannelId.ToLower() == addr.GetChannelKey().ToLower()).ToList();
                    removedChipPiles.AddRange(channelPiles);
                    if (channelPiles != null && channelPiles.Count > 0)
                    {
                        foreach (ChipPile p in channelPiles)
                        {
                            DiceBot.ChipPiles.Remove(p);
                        }
                    }
                }

                countAfter = DiceBot.ChipPiles.Count;
                string chipPilesList = "";// Utils.PrintList(deleteableAddresses.Select(a => a.channel).ToList());
                foreach (ChipPile thisPile in removedChipPiles)
                {
                    chipPilesList += "\n" + JsonConvert.SerializeObject(thisPile);
                }
                string outputChipPilesInfo = "ResolveChannelJoinsAudit Removed chipPiles for deleted channels. Before: " + countBefore + " After: " + countAfter + " \nList: " + chipPilesList;
                Console.WriteLine(outputChipPilesInfo);
                Utils.AddToChannelAuditLog(outputChipPilesInfo, null);

                countBefore = DiceBot.ChipPiles.Count;
                removedChipPiles = new List<ChipPile>();
                foreach (ChipPile p in DiceBot.ChipPiles)
                {
                    if (SavedChannelSettings.Count(a => a.Name.ToLower() == p.ChannelId.ToLower()) == 0)
                    {
                        removedChipPiles.Add(p);
                    }
                }
                chipPilesList = "";
                foreach (ChipPile thisPile in removedChipPiles)
                {
                    DiceBot.ChipPiles.Remove(thisPile);
                    chipPilesList += "\n" + JsonConvert.SerializeObject(thisPile);
                }
                countAfter = DiceBot.ChipPiles.Count;
                outputChipPilesInfo = "ResolveChannelJoinsAudit Removed chipPiles with no channel recorded. Before: " + countBefore + " After: " + countAfter + " \nList: " + chipPilesList;
                Console.WriteLine(outputChipPilesInfo);
                Utils.AddToChannelAuditLog(outputChipPilesInfo, null);

                BotCommandController.SaveChipsToDisk("removechanneljoinsaudit");



                countBefore = DiceBot.CharacterDatas.Count;
                List<CharacterData> removedCharacterDatas = new List<CharacterData>();

                List<CharacterData> nullChannels = DiceBot.CharacterDatas.Where(a => a.Channel == null).ToList();// new List<CharacterData>();

                foreach (CharacterData p in DiceBot.CharacterDatas)
                {
                    if (p.Channel == null || SavedChannelSettings.Count(a => a.Name.ToLower() == p.Channel.ToLower()) == 0)
                    {
                        removedCharacterDatas.Add(p);
                    }
                }
                string characterDataList = "";
                foreach (CharacterData thisPile in removedCharacterDatas)
                {
                    DiceBot.CharacterDatas.Remove(thisPile);
                    characterDataList += "\n" + JsonConvert.SerializeObject(thisPile);
                }
                countAfter = DiceBot.CharacterDatas.Count;
                string outputCharacterDatasInfo = "ResolveChannelJoinsAudit Removed character datas with no channel recorded. Before: " + countBefore + " After: " + countAfter + " \nList: " + characterDataList;
                Console.WriteLine(outputCharacterDatasInfo);
                Utils.AddToChannelAuditLog(outputCharacterDatasInfo, null);

                BotCommandController.SaveCharacterDataToDisk();

                CurrentChannelsAuditSession = null;
            }
        }

        public void LeaveChannel(MessageAddress address) //FList only
        {
            lock (MessageQueueLock)
            {
                ChannelsJoined.Remove(address.channel);
                MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.LCH, new LCHclient() { channel = address.channel }));
            }
        }

        public void SetStatus(string status, string message)
        {
            lock (MessageQueueLock)
            {
                MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.STA, new STAclient() { status = status, statusmsg = message }));
            }
        }

        public void SetChannelDescription(string channel, string description)//only fchat viable
        {
            lock (MessageQueueLock)
            {
                ChannelSettings set = GetChannelSettings(new MessageAddress() { channel = channel }, false);
                if (set != null && set.AllowSettingChannelDescription)
                {
                    MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.CDS, new CDSclient() { channel = channel, description = description }));
                }
            }
        }

        public void RequestChannelOpListAndQueueFurtherRequest(UserGeneratedCommand command)
        {
            if(!Utils.IsDiscordMessage(command))
                RequestChannelOpListAndQueueFurtherRequest(command, command.channel);
            else
                Console.WriteLine("Attempted channeloplist from discord message " + command.channel + ", " + command.commandName);
            //lock (BotCommandController.ModeratorRequestLock)
            //{
            //    WaitingChannelOpRequests.Add(command);
            //    lock (MessageQueueLock)
            //    {
            //        MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.COL, new COLclient() { channel = command.channel }));
            //    }
            //}
        }

        public void RequestChannelOpListAndQueueFurtherRequest(UserGeneratedCommand command, string checkChannelId)
        {
            lock (BotCommandController.ModeratorRequestLock)
            {
                command.channelOpRequested = checkChannelId;
                WaitingChannelOpRequests.Add(command);
                lock (MessageQueueLock)
                {
                    MessageQueue.AddMessage(BotMessageFactory.NewMessage(BotMessageFactory.COL, new COLclient() { channel = checkChannelId }));
                }
            }
        }

        public List<Enchantment> GetChannelPotions(MessageAddress address)
        {
            if (string.IsNullOrEmpty(address.channel))
                return null;
            string channelKey = address.GetChannelKey();

            if(SavedPotions == null)
            {
                Console.WriteLine("Error: savedpotions null");
                return null;
            }
            else
            {
                return SavedPotions.Where(a => a.Channel.ToLower() == channelKey.ToLower()).Select(a => a.Enchantment).ToList();
            }
        }

        public List<Enchantment> GetCharacterTotalEnchantments(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return null;

            if (SavedPotions == null)
            {
                Console.WriteLine("Error: savedpotions null");
                return null;
            }
            else
            {
                return SavedPotions.Where(a => a.OriginCharacter == characterName).Select(a => a.Enchantment).ToList();
            }
        }

        public List<JobsList> GetCharacterTotalJobsLists(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return null;

            if (SavedPotions == null)
            {
                Console.WriteLine("Error: savedpotions null");
                return null;
            }
            else
            {
                return SavedJobsLists.Where(a => a.OriginCharacter == characterName).Select(a => a.JobsList).ToList();
            }
        }

        public ChannelSettings GetChannelSettings(MessageAddress address, bool makeIfNull = true)
        {
            if (string.IsNullOrEmpty(address.channel) || address.guild == BotMain.DiscordPmGuild)
                return null;

            bool discordGuild = !string.IsNullOrEmpty(address.guild);
            string channelKey = address.GetChannelKey();
            ChannelSettings set =  SavedChannelSettings.FirstOrDefault(a => a.Name.ToLower() == channelKey.ToLower());

            if(set == null && makeIfNull)
            {
                set = new ChannelSettings()
                {
                    Name = channelKey,
                    AllowCustomTableRolls = true,
                    AllowTableRolls = true,
                    AllowTableInfo = true,
                    AllowChips = true,
                    AllowGames = true,
                    AllowSettingChannelDescription = false,
                    AllowSlots = true,
                    AllowRPG = true,
                    AllowNsfw = !discordGuild,
                    SlotsMultiplierLimit = 1000,
                    WorkMultiplier = 100,
                    WorkTierRange = 5,
                    WorkBaseAmount = 0,
                    StartingChips = 0,
                    UseDefaultPotions = true,
                    PotionChipsCost = 0,
                    LuckForecastChipsCost = 10, //was 200
                    CommandPrefix = '!',
                    ChipsClearance = ChipsClearanceLevel.ChannelOp,//.NONE,
                    CardPrintSetting = new PrintSetting() { SortCards = true, UsePlayingCardEmotes = true },
                    OnlyOpViewOthersChips = false,
                    WorkCooldownSeconds = BotMain.WorkCooldownSeconds,
                    SlotsCooldownSeconds = BotMain.SlotsSpinCooldownSeconds,
                    LuckForecastCooldownSeconds = BotMain.LuckForecastCooldownSeconds,
                    DungeonDelveCooldownSeconds = BotMain.DungeonDelveCooldownSeconds,
                    AllowChess = true,
                    AllowChessEicons = false,
                    AllowWork = true,
                    DefaultSlotsType = discordGuild ? SlotsType.Fruit : SlotsType.Bondage,
                    ShowCommasInNumbers = false,
                    CurrencyName = BotMain.DefaultCurrencyName,
                    CurrencyNamePlural = BotMain.DefaultCurrencyNamePlural,
                    SinglePlayerGameCooldownSeconds = 300,
                    GreetingMessage = "Welcome to the channel, [CHARACTERNAME]!",
                    GreetUsersOnlyOnceEver = false,
                    WorkCommandAlias = "work",
                    TotalBotMessages = 0,
                    LastBotmessageToChannel = 0,
                    LastBlackjackGameTime = 0,
                    LastRouletteSpinTime = 0,
                    ChannelDisplayName = "NoName",
                    DirectoryListing = "Join our channel!",
                    ShowInDirectory = true,
                    PotionCommandsAlias = "potion",
                    ShowSpoilerSlots = true,
                    SpoilerAllOutputs = false,
                    CreationDate = DoubleTime.GetCurrentTimestampSeconds(),

                    LastChannelOpsRequestTime = 0,
                    //ChannelOps = null,
                    ChannelOpsDirty = true
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

    public enum RunMode
    {
        NONE,
        FListOnly,
        DiscordOnly,
        FlistPlusDiscord,
        Default
    }
}