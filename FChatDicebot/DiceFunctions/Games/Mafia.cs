using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class Mafia : IGame
    {
        //todo: assign random and custom roles at beginning of game
        //todo: add voteendphase command to end phase early
        public const int RoundResultWaitSeconds = 300;
        public const int PlayerWhisperWaitSeconds = 4;
        public const int WaitPerPlayerMs = 1500;
        public const int MinimumRolePriority = 1; public const int MaximumRolePriority = 15;
        public const int MorningNewsSeconds = 10;
        public const int DaytimeSeconds = 210;
        public const int VotingTimeSeconds = 60;//todo: end early if everyone votes
        public const int VotingDefenseSeconds = 60;//todo: end early if everyone votes
        public const int VotingTrialSeconds = 60;//todo: end early if everyone votes
        public const int TrialResultSeconds = 10;
        public const int NighttimeSeconds = 120;
        public const int StartupReadingSeconds = 120;

        #region IGameRequiredData
        public string GetGameName()
        {
            return "Mafia";
        }

        public int GetMaxPlayers()
        {
            return 20;
        }

        public int GetMinPlayers()
        {
            return 6;
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
            return true;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 0;
        }
        #endregion

        public string GetGameHelp()
        {
            string thisGameCommands = "setroles (basic / custom / random), showplayers, showroles, endphase, capture (on / off), revealdeath (on / off)" +
                "(at night only - send to " + DiceBot.DiceBotCharacter + " in private message): usepower PLAYER NAME (at night, use your power on the selected target, or yourself) usepowersecondary PLAYER NAME";
            string thisGameStartupOptions = "revealdeath (reveal roles on death) basicroles OR customroles OR randomroles (set the starting roles assigned) capture OR lethal (set elimination verbs to capture or death)" +
                "\nThe default rules are: basic roles, no reveal on death, capture deaths on";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbmafia1[/eicon][eicon]dbmafia2[/eicon]";
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            string fullStatus = "";
            if (session.MafiaData != null)
            {
                if (session.MafiaData.MafiaPlayers != null && session.MafiaData.MafiaPlayers.Count() > 0)
                {
                    fullStatus = "Active Players: " + GetPlayerList(false, session, false);
                    string rolesList = GetRolesList(session, false);
                    fullStatus += "\nPossible Roles [sub](not in order)[/sub]: " + rolesList;
                    string alivePlayers = session.MafiaData.MafiaPlayers.Count(a => !a.Eliminated) + "/" + session.MafiaData.MafiaPlayers.Count();
                    fullStatus += "\nPlayers Alive: " + alivePlayers;
                }
                if(session.State == GameState.GameInProgress)
                {
                    fullStatus += "\nCurrent Phase: " + session.MafiaData.CurrentGamePhase;
                    if(session.QueuedActions != null && session.QueuedActions.Count() > 0)
                    {
                        QueuedAction act = session.QueuedActions.FirstOrDefault(a => a.QueuedActionType == QueuedActionType.AdvanceGamePhase);
                        double remainingTime = act.TriggerTime - Utils.GetCurrentTimestampSeconds();
                        fullStatus += "(" + Utils.PrintTimeFromSeconds(remainingTime) + " remaining)";
                    }
                }
                //if (session.RockPaperScissorsData.CurrentGamePhase == RockPaperScissorsGamePhase.WaitingForThrows)
                //{
                //    if (!string.IsNullOrEmpty(fullStatus))
                //        fullStatus += "\n";

                //    //get who has not yet made a symbol for the round
                //    List<RockPaperScissorsPlayer> unfinishedPlayers = session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.GameSymbol == GameSymbol.NONE).ToList();
                //    if(unfinishedPlayers.Count() >= 1)
                //    {
                //        fullStatus += string.Join(", ", unfinishedPlayers.Select(a => a.PlayerName)) + " still need to send their plays for this round.";
                //    }
                //}
            }
            
            return fullStatus;
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            //starting lives 2-4
            //set ante

            messageString = "";

            if(!session.MafiaData.RulesSet)
            {
                string rulesString = "";
                session.MafiaData.RulesSet = true;

                session.MafiaData.RevealRolesOnDeath = false;
                session.MafiaData.SetupRules = MafiaSetupRules.Basic;

                if(terms.Contains("revealdeath"))
                {
                    session.MafiaData.RevealRolesOnDeath = true;
                    rulesString += " (revealing roles on elimination)";
                }

                string captureString = " (elimination is capture)";
                session.MafiaData.UseCaptureElimination = true;

                if (terms.Contains("capture") || terms.Contains("capturedeath") || terms.Contains("captureelimination"))
                {
                    session.MafiaData.UseCaptureElimination = true;
                    captureString = " (elimination is capture)";
                }

                if (terms.Contains("lethal") || terms.Contains("lethaldeath") || terms.Contains("lethalelimination"))
                {
                    session.MafiaData.UseCaptureElimination = false;
                    captureString = " (elimination is death)";
                }

                string rolesString = "(basic roles)";

                if(terms.Contains("basicroles"))
                {
                    session.MafiaData.SetupRules = MafiaSetupRules.Basic;
                    rolesString = "(basic roles)";
                }
                if (terms.Contains("customroles"))
                {
                    session.MafiaData.SetupRules = MafiaSetupRules.Custom;
                    rolesString = "(custom roles)";
                }
                if (terms.Contains("randomroles"))
                {
                    session.MafiaData.SetupRules = MafiaSetupRules.Random;
                    rolesString = "(random roles)";
                }

                if(ante > 0)
                {
                    rulesString += " (ante set to " + ante + ")";
                }

                messageString = rolesString + rulesString + captureString;
            }

            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            string messageString = "";
            if (session.MafiaData != null && session.MafiaData.MafiaPlayers != null)
            {
                
                var player = GetMafiaPlayerByName(session, characterName);

                if(player != null)
                {
                    session.MafiaData.MafiaPlayers = session.MafiaData.MafiaPlayers.Where(a => a.PlayerName != characterName).ToList();

                    messageString = Utils.GetCharacterUserTags(characterName) + " was removed from the lobby.";
                }
            }

            return messageString;
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            string outputString = "";
            
            //set up starting data 
            foreach (string player in playerNames)
            {
                session.MafiaData.MafiaPlayers.Add(new MafiaPlayer()
                {
                    PlayerName = player,
                    Eliminated = false,
                    CurrentPlayerTarget = null,
                    UsingPower = false,
                    Stunned = false,
                    Regenerate = false,
                    Role = null,
                    VisitingNightPlayers = new List<string>(),
                    Framed = false,
                    VeteranOnWatch = false,
                    TimesPowerUsed = 0,
                    TimesSubPowerUsed = 0
                });
            }

            session.MafiaData.PossibleRoles = new List<MafiaRole>();
            session.MafiaData.Random = botMain.DiceBot.random;
            
            Random random = session.MafiaData.Random;

            //set pool of possible roles - keep true # first
            int totalPlayerCount = session.MafiaData.MafiaPlayers.Count();

            switch(session.MafiaData.SetupRules)
            {
                case MafiaSetupRules.Basic:
                    {
                        int randomSeed = random.Next(3);//0-2
                        if (totalPlayerCount > 12)
                            randomSeed = random.Next(4);
                        int mafiaNumber = 0;
                        switch (mafiaNumber)
                        {
                            case 0:
                            case 1:
                                mafiaNumber = 0;
                                break;
                            case 2:
                            case 3:
                                mafiaNumber = 1;
                                break;
                        }
                        mafiaNumber += totalPlayerCount / 4; //rounds down, 13 = 3 people
                        if (mafiaNumber < 1)
                            mafiaNumber = 1;

                        MafiaRole roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownSheriff);
                        session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);
                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownDoctor);
                        session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);

                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.MafiaGodfather);
                        session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);
                        mafiaNumber -= 1;

                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.MafiaMafioso);
                        for (int i = 0; i < mafiaNumber; i++)
                        {
                            session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);
                        }

                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownCitizen);
                        foreach (MafiaPlayer player in session.MafiaData.GetPlayersWithoutRoles())
                        {
                            player.Role = roleAssigned.Copy();
                        }
                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownCitizen);
                        foreach (MafiaPlayer player in session.MafiaData.GetPlayersWithoutRoles())
                        {
                            player.Role = roleAssigned.Copy();
                        }
                    }
                    break;
                case MafiaSetupRules.Custom:
                case MafiaSetupRules.Random:
                    {
                        int randomSeed = random.Next(3);//0-2
                        if (totalPlayerCount > 12)
                            randomSeed = random.Next(4);
                        int mafiaNumber = 0;
                        switch (mafiaNumber)
                        {
                            case 0:
                            case 1:
                                mafiaNumber = 0;
                                break;
                            case 2:
                            case 3:
                                mafiaNumber = 1;
                                break;
                        }
                        mafiaNumber += totalPlayerCount / 4; //rounds down, 13 = 3 people
                        if (mafiaNumber < 1)
                            mafiaNumber = 1;

                        MafiaRole roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownSheriff);
                        session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);
                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownDoctor);
                        session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);

                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.MafiaGodfather);
                        session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);
                        mafiaNumber -= 1;

                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.MafiaMafioso);
                        for (int i = 0; i < mafiaNumber; i++)
                        {
                            session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);
                        }

                        //testing here: these are not supposed to be in classic
                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownEscort);
                        session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);
                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownLookout);
                        session.MafiaData.AssignRoleToRandomUnroledPlayer(random, roleAssigned);

                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownCitizen);
                        foreach (MafiaPlayer player in session.MafiaData.GetPlayersWithoutRoles())
                        {
                            player.Role = roleAssigned.Copy();
                        }
                        roleAssigned = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.TownCitizen);
                        foreach (MafiaPlayer player in session.MafiaData.GetPlayersWithoutRoles())
                        {
                            player.Role = roleAssigned.Copy();
                        }
                    }
                    break;
            }

            session.MafiaData.PossibleRoles = session.MafiaData.MafiaPlayers.Select(a => a.Role).ToList();
            
            //select people at random for these roles
            
            //add a few extras to confuse people (for other modes)
            //if(session.MafiaData.SetupRules != MafiaSetupRules.Basic)
            //{
            //    MafiaRole newRandom = MafiaRoles.GetInstance().GetRandomMafiaRole(random, Faction.Mafia);
            //    session.MafiaData.PossibleRoles.Add(newRandom.Copy());
            //    newRandom = MafiaRoles.GetInstance().GetRandomMafiaRole(random, Faction.Town);
            //    session.MafiaData.PossibleRoles.Add(newRandom.Copy());
            //    int randomRolesAmount = (totalPlayerCount / 3) - 2;
            //    for (int i = 0; i < randomRolesAmount; i++)
            //    {
            //        newRandom = MafiaRoles.GetInstance().GetRandomMafiaRole(random, Faction.NONE);
            //        session.MafiaData.PossibleRoles.Add(newRandom.Copy());
            //    }
            //}
            
            int playerIndex = 1;
            foreach(MafiaPlayer player in session.MafiaData.MafiaPlayers)
            {
                string playerMessage = "Your role for Mafia is: " + player.Role.Print();
                if(player.Role.Faction == Faction.Mafia)
                {
                    List<MafiaPlayer> otherMafia = session.MafiaData.MafiaPlayers.Where(a => a.Role.Faction == Faction.Mafia && a.PlayerName != player.PlayerName ).ToList();
                    if (otherMafia.Count > 0)
                        playerMessage += "\nThe other " + GetFactionPrint(Faction.Mafia) + " Members on your team are: " + string.Join(", ", otherMafia.Select(a => a.Print(true, session.MafiaData.UseCaptureElimination)));
                    else
                        playerMessage += "\nThere are no other " + GetFactionPrint(Faction.Mafia) + " Members on your team.";
                }
                botMain.SendFutureMessage(playerMessage, session.ChannelId, player.PlayerName, false, 1500 * playerIndex);
                playerIndex++;
            }

            outputString = "A new game of [b]Mafia[/b] is starting...\nAssigning Roles to players.\nThe possible roles are: " + GetRolesList(session, false);

            session.State = DiceFunctions.GameState.GameInProgress;
            session.MafiaData.CurrentGamePhase = MafiaGamePhase.TrialResult;
            session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "StartGame", Utils.GetCurrentTimestampSeconds() + StartupReadingSeconds);

            return outputString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {
            List<QueuedAction> triggeredActions = session.GetTriggeredActions(currentTime);
            if(triggeredActions != null && triggeredActions.Count() > 0)
            {
                for(int i = 0; i < triggeredActions.Count(); i++ )
                {
                    QueuedAction currentAction = triggeredActions[i];
                    session.RemoveQueuedAction(currentAction);

                    switch(currentAction.QueuedActionType)
                    {
                        case QueuedActionType.AdvanceGamePhase:
                            MafiaGamePhase currentPhase = session.MafiaData.CurrentGamePhase;
                            MafiaGamePhase nextPhase = currentPhase;
                            switch(currentPhase)
                            {
                                case MafiaGamePhase.MorningNews:
                                    nextPhase = MafiaGamePhase.Day;
                                    break;
                                case MafiaGamePhase.Day:
                                    nextPhase = MafiaGamePhase.VotingNomination;
                                    break;
                                case MafiaGamePhase.VotingNomination:
                                    nextPhase = MafiaGamePhase.VotingDefense;
                                    break;
                                case MafiaGamePhase.VotingDefense:
                                    nextPhase = MafiaGamePhase.VotingExecution;
                                    break;
                                case MafiaGamePhase.VotingExecution:
                                    nextPhase = MafiaGamePhase.TrialResult;
                                    break;
                                case MafiaGamePhase.TrialResult:
                                    nextPhase = MafiaGamePhase.Night;
                                    break;
                                case MafiaGamePhase.Night:
                                    nextPhase = MafiaGamePhase.MorningNews;
                                    break;
                            }
                            if(nextPhase != currentPhase)
                            {
                                string newPhaseResult = StartNewPhase(nextPhase, botMain, session);
                                botMain.SendMessageInChannel(newPhaseResult, session.ChannelId);
                            }
                            break;
                    }
                }
            }
        }

        private string StartNewPhase(MafiaGamePhase newPhase, BotMain botMain, GameSession session)
        {
            string returnString = "";
            double currentTime = Utils.GetCurrentTimestampSeconds();
            session.MafiaData.CurrentGamePhase = newPhase;

            List<MafiaPlayer> livingPlayers = session.MafiaData.GetLivingPlayers();

            string gameFinishedString = CheckGameFinished(session, botMain.DiceBot);

            if (!string.IsNullOrEmpty(gameFinishedString) || session.State != GameState.GameInProgress)
            {
                returnString = gameFinishedString;
                return returnString;
            }

            switch(newPhase)
            {
                case MafiaGamePhase.MorningNews:
                    //resolve targeted powers from night
                    string allActions = "";
                    for (int rolePriority = MinimumRolePriority; rolePriority < MaximumRolePriority; rolePriority++)
                    {
                        List<MafiaPlayer> relevantPlayers = session.MafiaData.MafiaPlayers.Where(a => a.Role != null && a.Role.PowerResolveStep == rolePriority).ToList();

                        for (int i = 0; i < relevantPlayers.Count; i++)
                        {
                            MafiaPlayer player = relevantPlayers[i];

                            if (player.UsingPower)
                            {
                                MafiaPlayer target = GetMafiaPlayerByName(session, player.CurrentPlayerTarget);
                                string applyPowerString = ApplyPowerToPlayer(botMain, session, player, target);

                                if(!string.IsNullOrEmpty(applyPowerString))
                                {
                                    if (!string.IsNullOrEmpty(allActions))
                                        allActions += "\n";

                                    allActions += applyPowerString;
                                }
                            }
                        }
                    }
                    //after-activity notices for everyone
                    foreach(MafiaPlayer play in session.MafiaData.MafiaPlayers)
                    {
                        if(!play.Eliminated && play.UsingPower)
                        {
                            if(play.VeteranOnWatch)
                            {
                                botMain.SendPrivateMessage("Your visitors while [b]on watch[/b] tonight: " + GetVisitorsString(play), play.PlayerName);
                            }
                            if(play.Role.Id == MafiaRoles.TownLookout)
                            {
                                MafiaPlayer target = GetMafiaPlayerByName(session, play.CurrentPlayerTarget);
                                if(target != null)
                                    botMain.SendPrivateMessage("Your target, " + Utils.GetCharacterUserTags(target.PlayerName) + "'s visitors tonight: " + GetVisitorsString(target), play.PlayerName);
                            }
                        }
                        if(play.Stunned)
                        {
                            botMain.SendPrivateMessage("You were visited in the night by a very 'distracting' guest and haven't been able to use your power!", play.PlayerName);
                        }
                    }

                    returnString = "Morning (" + Utils.PrintTimeFromSeconds(MorningNewsSeconds) + "): A new morning dawns. [eicon]sunshine[/eicon]\n" +
                        (string.IsNullOrEmpty(allActions) ? "There are no gruesome deeds to report." : allActions) + "\nThe remaining people meet in town square: " + GetPlayerList(true, session, false);

                    foreach(MafiaPlayer mp in session.MafiaData.MafiaPlayers)
                    {
                        mp.ResetForNewDay();
                    }
                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "MorningEnd", currentTime + MorningNewsSeconds);
                    break;
                case MafiaGamePhase.Day: //probably never gets called
                    returnString = "Day (" + Utils.PrintTimeFromSeconds(DaytimeSeconds) + "): Discuss the recent events with the town and try to discover who's in the Mafia.";
                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "MorningEnd", currentTime + DaytimeSeconds);
                    break;
                case MafiaGamePhase.VotingNomination:
                    returnString = "Voting Nomination (" + Utils.PrintTimeFromSeconds(VotingTimeSeconds) + "): Select the player you want to vote is guilty in this channel with !gc vote PLAYER NAME";
                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + VotingTimeSeconds);
                    break;
                case MafiaGamePhase.VotingDefense:
                    //total votes and see who was nominated
                    int minimumVotes = 1;
                    // Find the MafiaPlayer with the most votes
                    var voteCounts = livingPlayers
                        .GroupBy(player => player.VoteTarget)
                        .Select(group => new { VoteTarget = group.Key, VoteCount = group.Count() + 2 * group.Count(a => a.Role.Id == MafiaRoles.TownMayor)  })
                        .OrderByDescending(vote => vote.VoteCount)
                        .ToList();

                    voteCounts.RemoveAll(a => a.VoteTarget == null);
                    
                    string accusationResult = "";
                    if (voteCounts.Count == 0 || (voteCounts.Count > 1 && voteCounts[0].VoteCount == voteCounts[1].VoteCount) || voteCounts[0].VoteCount < minimumVotes)
                    {
                        accusationResult = "No player selected due to tie or no votes.";
                        if(voteCounts.Count != 0 && voteCounts[0].VoteCount < minimumVotes)
                        {
                            accusationResult = "No player was able to meet the minimum number of votes (" + minimumVotes + ").";
                        }
                        accusationResult += " Skipping to night...";

                        returnString = accusationResult + "\n" + StartNewPhase(MafiaGamePhase.Night, botMain, session);
                    }
                    else
                    {
                        accusationResult = "The votes have been tallied. " + Utils.GetCharacterUserTags(voteCounts[0].VoteTarget) + " has been arrested and stands accused of plotting against the town.";
                        session.MafiaData.CurrentPlayerOnTrial = GetMafiaPlayerByName(session, voteCounts[0].VoteTarget);

                        returnString = accusationResult + "\n" + "Trial (" + Utils.PrintTimeFromSeconds(VotingDefenseSeconds) + "): The accused can make their defense.";
                        session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + VotingDefenseSeconds);
                    }

                    break;
                case MafiaGamePhase.VotingExecution:
                    if(session.MafiaData.CurrentPlayerOnTrial == null)
                    {
                        returnString = "Voting Execution (" + Utils.PrintTimeFromSeconds(VotingTrialSeconds) + "): No player is on trial. Skipping";
                        session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "SkipPhase", 2);
                    }
                    else
                    {
                        int minimumExecutionV = livingPlayers.Count() / 2 + 1;
                        returnString = "Voting Execution (" + Utils.PrintTimeFromSeconds(VotingTrialSeconds) + "): Vote whether you believe " + Utils.GetCharacterUserTags(session.MafiaData.CurrentPlayerOnTrial.PlayerName) + " is innocent or guilty to decide whether to execute them. It will require a total of (" + minimumExecutionV + ") votes. Use !gc guilty OR !gc innocent ";
                        session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + VotingTrialSeconds);
                    }
                    break;
                case MafiaGamePhase.TrialResult:
                    int minimumExecutionVotes = livingPlayers.Count() / 2 + 1;
                    int votesGuilty = livingPlayers.Count(a => a.TrialVote == MafiaTrialVote.Guilty);
                    int votesInnocent = livingPlayers.Count(a => a.TrialVote == MafiaTrialVote.Innocent);
                    var mayors = livingPlayers.Where(a => a.Role.Id == MafiaRoles.TownMayor).ToList();
                    foreach(MafiaPlayer playerM in mayors)
                    {
                        if (playerM.TrialVote == MafiaTrialVote.Guilty)
                            votesGuilty += 2;
                        if (playerM.TrialVote == MafiaTrialVote.Innocent)
                            votesInnocent += 2;
                    }
                    string trialResult = "";
                    string executionResult = "";
                    string votesTally = "Votes: [b]" + votesGuilty + "[/b] Guilty vs [b]" + votesInnocent + "[/b] Innocent";
                    if(votesGuilty >= minimumExecutionVotes)
                    {
                        trialResult = "[b]GUILTY[/b]! [eicon]guilty1[/eicon][eicon]guilty2[/eicon] [sub]May god have mercy on your soul.[/sub] ";
                        executionResult = "\n" + EliminatePlayer(botMain, session, session.MafiaData.CurrentPlayerOnTrial, session.MafiaData.CurrentPlayerOnTrial, true);
                    }
                    else
                    {
                        trialResult = "[b]INNOCENT[/b]! [eicon]notguilty1[/eicon][eicon]notguilty2[/eicon] [sub]You will live another day.[/sub]";
                    }

                    returnString = "Trial Result (" + Utils.PrintTimeFromSeconds(TrialResultSeconds) + "): " + votesTally + "\nThe accused has been found... " + trialResult + executionResult;
                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + TrialResultSeconds);           
                    break;
                case MafiaGamePhase.Night:
                    returnString = "Night (" + Utils.PrintTimeFromSeconds(NighttimeSeconds) + "): The town rests peacefully... [eicon]cmoon2[/eicon]";

                    foreach(MafiaPlayer player in livingPlayers)
                    {
                        int index = 1;
                        if(!player.Eliminated && player.Role.HasPowerAtNight)
                        {
                            botMain.SendFutureMessage("It is night and you can use your ability: " + player.Role.PowerDescrpition + "\nActivate your power with !usepower PLAYERNAME\nThese are the remaining players: " + GetPlayerList(true, session, false), session.ChannelId, player.PlayerName, false, WaitPerPlayerMs * index);
                            index++;
                        }
                    }

                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + NighttimeSeconds);
                    break;
            }

            return returnString;
        }

        public void MessagePlayerToSendCommand(BotMain botMain, string channel, MafiaPlayer player, int msDelay)
        {
            string powerString = "";
            if (player.Role.TargetPlayerAtNight)
                powerString = "PLAYER NAME";
            if (player.Role.Id == MafiaRoles.NeutralWitch)
                powerString = "PLAYER NAME \n then, designate the second target with !usepowersecondary PLAYER NAME";
            string message = "It is night in the town... Send your action here in private for Mafia in " + channel + " by using !usepower " + powerString;
            if(msDelay > 0)
            {
                botMain.SendFutureMessage(message, channel, player.PlayerName, false, msDelay);
            }
            else
            {
                botMain.SendPrivateMessage(message, player.PlayerName);
            }
        }

        public string ApplyPowerToPlayer(BotMain botMain, GameSession session, MafiaPlayer source, MafiaPlayer target)
        {
            MafiaRole sourceRole = source.Role;
            string resultString = "";

            if(!source.Stunned && !source.Eliminated)
            {
                if(target != null)
                {
                    target.VisitingNightPlayers.Add(source.PlayerName);
                }
                source.TimesPowerUsed += 1;

                if (target == null && source.Role.TargetPlayerAtNight)
                {
                    resultString = "Error: power activation requires a target Source: " + source.PlayerName + ". Role: " + source.Role.Print();
                }
                else if(target != null && target.VeteranOnWatch)
                {
                    resultString = EliminatePlayer(botMain, session, target, source, false);
                }
                else
                {
                    switch (sourceRole.Id)
                    {
                        case MafiaRoles.NeutralSerialKiller:
                        case MafiaRoles.MafiaGodfather:
                        case MafiaRoles.TownVigilante:
                            if (!target.Regenerate && !target.Eliminated)
                            {
                                resultString = EliminatePlayer(botMain, session, source, target, false);
                            }
                            else
                            {
                                resultString = "";
                            }
                            break;
                        case MafiaRoles.MafiaFramer: //no visible result
                            target.Framed = true;
                            break;
                        case MafiaRoles.TownDoctor: //no visible result
                            if (target.PlayerName == source.PlayerName)
                            {
                                source.TimesSubPowerUsed += 1;
                            }
                            target.Regenerate = true;
                            break;
                        case MafiaRoles.NeutralSurvivor: //no visible result
                            source.Regenerate = true;
                            break;
                        case MafiaRoles.TownSheriff: //whispered result when it happens
                            Faction factionResult = target.Role.Faction;
                            if (target.Framed)
                                factionResult = Faction.Mafia;
                            botMain.SendPrivateMessage("Your target, " + Utils.GetCharacterUserTags(target.PlayerName) + " is part of the " + GetFactionPrint(factionResult) + " faction.", source.PlayerName);
                            break;
                        case MafiaRoles.MafiaConsigliere: //whispered result when it happens
                            botMain.SendPrivateMessage("Your target, " + Utils.GetCharacterUserTags(target.PlayerName) + " has the role: " + target.Role.Print(), source.PlayerName);
                            break;
                        case MafiaRoles.TownLookout: //whispered result after
                            break;
                        case MafiaRoles.TownVeteran: //whispered result after
                            source.VeteranOnWatch = true;
                            break;
                        case MafiaRoles.MafiaConsort:
                        case MafiaRoles.TownEscort:
                            target.Stunned = true;
                            break;
                        case MafiaRoles.NeutralWitch:
                            if (!string.IsNullOrEmpty(source.SecondaryPlayerTarget) && !string.IsNullOrEmpty(source.CurrentPlayerTarget))
                            {
                                target.WasControlled = true;
                                target.CurrentPlayerTarget = source.SecondaryPlayerTarget;
                            }
                            else
                            {
                                botMain.SendPrivateMessage("[color=yellow]Notice:[/color] Your power as witch requires two targets! The secondary target was not set so it cannot function tonight.", source.PlayerName);
                            }
                            break;
                        case MafiaRoles.MafiaDisguiser: //whispered result after
                            source.DisguisedRoleId = target.Role.Id;
                            break;
                        case MafiaRoles.TownInvestigator: //whispered result when it happens
                            String result = "has no role. (not found on investigator table)";
                            int usedRoleId = target.Role.Id;
                            if (target.DisguisedRoleId > 0)
                                usedRoleId = target.DisguisedRoleId;

                            switch(target.Role.Id)
                            {
                                case MafiaRoles.TownCitizen:
                                case MafiaRoles.TownVigilante:
                                case MafiaRoles.TownVeteran:
                                case MafiaRoles.MafiaMafioso:
                                    result = "owns weapons. (Vigilante, Veteran, Citizen, Mafioso)";
                                    break;
                                case MafiaRoles.TownInvestigator:
                                case MafiaRoles.TownMayor:
                                case MafiaRoles.MafiaConsigliere:
                                    result = "has sensitive information to reveal. (Mayor, Investigator, Consigliere)";
                                    break;
                                case MafiaRoles.MafiaConsort:
                                case MafiaRoles.TownEscort:
                                case MafiaRoles.NeutralWitch:
                                    result = "is skilled at disrupting others. (Consort, Witch, Escort)";
                                    break;
                                case MafiaRoles.MafiaGodfather:
                                case MafiaRoles.NeutralSerialKiller:
                                case MafiaRoles.TownDoctor:
                                    result = "is covered in blood. (Doctor, Serial Killer, Godfather)";
                                    break;
                                case MafiaRoles.NeutralSurvivor:
                                case MafiaRoles.TownLookout:
                                case MafiaRoles.MafiaDisguiser:
                                    result = "shies away from others. (Lookout, Survivalist, Disguiser)";
                                    break;
                                case MafiaRoles.MafiaFramer:
                                case MafiaRoles.TownSheriff:
                                case MafiaRoles.NeutralJester:
                                    result = "may not be what they seem. (Sheriff, Jester, Framer, Anyone Framed)";
                                    break;
                            }
                            if (target.Framed)
                                result = "may not be what they seem. (Sheriff, Framer, Anyone Framed)";
                            botMain.SendPrivateMessage("Your target, " + Utils.GetCharacterUserTags(target.PlayerName) + " " + result, source.PlayerName);
                            break;

                    }
                }
                
            }
            else
            {
                botMain.SendPrivateMessage("You were stopped from activating your ability tonight!", source.PlayerName);
            }

            return resultString;

        }

        public string GetVisitorsString(MafiaPlayer target)
        {
            string visitation = "";
            if (target.VisitingNightPlayers.Count > 0)
            {
                foreach (string visitor in target.VisitingNightPlayers)
                {
                    if (!string.IsNullOrEmpty(visitation))
                        visitation += ", ";
                    visitation += Utils.GetCharacterUserTags(visitor);
                }
            }
            else
            {
                visitation = "no one";
            }
            return visitation;
        }

        public string EliminatePlayer(BotMain botMain, GameSession session, MafiaPlayer source, MafiaPlayer target, bool townExecution)
        {
            target.Eliminated = true;
            
            if(target.Role.Id == MafiaRoles.MafiaGodfather)
            {
                var remainingMafia = session.MafiaData.MafiaPlayers.Where(a => a.Role.Faction == Faction.Mafia && !a.Eliminated).ToList();
                if(remainingMafia.Count > 0)
                {
                    remainingMafia[0].Role = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.MafiaGodfather).Copy();

                    botMain.SendPrivateMessage("The godfather has died. You have become the new godfather. It has replaced your previous role: You now decide who to kill at night for the Mafia.", remainingMafia[0].PlayerName);
                }
            }

            List<MafiaPlayer> executionersForTarget = session.MafiaData.MafiaPlayers.Where(a => a.ExecutionTarget == target.PlayerName).ToList();

            string resultString = "";
            if(townExecution)
            {
                resultString = Utils.GetCharacterUserTags(target.PlayerName) + " was " + GetKilledString(session.MafiaData.UseCaptureElimination) + " by popular vote!";
                if (target.Role.Id == MafiaRoles.NeutralJester)
                {
                    target.AchievedEarlyVictory = true;
                    botMain.SendPrivateMessage("You have been executed and you've [b]won the game[/b], regardless of what happens in the future.", target.PlayerName);
                }

                foreach(MafiaPlayer player in executionersForTarget)
                {
                    player.AchievedEarlyVictory = true;
                    player.ExecutionTarget = null;
                    botMain.SendPrivateMessage("Your target has been executed and you've [b]won the game[/b], regardless of what happens in the future.", player.PlayerName);
                }
            }
            else
            {
                bool usingCaptureVerbs = session.MafiaData.UseCaptureElimination;
                resultString = Utils.GetCharacterUserTags(target.PlayerName) + " was found " + GetDeadString(usingCaptureVerbs) + "! They were ";
                if (source.Role.Id == MafiaRoles.MafiaGodfather || source.Role.Id == MafiaRoles.NeutralSerialKiller)
                    resultString += Mafia.GetFlavorTextForMurder(session.MafiaData.Random, target.PlayerName, usingCaptureVerbs);
                else if (source.Role.Id == MafiaRoles.TownVigilante)
                    resultString += " " + GetKilledString(usingCaptureVerbs) + " by a [color=green]vigilante[/color].";
                else if (source.Role.Id == MafiaRoles.TownVeteran)
                    resultString += " " + GetKilledString(usingCaptureVerbs) + " while visiting a [color=green]veteran[/color].";
                else
                    resultString += " " + GetKilledString(usingCaptureVerbs) + " by an unknown source.";

                foreach (MafiaPlayer player in executionersForTarget)
                {
                    MafiaRole jesterRole = MafiaRoles.GetInstance().GetMafiaRole(MafiaRoles.NeutralJester);
                    player.Role = jesterRole.Copy();
                    player.AchievedEarlyVictory = false;
                    player.ExecutionTarget = null;
                    botMain.SendPrivateMessage("Your execution target has been killed during the night. Your new role is: " + player.Role.Print(), player.PlayerName);
                }
            }

            if (session.MafiaData.RevealRolesOnDeath)
                resultString += " Their role was: " + target.Role.Print();
            return resultString;
        }

        public string ActivatePowerAtNight(BotMain botMain, string[] rawTerms, string characterName, GameSession session, bool secondaryTarget)
        {
            MafiaPlayer sourcePlayer = GetMafiaPlayerByName(session, characterName);

            //validation
            string outputString = "";
            if(sourcePlayer == null)
            {
                outputString = "Error: Your character was not found active in this channel's session of " + GetGameName() + ".";
            }
            else
            {
                MafiaRole sourceRole = sourcePlayer.Role;
                if(sourceRole == null)
                {
                    outputString = "Error: Your character does not have a valid role in " + GetGameName() + ".";
                }
                else if(!sourceRole.HasPowerAtNight)
                {
                    outputString = "Failed: Your character's role (" + sourceRole.Name + ") does not have a power to use at night in " + GetGameName() + ".";
                }
                else
                {
                    string targetName = Utils.GetUserNameFromFullInputs(rawTerms);
                    MafiaPlayer targetPlayer = GetMafiaPlayerByName(session, targetName);

                    if (sourceRole.TargetPlayerAtNight && targetPlayer == null)
                    {
                        outputString = "Failed: Your target, '" + targetName + "', was not found";
                    }
                    else
                    {
                        sourcePlayer.UsingPower = true;
                        sourcePlayer.CurrentPlayerTarget = targetPlayer.PlayerName;
                        outputString = "Your target for your power was successfully set to '" + targetName + "'.";
                    }
                }
            }
            return outputString;
        }
        
        public static string GetFactionPrint(Faction faction)
        {
            string rtn = "";
            string color = GetFactionColor(faction);
            switch(faction)
            {
                case Faction.Town:
                    rtn = "[color=" + color + "]Town[/color]";
                    break;
                case Faction.Mafia:
                    rtn = "[color=" + color + "]Mafia[/color]";
                    break;
                case Faction.NeutralHostile:
                    rtn = "[color=" + color + "]Neutral Hostile[/color]";
                    break;
                case Faction.Neutral:
                    rtn = "[color=" + color + "]Neutral[/color]";
                    break;
                case Faction.Vampire:
                    rtn = "[color=" + color + "]Vampire[/color]";
                    break;
                case Faction.NONE:
                    rtn = "[color=" + color + "]NONE (error)[/color]";
                    break;
            }
            return rtn;
        }

        public static string GetFactionColor(Faction faction)
        {
            string color = "green";
            switch(faction)
            {
                case DiceFunctions.Faction.Town:
                    color = "green";
                    break;
                case DiceFunctions.Faction.Mafia:
                    color = "brown";
                    break;
                case DiceFunctions.Faction.NeutralHostile:
                    color = "red";
                    break;
                case DiceFunctions.Faction.Neutral:
                    color = "gray";
                    break;
                case DiceFunctions.Faction.Vampire:
                    color = "pink";
                    break;
            }
            return color;
        }

        public static string GetDeadString(bool useCaptureString)
        {
            return useCaptureString ? "[color=red]Captured[/color]" : "[color=red]Dead[/color]";
        }

        public static string GetKilledString(bool useCaptureString)
        {
            return useCaptureString ? "captured" : "killed";
        }

        public static string GetFlavorTextForMurder(System.Random random, string murderedPlayerName, bool usingChastityString)
        {
            string flavorText = "";

            int flavorRoll = random.Next(16);
            switch (flavorRoll)
            {
                case 0:
                    flavorText = usingChastityString? "wrapped up in a perfect mummy costume." : "riddled with bullets.";
                    break;
                case 1:
                    flavorText = usingChastityString ? "chained to the wall." : "pushed down an elevator shaft.";
                    break;
                case 2:
                    flavorText = usingChastityString ? "talked into it." : "beaten to death with a club.";
                    break;
                case 3:
                    flavorText = usingChastityString ? "kidnapped and taken to the dungeon." : "beaten to death with a cane.";
                    break;
                case 4:
                    flavorText = usingChastityString ? "wrapped up tight in leather straps." : "stabbed several times with a knife.";
                    break;
                case 5:
                    flavorText = usingChastityString ? "shackled and collared." : "decapitated by a sword.";
                    break;
                case 6:
                    flavorText = usingChastityString ? "put in a dog cage." : "run over by a car.";
                    break;
                case 7:
                    flavorText = usingChastityString ? "hypnotized and docile." : "poisoned in their evening tea.";
                    break;
                case 8:
                    flavorText = usingChastityString ? "drugged and carried off." : "crushed by a falling piano.";
                    break;
                case 9:
                    flavorText = usingChastityString ? "locked in a latex gimp suit." : "burned to death in a house fire.";
                    break;
                case 10:
                    flavorText = usingChastityString ? "bound and gagged." : "drowned in a pool.";
                    break;
                case 11:
                    flavorText = usingChastityString ? "sealed in a vacuum bed." : "mauled by a wild animal.";
                    break;
                case 12:
                    flavorText = usingChastityString ? "drugged and carried off." : "shot through the heart by an arrow.";
                    break;
                case 13:
                    flavorText = usingChastityString ? "locked in a cute little cage." : "suffocated by a pillow.";
                    break;
                case 14:
                    flavorText = usingChastityString ? "given a free shibari lesson." : "blown to bits by an explosion.";
                    break;
                case 15:
                    flavorText = usingChastityString ? "vibed till they couldn't see straight." : "given a lethal injection.";
                    break;
            }

            return flavorText;
        }
        
        private bool GameIsComplete(GameSession session)
        {
            return session.RockPaperScissorsData.RockPaperScissorsPlayers.Count(a => a.PlayerLives > 0) <= 1;
        }

        private string FinishGame(Faction victoryFaction, GameSession session, DiceBot diceBot)
        {
            string returnString = "";

            returnString += "[b]Mafia[/b] game finished! " + GetFactionPrint(victoryFaction) + " has won! [eicon]confetti[/eicon]";

            foreach(MafiaPlayer player in session.MafiaData.MafiaPlayers)
            {
                if(victoryFaction == player.Role.Faction)
                {
                    bool victory = true;
                    if ((victoryFaction == Faction.Neutral || victoryFaction == Faction.NeutralHostile) && player.Eliminated)
                        victory = false;

                    player.WonGame = victory;
                }
                if (player.AchievedEarlyVictory)
                    player.WonGame = true;
                else if (player.Role.Id == MafiaRoles.NeutralJester || player.Role.Id == MafiaRoles.NeutralExecutioner)
                    player.WonGame = false;
                else if (player.Role.Id == MafiaRoles.NeutralWitch && victoryFaction != Faction.Town && !player.Eliminated)
                    player.WonGame = true;
                else if (player.Role.Id == MafiaRoles.NeutralSurvivor && !player.Eliminated)
                    player.WonGame = true;
            }

            List<MafiaPlayer> winners = session.MafiaData.MafiaPlayers.Where(a => a.WonGame).ToList();

            string winnersString = string.Join(", ", winners.Select(a => a.Print(true, session.MafiaData.UseCaptureElimination)));

            session.State = DiceFunctions.GameState.Finished;

            return returnString + "\nGame Winners: " + winnersString;
        }

        private string GetPlayerList(bool onlyLiving, GameSession session, bool revealRole, bool newLines = false)
        {
            string rtn = "";
            List<MafiaPlayer> relevantPlayers = session.MafiaData.MafiaPlayers;
            if(onlyLiving)
                relevantPlayers = session.MafiaData.MafiaPlayers.Where(a => !a.Eliminated).ToList();

            foreach (MafiaPlayer p in relevantPlayers)
            {
                if (!string.IsNullOrEmpty(rtn))
                {
                    if (newLines)
                        rtn += "\n";
                    else
                        rtn += ", ";
                }

                rtn += p.Print(revealRole, session.MafiaData.UseCaptureElimination);
            }
            return rtn;
        }

        private string GetRolesList(GameSession session, bool newLines = false)
        {
            string rtn = "";
            foreach (MafiaRole p in session.MafiaData.PossibleRoles)
            {
                if (!string.IsNullOrEmpty(rtn))
                {
                    if (newLines)
                        rtn += "\n";
                    else
                        rtn += ", ";
                }

                rtn += p.Print();
            }
            return rtn;
        }

        public string CheckGameFinished(GameSession session, DiceBot diceBot)
        {
            string returnString = "";
            Faction victoryFaction = Faction.NONE;

            if(session.MafiaData.MafiaPlayers == null || session.MafiaData.MafiaPlayers.Count == 0 || session.State != GameState.GameInProgress)
            {
                return returnString;
            }

            int totalPlayerCount = session.MafiaData.MafiaPlayers.Count(a => !a.Eliminated);
            int mafiaCount = session.MafiaData.MafiaPlayers.Count( a => !a.Eliminated && a.Role.Faction == Faction.Mafia);
            int townCount = session.MafiaData.MafiaPlayers.Count( a => !a.Eliminated && a.Role.Faction == Faction.Town);
            int neutralCount = session.MafiaData.MafiaPlayers.Count(a => !a.Eliminated && a.Role.Faction == Faction.Neutral);
            int neutralHostileCount = session.MafiaData.MafiaPlayers.Count(a => !a.Eliminated && a.Role.Faction == Faction.NeutralHostile);
            int witchCount = session.MafiaData.MafiaPlayers.Count(a => !a.Eliminated && a.Role.Id == MafiaRoles.NeutralWitch);
            //check town victory : mafia dead
            if(mafiaCount == 0 && neutralHostileCount == 0)
            {
                victoryFaction = Faction.Town;
            }
            //check mafia victory : town + neutral < mafia && neutralhostile = 0
            else if (mafiaCount >= townCount + neutralCount && neutralHostileCount <= witchCount)
            {
                victoryFaction = Faction.Mafia;
            }
            else if (neutralHostileCount >= townCount && mafiaCount == 0 && totalPlayerCount <= 2)//just handles SK victory
            {
                victoryFaction = Faction.NeutralHostile;
            }

            if(victoryFaction != Faction.NONE)
            {
                returnString = FinishGame(victoryFaction, session, diceBot);
            }

            return returnString;
        }

        public MafiaPlayer GetMafiaPlayerByName(GameSession session, string name)
        {
            return session.MafiaData.MafiaPlayers.FirstOrDefault(a => a.PlayerName.ToLower() == name.ToLower());
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            string returnString = "";
            if (terms.Contains("showplayers"))
            {
                string playerList = GetPlayerList(false, session, false);

                returnString = playerList;
            }
            if (terms.Contains("showroles"))
            {
                string playerList = GetRolesList(session, false);

                returnString = playerList;
            }
            else if (terms.Contains("debugprintplayers"))
            {
                string playerList = GetPlayerList(false, session, true, true);

                returnString = playerList;
            }
            else if (terms.Contains("debugvoteguilty"))
            {
                if (session.MafiaData.CurrentGamePhase != MafiaGamePhase.VotingExecution)
                {
                    returnString = "Failed: It is not the VotingExecution phase.";
                }
                else
                {
                    string[] remainingTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, "innocent");
                    remainingTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingTerms, "guilty");
                    remainingTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingTerms, "debugvoteguilty");

                    string nominatedName = Utils.GetUserNameFromFullInputs(remainingTerms);

                    MafiaPlayer forcedPlayer = GetMafiaPlayerByName(session, nominatedName);
                    if (forcedPlayer == null)
                    {
                        returnString = "Failed: " + Utils.GetCharacterUserTags(nominatedName) + " was not found as a valid player to vote for.";
                    }
                    else if (forcedPlayer == null)
                    {
                        returnString = "Failed: " + Utils.GetCharacterUserTags(nominatedName) + " was not found in the list of players for Mafia and cannot vote.";
                    }
                    else if (forcedPlayer.Eliminated)
                    {
                        returnString = "Failed: " + Utils.GetCharacterUserTags(nominatedName) + " is " + Mafia.GetDeadString(session.MafiaData.UseCaptureElimination) + " and cannot vote.";
                    }
                    else
                    {
                        string accusationString = "";
                        MafiaTrialVote oldVote = forcedPlayer.TrialVote;
                        MafiaTrialVote vote = MafiaTrialVote.NONE;
                        if (terms.Contains("innocent"))
                        {
                            vote = MafiaTrialVote.Innocent;
                            accusationString = "[b]innocent[/b]";
                        }
                        if (terms.Contains("guilty"))
                        {
                            vote = MafiaTrialVote.Guilty;
                            accusationString = "[b]guilty[/b]";
                        }
                        if (terms.Contains("abstain"))
                        {
                            vote = MafiaTrialVote.NONE;
                            accusationString = "[b]abstaining[/b]";
                        }

                        if (vote != oldVote)
                        {
                            if (oldVote != MafiaTrialVote.NONE)
                                accusationString = "changed their vote to " + accusationString + "!";
                            else
                                accusationString = "has voted " + accusationString + "!";
                        }
                        else
                            accusationString = "has not changed their vote. (already " + accusationString + ")";

                        forcedPlayer.TrialVote = vote;

                        returnString = Utils.GetCharacterUserTags(forcedPlayer.PlayerName) + " " + accusationString;
                    }
                }
            }
            else if (terms.Contains("roleslist") || terms.Contains("showroles"))
            {
                string playerList = session.MafiaData.PrintPossibleRoles();

                returnString = playerList;
            }
            else if (terms.Contains("endphase"))
            {
                if(session.QueuedActions != null && session.QueuedActions.Count() > 0)
                {
                    double currentTime = Utils.GetCurrentTimestampSeconds();
                    foreach(QueuedAction action in session.QueuedActions)
                    {
                        action.TriggerTime = currentTime;
                    }
                    returnString = "You have skipped to the end of this phase (" + session.MafiaData.CurrentGamePhase + ").";
                }
                else
                {
                    returnString = "Error: no pending phase change found to skip to!";
                }
            }
            else if (terms.Contains("vote"))
            {
                MafiaPlayer sourcePlayer = GetMafiaPlayerByName(session, character);

                if(session.MafiaData.CurrentGamePhase != MafiaGamePhase.VotingNomination)
                {
                    returnString = "Failed: It is not the voting nomination phase.";
                }
                else if(sourcePlayer == null)
                {
                    returnString = "Failed: " + Utils.GetCharacterUserTags(character) + " was not found in the list of players for Mafia and cannot vote.";
                }
                else if (sourcePlayer.Eliminated)
                {
                    returnString = "Failed: " + Utils.GetCharacterUserTags(character) + " is " + Mafia.GetDeadString(session.MafiaData.UseCaptureElimination) + " and cannot vote.";
                }
                else
                {
                    string[] remainingTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, "vote");

                    string nominatedName = Utils.GetUserNameFromFullInputs(remainingTerms);

                    MafiaPlayer nominatedPlayer = GetMafiaPlayerByName(session, nominatedName);
                    if(nominatedPlayer == null)
                    {
                        returnString = "Failed: " + Utils.GetCharacterUserTags(nominatedName) + " was not found as a valid player to vote for.";
                    }
                    else
                    {
                        string accusationString = "";
                        if(string.IsNullOrEmpty(sourcePlayer.VoteTarget))
                        {
                            accusationString = Utils.GetCharacterUserTags(character) + " has voted for " + Utils.GetCharacterUserTags(nominatedName) + "!";
                            returnString = accusationString;
                            sourcePlayer.VoteTarget = nominatedName;
                        }
                        else if(sourcePlayer.VoteTarget != nominatedName)
                        {
                            accusationString = Utils.GetCharacterUserTags(character) + " has changed their vote to " + Utils.GetCharacterUserTags(nominatedName) + "!";
                            returnString = accusationString;
                            sourcePlayer.VoteTarget = nominatedName;
                        }
                        else
                        {
                            returnString = "Failed: You have already voted for this player.";
                        }
                    }
                }

            }
            else if (terms.Contains("innocent") || terms.Contains("guilty") || terms.Contains("abstain"))
            {
                MafiaPlayer sourcePlayer = GetMafiaPlayerByName(session, character);

                if (session.MafiaData.CurrentGamePhase != MafiaGamePhase.VotingExecution)
                {
                    returnString = "Failed: It is not the voting trial phase.";
                }
                else if (sourcePlayer == null)
                {
                    returnString = "Failed: " + Utils.GetCharacterUserTags(character) + " was not found in the list of players for Mafia and cannot vote.";
                }
                else if (sourcePlayer.Eliminated)
                {
                    returnString = "Failed: " + Utils.GetCharacterUserTags(character) + " is " + Mafia.GetDeadString(session.MafiaData.UseCaptureElimination) + " and cannot vote.";
                }
                else
                {
                    string accusationString = "";
                    MafiaTrialVote oldVote = sourcePlayer.TrialVote;
                    MafiaTrialVote vote = MafiaTrialVote.NONE;
                    if (terms.Contains("innocent"))
                    {
                        vote = MafiaTrialVote.Innocent;
                        accusationString = "[b]innocent[/b]";
                    }
                    if (terms.Contains("guilty"))
                    {
                        vote = MafiaTrialVote.Guilty;
                        accusationString = "[b]guilty[/b]";
                    }
                    if (terms.Contains("abstain"))
                    {
                        vote = MafiaTrialVote.NONE;
                        accusationString = "[b]abstaining[/b]";
                    }

                    if (vote != oldVote)
                    {
                        if (oldVote != MafiaTrialVote.NONE)
                            accusationString = "changed their vote to " + accusationString + "!";
                        else
                            accusationString = "has voted " + accusationString + "!";
                    }
                    else
                        accusationString = "has not changed their vote. (already " + accusationString + ")";

                    sourcePlayer.TrialVote = vote;

                    returnString = Utils.GetCharacterUserTags(sourcePlayer.PlayerName) + " " + accusationString;
                }
            }
            else if (terms.Contains("setroles") || terms.Contains("roles") || terms.Contains("setrules") || terms.Contains("setmode") )
            {
                string rolesString = "";
                MafiaSetupRules newRules = MafiaSetupRules.NONE;
                if (terms.Contains("basicroles"))
                {
                    newRules = MafiaSetupRules.Basic;
                    rolesString = "(basic roles)";
                }
                if (terms.Contains("customroles"))
                {
                    newRules = MafiaSetupRules.Custom;
                    rolesString = "(custom roles)";
                }
                if (terms.Contains("randomroles"))
                {
                    newRules = MafiaSetupRules.Custom;
                    rolesString = "(random roles)";
                }

                if(newRules == MafiaSetupRules.NONE)
                {
                    returnString = "Failed: no rules found to change to.";
                }
                else if (session.State != GameState.Unstarted && session.State != GameState.Finished)
                {
                    returnString = "Failed: cannot change game rules of a game in progress.";
                }
                else
                {
                    session.MafiaData.SetupRules = newRules;
                    returnString = "Rules set: " + rolesString;
                }
            }
            else if (terms.Contains("setrevealroleondeath") || terms.Contains("setrevealdeath") || terms.Contains("setdeathreveal")
                || terms.Contains("capturedeath") || terms.Contains("capture"))
            {
                if (terms.Length < 2)
                {
                    returnString = "Error: improper command format. Use 'setallowtokens (on) or (off)'.";
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
                        if (terms.Contains("setrevealroleondeath") || terms.Contains("setrevealdeath") || terms.Contains("setdeathreveal"))
                        {
                            session.MafiaData.RevealRolesOnDeath = setValue;
                            returnString = "'Reveal Roles on Death' rule set to was set to " + (setValue ? "ON" : "OFF");
                        }
                        if (terms.Contains("capturedeath") || terms.Contains("capture"))
                        {
                            session.MafiaData.UseCaptureElimination = setValue;
                            returnString = "'Capture Deaths' rule set to was set to " + (setValue ? "ON" : "OFF");
                        }
                    }
                    else
                    {
                        returnString = "Error: Input was invalid. Value must be set to on/ true, or off/ false";
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

    public class MafiaData
    {
        public Random Random = new Random();
        public bool RulesSet;
        public int DayNumber;
        public MafiaGamePhase CurrentGamePhase;
        public List<MafiaPlayer> MafiaPlayers = new List<MafiaPlayer>();

        public List<MafiaRole> PossibleRoles = new List<MafiaRole>();

        public MafiaSetupRules SetupRules = MafiaSetupRules.Basic;
        public bool RevealRolesOnDeath = false;
        public bool UseCaptureElimination = true;

        public List<MafiaRole> ThisSessionAvailableRoles = new List<MafiaRole>();

        public MafiaPlayer CurrentPlayerOnTrial;

        public List<MafiaPlayer> GetLivingPlayers()
        {
            return MafiaPlayers.Where(a => !a.Eliminated).ToList();
        }

        public List<MafiaPlayer> GetPlayersWithoutRoles()
        {
            return MafiaPlayers.Where(a => a.Role == null).ToList();
        }

        public void AssignRoleToRandomUnroledPlayer(System.Random random, MafiaRole role)
        {
            List<MafiaPlayer> remainingPlayers = GetPlayersWithoutRoles();
            MafiaPlayer selectedPlayer = remainingPlayers[random.Next(remainingPlayers.Count)];
            selectedPlayer.Role = role.Copy();
        }

        public string PrintPossibleRoles()
        {
            if(PossibleRoles == null || PossibleRoles.Count == 0)
            {
                return "no possible roles assigned";
            }
            return string.Join(", ", PossibleRoles.Select(a => a.PrintName()));
        }
    }

    public class MafiaPlayer 
    {
        public MafiaRole Role;

        public string CurrentPlayerTarget;
        public string SecondaryPlayerTarget;

        public string PlayerName;

        public bool UsingPower;
        //Stun, Regenerate (doctor), Dead, Cursed, Wounded(?)
        public bool Eliminated;
        public bool Stunned;
        public bool Regenerate;
        public bool Framed;
        public bool VeteranOnWatch;
        public bool WasControlled;
        public List<string> VisitingNightPlayers;

        public int TimesPowerUsed = 0;
        public int TimesSubPowerUsed = 0;

        public int DisguisedRoleId = 0;
        public string VoteTarget;
        public MafiaTrialVote TrialVote = MafiaTrialVote.NONE;

        public bool WonGame;
        public bool AchievedEarlyVictory;
        public string ExecutionTarget;
        

        public void ResetForNewDay()
        {
            UsingPower = false;
            CurrentPlayerTarget = null;
            SecondaryPlayerTarget = null;
            Stunned = false;
            Regenerate = false;
            Framed = false;
            VeteranOnWatch = false;
            VisitingNightPlayers = new List<string>();
            DisguisedRoleId = 0;
            WasControlled = false;
            VoteTarget = null;
            TrialVote = MafiaTrialVote.NONE;

            //Eliminated and TimesPowerUsed not reset
        }

        public string Print(bool revealRole, bool showChastitydeaths)
        {
            string roleStr =  revealRole? " (" + Role.Name + ")" : "";
            string voteStr = "";
            return PlayerName + roleStr + (Eliminated ? " (" + Mafia.GetDeadString(showChastitydeaths) + ")" : "") + voteStr;
        }
    }

    public class MafiaRole
    {
        public int Id;
        public string Name;
        public string RoleDescription;
        public string PowerDescrpition;
        public int PowerResolveStep; //self activation = 1 (veteran, survivalist), pre-investigate = 2-4 (doctor, escort), investigate = 7-9 (sheriff), murder = 10 (godfather, serial killer), aftermurder up to 15 (lookout)
        public bool HasPowerAtNight;
        public bool TargetPlayerAtNight;
        public bool CanRevealDuringDay;
        public Faction Faction;
        public string Eicon;

        public string Print()
        {
            return "[eicon]" + Eicon +"[/eicon] [b]" + Name + "[/b] - " + Mafia.GetFactionPrint(Faction) + ": " + RoleDescription + " [b]Power:[/b] " + PowerDescrpition;
        }
        public string PrintName()
        {
            string color = Mafia.GetFactionColor(Faction);

            return "[b][color=" + color + "]" + Name + "[/color][/b]";
        }

        public MafiaRole Copy()
        {
            return new MafiaRole()
            {
                Id = this.Id,
                Name = this.Name,
                RoleDescription = this.RoleDescription,
                PowerDescrpition = this.PowerDescrpition,
                PowerResolveStep = this.PowerResolveStep,
                HasPowerAtNight = this.HasPowerAtNight,
                TargetPlayerAtNight = this.TargetPlayerAtNight,
                CanRevealDuringDay = this.CanRevealDuringDay,
                Faction = this.Faction,
                Eicon = this.Eicon
            };
        }
    }

    public enum Faction
    {
        NONE,
        Town,
        Mafia,
        Neutral,
        NeutralHostile,
        Vampire
    }

    public enum MafiaGamePhase
    {
        MorningNews,
        Day,
        VotingNomination,
        VotingDefense,
        VotingExecution,
        TrialResult,
        Night
    }

    public enum MafiaSetupRules
    {
        NONE,
        Basic,
        Custom,
        Random
    }

    public enum MafiaTrialVote
    {
        NONE,
        Guilty,
        Innocent
    }

    public class MafiaRoles
    {
        public const int MafiaGodfather = 1, MafiaMafioso = 2, MafiaFramer = 3, MafiaConsigliere = 4, MafiaConsort = 5, MafiaDisguiser = 6;
        public const int TownCitizen = 101, TownDoctor = 102, TownSheriff = 103, TownLookout = 104, TownVeteran = 105, 
            TownEscort = 106, TownInvestigator = 107, TownVigilante = 108, TownMayor = 109;
        public const int NeutralSerialKiller = 201, NeutralSurvivor = 202, NeutralWitch = 203, NeutralJester = 204, NeutralExecutioner = 205;

        public List<MafiaRole> Roles;

        private static MafiaRoles Instance;
        public static MafiaRoles GetInstance()
        {
            if (Instance == null)
                Instance = new MafiaRoles();

            return Instance;
        }

        public MafiaRoles()
        {
            PopulateData();
        }

        private void PopulateData()
        {
            Roles = new List<MafiaRole>();
            Roles.Add(new MafiaRole()
            {
                Id = MafiaGodfather,
                Name = "Godfather",
                Eicon = "lolimafia1",
                Faction = DiceFunctions.Faction.Mafia,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 10,
                PowerDescrpition = "Decide someone to kill.",
                RoleDescription = "The Godfather is the leader of the " + Mafia.GetFactionPrint(Faction.Mafia) + " and decides who will be killed each night. Wins with the " + Mafia.GetFactionPrint(Faction.Mafia) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = MafiaMafioso,
                Name = "Mafioso",
                Eicon = "lolimafiacleansgun",
                Faction = DiceFunctions.Faction.Mafia,
                CanRevealDuringDay = false,
                HasPowerAtNight = false,
                PowerDescrpition = "No power.",
                RoleDescription = Mafia.GetFactionPrint(Faction.Mafia) + " members will become Godfather if the Godfather is killed. Wins with the " + Mafia.GetFactionPrint(Faction.Mafia) + ".",
                TargetPlayerAtNight = false
            });

            Roles.Add(new MafiaRole()
            {
                Id = MafiaFramer,
                Name = "Framer",
                Eicon = "lolimafiacleansgun",
                Faction = DiceFunctions.Faction.Mafia,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 4,
                PowerDescrpition = "Decide someone to make appear suspicious.",
                RoleDescription = "The framer helps the " + Mafia.GetFactionPrint(Faction.Mafia) + " by giving " + Mafia.GetFactionPrint(Faction.Town) + " investigators misleading information. Wins with the " + Mafia.GetFactionPrint(Faction.Mafia) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = MafiaConsigliere,
                Name = "Consigliere",
                Eicon = "lolimafiacleansgun",
                Faction = DiceFunctions.Faction.Mafia,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 8,
                PowerDescrpition = "Decide someone to discover their role.",
                RoleDescription = "The consigliere helps the " + Mafia.GetFactionPrint(Faction.Mafia) + " by discovering the roles of players. Wins with the " + Mafia.GetFactionPrint(Faction.Mafia) + ".",
                TargetPlayerAtNight = true
            });
            
            Roles.Add(new MafiaRole()
            {
                Id = MafiaDisguiser,
                Name = "Disguiser",
                Eicon = "lolimafiacleansgun",
                Faction = DiceFunctions.Faction.Mafia,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 4,
                PowerDescrpition = "Decide someone (active or eliminated) to appear to have the same role (but not faction) as.",
                RoleDescription = "The disguiser helps the " + Mafia.GetFactionPrint(Faction.Mafia) + " by pretending to be other roles of active or eliminated players. Wins with the " + Mafia.GetFactionPrint(Faction.Mafia) + ".",
                TargetPlayerAtNight = true
            });
            
            Roles.Add(new MafiaRole()
            {
                Id = MafiaConsort,
                Name = "Consort",
                Eicon = "strip",
                Faction = DiceFunctions.Faction.Mafia,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 3,
                PowerDescrpition = "Decide a player to distract for the night, negating their power.",
                RoleDescription = "The Escort can choose someone to distract each night and prevent from using their ability. Wins with the " + Mafia.GetFactionPrint(Faction.Mafia) + ".",
                TargetPlayerAtNight = true
            });
            
            Roles.Add(new MafiaRole()
            {
                Id = TownCitizen,
                Name = "Citizen",
                Eicon = "workerlynn",
                Faction = DiceFunctions.Faction.Town,
                CanRevealDuringDay = false,
                HasPowerAtNight = false,
                PowerDescrpition = "No Power.",
                RoleDescription = "A citizen is the main role in the town. Citizens have no special powers, but their vote is very important in executing the " + Mafia.GetFactionPrint(Faction.Mafia) + ". Wins with the " + Mafia.GetFactionPrint(Faction.Town) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = TownSheriff,
                Name = "Sheriff",
                Eicon = "cop-salute",
                Faction = DiceFunctions.Faction.Town,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 8,
                PowerDescrpition = "Decide someone to investigate at night to discover if they are part of the Mafia.",
                RoleDescription = "The sheriff investigates people and discovers what faction they belong to at night. Wins with the " + Mafia.GetFactionPrint(Faction.Town) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = TownDoctor,
                Name = "Doctor",
                Eicon = "beebs nurse peace",
                Faction = DiceFunctions.Faction.Town,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 4,
                PowerDescrpition = "Decide someone to save from being murdered at night.",
                RoleDescription = "The doctor protects one person from being murdered at night. They can only target themself once. Wins with the " + Mafia.GetFactionPrint(Faction.Town) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = TownLookout,
                Name = "Lookout",
                Eicon = "nicopanic",
                Faction = DiceFunctions.Faction.Town,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 15,
                PowerDescrpition = "Decide someone to watch at night to see who visits them.",
                RoleDescription = "The lookout watches one person at night to see who visits them. Wins with the " + Mafia.GetFactionPrint(Faction.Town) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = TownVeteran,
                Name = "Veteran",
                Eicon = "nicopanic",
                Faction = DiceFunctions.Faction.Town,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 1,
                PowerDescrpition = "Decide a night to go on watch, killing everyone who visits you.",
                RoleDescription = "The Veteran can choose to go on watch at night up to 3 nights. They will kill any visitors they get. Wins with the " + Mafia.GetFactionPrint(Faction.Town) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = TownEscort,
                Name = "Escort",
                Eicon = "britwork1",
                Faction = DiceFunctions.Faction.Town,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 3,
                PowerDescrpition = "Decide a player to distract for the night, negating their power.",
                RoleDescription = "The Escort can choose someone to distract each night and prevent from using their ability. Wins with the " + Mafia.GetFactionPrint(Faction.Town) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = TownInvestigator,
                Name = "Investigator",
                Eicon = "nicopanic",
                Faction = DiceFunctions.Faction.Town,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 8,
                PowerDescrpition = "Decide a player to investigate each night, gaining a hint at their role.",
                RoleDescription = "The Investigator can choose one player to investigate each night, which grants a hint to what role they have. Wins with the " + Mafia.GetFactionPrint(Faction.Town) + ".",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = TownVigilante,
                Name = "Vigilante",
                Eicon = "nicopanic",
                Faction = DiceFunctions.Faction.Town,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 11,
                PowerDescrpition = "Decide a player to kill at night. (limited uses)",
                RoleDescription = "The Vigilante takes law into their own hands by deciding someone to kill at night. They have up to 3 bullets to use. Wins with the " + Mafia.GetFactionPrint(Faction.Town) + ".",
                TargetPlayerAtNight = true
            });

            //TownLookout = 104, Veteran = 105, 
            // = 106,  = 107,  = 108;

            Roles.Add(new MafiaRole()
            {
                Id = NeutralSerialKiller,
                Name = "Serial Killer",
                Eicon = "murderous_3",
                Faction = DiceFunctions.Faction.NeutralHostile,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 10,
                PowerDescrpition = "Decide someone to kill.",
                RoleDescription = "The Serial Killer murders people at night, and has no faction or allies. Wins when they are one of the last two people alive.",
                TargetPlayerAtNight = true
            });

            Roles.Add(new MafiaRole()
            {
                Id = NeutralSurvivor,
                Name = "Suvivor",
                Eicon = "nicopanic",
                Faction = DiceFunctions.Faction.Neutral,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 1,
                PowerDescrpition = "Decide whether to wear a bulletproof vest.",
                RoleDescription = "The survivalist only wishes to survive. They can don a bulletproof vest at night to avoid being killed up to 4 times. Wins by being alive with the " + Mafia.GetFactionPrint(Faction.Town) + " or the " + Mafia.GetFactionPrint(Faction.Mafia) + " at the end of the game.",
                TargetPlayerAtNight = false
            });

            Roles.Add(new MafiaRole()
            {
                Id = NeutralWitch,
                Name = "Witch",
                Eicon = "nicopanic",
                Faction = DiceFunctions.Faction.NeutralHostile,
                CanRevealDuringDay = false,
                HasPowerAtNight = true,
                PowerResolveStep = 2,
                PowerDescrpition = "Decide who to control by picking a target for their action.",
                RoleDescription = "The witch controls others. She can pick any target and choose their night action for them, although she doesn't know what their role is. Wins when they survive and the " + Mafia.GetFactionPrint(Faction.Town) + " loses.",
                TargetPlayerAtNight = false
            });

            Roles.Add(new MafiaRole()
            {
                Id = NeutralJester,
                Name = "Jester",
                Eicon = "nicopanic",
                Faction = DiceFunctions.Faction.Neutral,
                CanRevealDuringDay = false,
                HasPowerAtNight = false,
                PowerResolveStep = 0,
                PowerDescrpition = "None.",
                RoleDescription = "The jester wants to be executed. Wins if (and only if) they are executed by vote during the day.",
                TargetPlayerAtNight = false
            });

            Roles.Add(new MafiaRole()
            {
                Id = NeutralExecutioner,
                Name = "Executioner",
                Eicon = "nicopanic",
                Faction = DiceFunctions.Faction.Neutral,
                CanRevealDuringDay = false,
                HasPowerAtNight = false,
                PowerResolveStep = 0,
                PowerDescrpition = "None.",
                RoleDescription = "The executioner wants a certain person to die. Wins if their target is executed by vote during the day. If their target dies at night, the executioner becomes a jester.",
                TargetPlayerAtNight = false
            });
        }

        public MafiaRole GetMafiaRole(int id)
        {
            return Roles.FirstOrDefault(a => a.Id == id);
        }

        public MafiaRole GetRandomMafiaRole(System.Random random, Faction selectedFaction)
        {
            if (Roles == null || Roles.Count == 0)
                return null;

            if(selectedFaction  == Faction.NONE)
            {
                return Roles[random.Next(Roles.Count)];
            }
            else
            {
                List<MafiaRole> relevantRoles = Roles.Where(a => a.Faction == selectedFaction).ToList();

                return relevantRoles[random.Next(relevantRoles.Count)];
            }
        }
    }
}
