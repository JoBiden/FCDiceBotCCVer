using Discord;
using Discord.WebSocket;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FChatDicebot
{
    public class Utils
    {
        public static WebRequest CreateWebRequest(string url, System.Object saveReq, string method = "POST", bool xWwwForm = false)
        {
            WebRequest request = WebRequest.Create(url);

            request.Timeout = 20000;
            request.Method = method;

            if (method == "POST")
            {
                Console.WriteLine("method POST");
                if(xWwwForm)
                {
                    request.ContentType = "application/x-www-form-urlencoded";

                    string strNew = saveReq.ToString();
                    using (StreamWriter stOut = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII))
                    {
                        stOut.Write(strNew);
                        stOut.Close();
                    }
                }
                else
                {
                    request.ContentType = "application/json";

                    string jsonString = "";
                    try
                    {
                        jsonString = JsonConvert.SerializeObject(saveReq);
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("error occured on jsonconvert: " + exc.ToString());
                    }
                    byte[] bytes = Encoding.ASCII.GetBytes(jsonString);

                    Stream os = null;
                    try
                    {
                        request.ContentLength = bytes.Length;
                        os = request.GetRequestStream();
                        os.Write(bytes, 0, bytes.Length);
                        os.Close();
                        Console.WriteLine("wrote stream " + jsonString);
                    }
                    catch (WebException ex)
                    {
                        Console.WriteLine("error occred on content write " + ex.ToString());
                    }
                }
            }


            return request;
        }

        public static int Clamp(int input, int min, int max)
        {
            return Math.Max(Math.Min(input, max), min);
        }

        public static float Clamp(float input, float min, float max)
        {
            return Math.Max(Math.Min(input, max), min);
        }

        public static string PrintList(string[] stringArray)
        {
            if (stringArray == null || stringArray.Length == 0)
                return "";

            string rtnString = "";

            foreach (string i in stringArray)
            {
                if (rtnString.Length > 0)
                    rtnString += ", ";
                rtnString += i.ToString();
            }

            return rtnString;
        }

        public static string GetYesNo(bool input)
        {
            return input ? "yes" : "no";
        }

        public static string PrintList(List<string> stringList)
        {
            if (stringList == null || stringList.Count == 0)
                return "";

            string rtnString = "";

            foreach (string i in stringList)
            {
                if (rtnString.Length > 0)
                    rtnString += ", ";
                rtnString += i;
            }

            return rtnString;
        }

        public static string PrintList(char[] charArray)
        {
            if (charArray == null || charArray.Length == 0)
                return "";

            string rtnString = "";

            foreach (char i in charArray)
            {
                if (rtnString.Length > 0)
                    rtnString += ", ";
                rtnString += i.ToString();
            }

            return rtnString;
        }

        public static bool Percentile(System.Random rnd, double numberThreshold)
        {
            return (rnd.NextDouble() * 100) <= numberThreshold;
        }

        public static string PrintList(List<int> intList)
        {
            if (intList == null || intList.Count == 0)
                return "";

            string rtnString = "";

            foreach (int i in intList)
            {
                if (rtnString.Length > 0)
                    rtnString += ", ";
                rtnString += i.ToString();
            }

            return rtnString;
        }

        public static List<int> CopyList(List<int> input)
        {
            if (input == null)
                return null;

            List<int> rtnList = new List<int>();
            if (input.Count == 0)
                return rtnList;

            foreach (int i in input)
            {
                rtnList.Add(i);
            }

            return rtnList;
        }

        public static string[] LowercaseStrings(string[] inputs)
        {
            if (inputs == null)
                return null;

            string[] rtnArray = new String[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                rtnArray[i] = inputs[i].ToLower();
            }
            return rtnArray;
        }

        public static string[] TrimStringsAndRemoveEmpty(string[] inputs)
        {
            if (inputs == null)
                return null;

            int emptyStrings = inputs.Count(a => a == "" || a == null);

            string[] rtnArray = new String[inputs.Length - emptyStrings];

            int addedStringsIndex = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                if(!string.IsNullOrEmpty(inputs[i]))
                {
                    rtnArray[addedStringsIndex] = inputs[i].Trim();
                    addedStringsIndex++;
                }
            }
            return rtnArray;
        }

        public static string[] FixComparators(string[] inputs)
        {
            if (inputs == null)
                return null;

            string[] rtnArray = new String[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                rtnArray[i] = inputs[i].Replace("&gt;", @">").Replace("&lt;", @"<");
            }
            return rtnArray;
        }

        public static string SanitizeInput(string s)
        {
            return s.Replace("}", "").Replace("{", "").Replace("\\", "").Replace("\"", "").Replace("\'", "").Replace("|", "").Replace(",", "");
        }

        public static int GetNumberFromInputs(string[] inputs)
        {
            int returnInt = -1;
            if (inputs == null || inputs.Length == 0)
                return returnInt;

            foreach (string s in inputs)
            {
                int.TryParse(s, out returnInt);
                if (returnInt > 0)
                {
                    break;
                }
            }

            return returnInt;
        }

        public static List<int> GetAllNumbersFromInputs(string[] inputs)
        {
            List<int> rtnList = new List<int>();
            int returnInt = -1;
            if (inputs == null || inputs.Length == 0)
                return rtnList;

            foreach (string s in inputs)
            {
                int.TryParse(s, out returnInt);
                if (returnInt > 0)
                {
                    rtnList.Add(returnInt);
                }
            }

            return rtnList;
        }

        public static string[] GetRemainingTermsAfterRemovingOneTerm(string[] inputs, string removeString)
        {
            List<string> returnList = new List<string>();

            if (inputs != null && inputs.Length > 0)
            {
                bool foundRemoval = false;
                foreach (string s in inputs)
                {
                    if(!foundRemoval && s.ToLower() == removeString.ToLower())
                    {
                        foundRemoval = true;
                    }
                    else
                    {
                        returnList.Add(s);
                    }
                }
            }

            return returnList.ToArray();
        }

        public static string[] RemoveStringFromAllTerms(string[] inputs, string removeString)
        {
            List<string> returnList = new List<string>();

            if (inputs != null && inputs.Length > 0)
            {
                foreach (string s in inputs)
                {
                    returnList.Add(s.Replace(removeString, ""));
                }
            }

            return returnList.ToArray();
        }

        public static string CombineStringArray(string[] input)
        {
            if (input == null)
                return null;
            if (input.Length == 0)
                return "";

            string returnString = "";
            foreach (string s in input)
            {
                returnString += s;
            }
            return returnString;
        }

        public static string GetChannelIdFromInputs(string[] inputs, out string error)
        {
            error = "";

            string returnString = "";
            if (inputs == null || inputs.Length == 0)
            {
                error = "Failed: Channel input not found. Include the paste of the channel's invite from using /code.";
                return returnString;
            }

            string combinedInputs = CombineStringArray(inputs);

            if (combinedInputs.Contains("[/session]"))
            {
                string combined1 = combinedInputs.Replace("[session=", "").Replace("[/session]", "");

                int startIndex = combined1.IndexOf(']') + 1;
                if (startIndex + 24 <= combined1.Length)
                {
                    string s = combined1.Substring(startIndex, 24);

                    returnString = s;
                }
                else
                {
                    error = "Failed: Channel Session input not correct length. Include the entire paste of the channel's invite from using /code.";
                }
            }
            else if(combinedInputs.ToLower().StartsWith("adh-"))
            {
                if(combinedInputs.Length != 24)
                {
                    error = "Failed: Channel Session input not correct length.";
                }
                else
                {
                    returnString = combinedInputs;
                }
            }
            else
            {
                error = "Failed: Channel Session input not found. Include the entire paste of the channel's invite from using /code.";
            }

            return returnString;
        }

        public static string RemoveChannelIdFromInputsAndReturnFullInputs(string[] inputs, out string error)
        {
            error = "";

            string combinedInputs = GetFullStringOfInputs(inputs);
            if (combinedInputs.Contains("[/session]"))
            {
                int index1 = combinedInputs.IndexOf("[session=");
                int index2 = combinedInputs.IndexOf("[/session]");

                combinedInputs = combinedInputs.Substring(0, index1) + combinedInputs.Substring(index2 + 10);
            }

            return combinedInputs;
        }

        public static string GetFullStringOfInputsAfterTermX(string[] inputs, int termsToIgnore)
        {
            if (inputs == null || inputs.Length <= termsToIgnore)
                return null;

            string[] newInputs = new string[inputs.Length - termsToIgnore];
            int currentIndex = 0;
            for (int i = termsToIgnore; i < inputs.Length; i++)
            {
                newInputs[currentIndex] = inputs[i];
                currentIndex++;
            }

            return GetFullStringOfInputs(newInputs);
        }

        public static string GetFullStringOfInputs(string [] inputs)
        {
            string returnString = "";
            if (inputs == null || inputs.Length == 0)
                return returnString;

            string[] newArray = new string[inputs.Length];
            for (int i = 0; i < inputs.Length - 1; i++)
            {
                newArray[i] = inputs[i] + " ";
            }
            newArray[inputs.Length - 1] = inputs[inputs.Length - 1];
            //for (int i = 0; i < inputs.Length - 1; i++ )
            //{
            //    inputs[i] = inputs[i] + " ";
            //}

            returnString = CombineStringArray(newArray);

            return returnString;
        }

        public static string GetUserNameFromFullInputs(string [] inputs)
        {
            string partial = GetFullStringOfInputs(inputs);
            partial = partial.Replace("[user]", "").Replace("[/user]", "").Replace("[b]", "").Replace("[/b]", "").Replace("[icon]", "").Replace("[/icon]", "").Trim();
            return partial;
        }

        public static bool GetNsfwError(ChannelSettings settings, ICustomUserContent userContent, out string errorMessage)
        {
            errorMessage =  "";
            if (settings == null || settings.AllowNsfw || userContent == null || !userContent.IsNsfw())
                return false;
            else
            {
                errorMessage = "Failed: This item contains Nsfw content and these are not allowed in this guild's settings.";
                return true;
            }
        }

        public static string LimitStringToNCharacters(string inputString, int maxCharacters)//, bool addTestLength = false)
        {
            if (string.IsNullOrEmpty(inputString))
                return "";

            string rtn = "";
            int characterPoints = 0;
            //there is an issue where specialCharacters count as 3 characters each for length when transmitted to the server
            //string specialCharacters = "♥♦♣♠🍵✪ƪ✝"; //altering so it counts anything outside of ASCI 32 (space) to ASCI 126 (~)
            for (int i = 0; i < inputString.Length; i++ )
            {
                char thisChar = inputString[i];
                if (thisChar < 32 || thisChar > 126)//specialCharacters.Contains(thisChar))
                    characterPoints += 3;
                else
                    characterPoints += 1;

                if (characterPoints > maxCharacters)
                    break;
                rtn += inputString[i];
            }

            //if (addTestLength)
            //    return rtn;// + " {" + characterPoints + "}";
            //else
                return rtn;
            // (inputString.Length > maxCharacters ? inputString.Substring(0, maxCharacters) : inputString);
        }

        public static string GetDeckTypeString(DeckType deckType, string customDeckName)
        {
            switch(deckType)
            {
                case DeckType.Playing:
                    return "Playing";
                case DeckType.Tarot:
                    return "Tarot";
                case DeckType.ManyThings:
                    return "Many Things";
                case DeckType.Uno:
                    return "Uno";
                case DeckType.BreakerRumble:
                    return "Breaker Rumble";
                case DeckType.BreakerRumbleExtra:
                    return "Breaker Rumble (Extra)";
                case DeckType.BreakerRumbleClassic:
                    return "Breaker Rumble (Classic)";
                case DeckType.NONE:
                    return "[Deck Error]";
                case DeckType.Custom:
                    return customDeckName;
            }
            return "undefined";
        }

        public static string GetCardMoveTypeString(CardMoveType moveType)
        {
            switch (moveType)
            {
                case CardMoveType.DiscardCard:
                    return "discarded";
                case CardMoveType.PlayCard:
                    return "played";
                case CardMoveType.ToHandFromDiscard:
                    return "moved to hand from discard";
                case CardMoveType.ToPlayFromDiscard:
                    return "moved to play from discard";
                case CardMoveType.ToDiscardFromPlay:
                    return "moved to discard from play";
                case CardMoveType.ToHandFromPlay:
                    return "moved to hand from play";
            }

            return "undefined";
        }

        public static string GetCardMoveTypeString(CardPileId fromPile, CardPileId toPile)
        {
            return "from " + GetPileName(fromPile) + " to " + GetPileName(toPile);
        }

        public static string GetPileName(CardPileId pileId)
        {
            switch(pileId)
            {
                case CardPileId.Hand:
                    return "hand";
                case CardPileId.Deck:
                    return "deck";
                case CardPileId.Burn:
                    return "burned";
                case CardPileId.Dealer:
                    return "dealer";
                case CardPileId.Play:
                    return "play";
                case CardPileId.Discard:
                    return "discard";
                case CardPileId.HiddenInPlay:
                    return "hidden";
                case CardPileId.NONE:
                default:
                    return "noPile";
            }
        }

        public static string GetDeckTypeStringHidePlaying(DeckType deckType, string customDeckName)
        {
            string deckTypeString = "";
            if (deckType != DeckType.Playing)
            {
                deckTypeString = "(" + Utils.GetDeckTypeString(deckType, customDeckName) + ") ";
            }
            return deckTypeString;
        }

        public static Deck CreateDeckFromInput(string saveDeckInput)
        {
            String[] allCards = saveDeckInput.Split(',');
            Deck d = new Deck(DeckType.Custom);
            List<DeckCard> cards = new List<DeckCard>();
            if(allCards != null)
            {
                foreach (string s in allCards)
                {
                    string cardName = SanitizeInput(s.Trim());
                    if (s.Count() > 100)
                        cardName = s.Take(100).ToString();
                    cards.Add(new DeckCard() { specialName = cardName });
                }
            }
            d.InsertNewCards(cards);

            return d;
        }

        public static string GetTimeSpanPrint(TimeSpan t)
        {
            string form = string.Format("{0} days, {1} hours, {2} minutes, {3} seconds", t.Days, t.Hours, t.Minutes, t.Seconds);
            return form;
        }

        public static void CopyFile(string fileName, string sourcePath, string destinationPath)
        {
            try
            {
                string transferStart = GetTotalFileName(sourcePath, fileName);
                string transferTarget = GetTotalFileName(destinationPath, fileName);
                System.IO.File.Copy(transferStart, transferTarget, true);

            }catch(Exception exc)
            {
                Console.WriteLine("Exception on copy file " + exc.ToString());
            }
        }

        public static void WriteToFileAsData(object saveData, string fileName)
        {
            string g = JsonConvert.SerializeObject(saveData);
            string filePath = fileName;

            try
            {
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(g);
                File.WriteAllBytes(filePath, bytes);
            }
            catch (System.Exception)
            {
                Console.WriteLine("exception on writeAsData " + filePath);
            }
        }

        public static void AddToLog(string note, object saveData)
        {
            try
            {
                AppendToFileAsData(DateTime.Now.ToString() + ": " + note + ": ", saveData, Utils.GetTotalFileName(BotMain.LogsFolder, AddDateToFileName(BotMain.LogFileName)));

            }catch(Exception exc)
            {
                Console.WriteLine("exception on addtolog: " + exc.ToString());
            }
        }
        public static void AddToChannelAuditLog(string note, object saveData)
        {
            try
            {
                AppendToFileAsData(DateTime.Now.ToString() + ": " + note + ": ", saveData, Utils.GetTotalFileName(BotMain.LogsFolder, AddDateToFileName(BotMain.ChannelAuditLogFileName)));

            }
            catch (Exception exc)
            {
                Console.WriteLine("exception on addtochannelauditlog: " + exc.ToString());
            }
        }

        public static string AddDateToFileName(string fileName)
        {
            DateTime dNow = DateTime.Now.Date;
            string dateCode = "_" +  dNow.Month + "_" + dNow.Day + "_" + dNow.Year;

            string usedName = fileName;
            string usedSuffix = ".txt";
            if (fileName.Contains('.'))
            {
                string[] stringparts = fileName.Split('.');
                usedName = stringparts[0];
                usedSuffix = "." + stringparts[1];
            }

            return usedName + dateCode + usedSuffix;
        }

        public static void AppendToFileAsData(string noteString, object saveData, string fileName)
        {
            string g = saveData == null? "" : JsonConvert.SerializeObject(saveData);
            string filePath = fileName;

            try
            {
                File.AppendAllLines(filePath, new List<string>() {noteString, g});
            }
            catch (System.Exception)
            {
                Console.WriteLine("exception on writeAsData " + filePath);
            }
        }

        public static string GetStringOfNLength(int n)
        {
            string rtnString = "";
            for(int i = 0; i < n; i++)
            {
                rtnString += "1";
            }
            return rtnString;
        }

        public static string GetStringOfNLength(int n, string x)
        {
            string rtnString = "";
            for (int i = 0; i < n; i++)
            {
                rtnString += x;
            }
            return rtnString;
        }

        public static string GetTotalFileName(string folderName, string fileName)
        {
            string fileNameWithTest = BotMain._testVersion ? BotMain.TestFilePrefix + fileName : fileName;
            return folderName + "\\" + fileNameWithTest;
        }

        public static bool IsCharacterAdmin(List<string> adminCharacters, string character)
        {
            return adminCharacters.Contains(character);
        }

        public static bool IsCharacterTrusted(List<ChannelCharacter> trustedCharacters, MessageAddress address)
        {
            string channelKey = address.GetChannelKey();
            return trustedCharacters.Count(a => a.Channel.ToLower() == channelKey.ToLower() && a.Character == address.character) > 0;
        }

        public static bool BotMessageIsFListChatMessage(BotMessage message)
        {
            return message.messageType == BotMessageFactory.MSG || message.messageType == BotMessageFactory.PRI;
        }

        public static string GetCharacterStringFromSpecialName(string characterName)
        {
            string cardDrawingCharacterString = TextFormat.GetCharacterUserTags(characterName);
            if (characterName == DiceBot.DealerPlayerAlias)
                cardDrawingCharacterString = "The dealer";
            else if (characterName == DiceBot.BurnCardsPlayerAlias)
                cardDrawingCharacterString = "Burned";
            else if (characterName == DiceBot.DiscardPlayerAlias)
                cardDrawingCharacterString = "Discarded";
            else if (characterName == DiceBot.PotPlayerAlias)
                cardDrawingCharacterString = "The pot";
            else if (characterName == DiceBot.HousePlayerAlias)
                cardDrawingCharacterString = "The house";

            return cardDrawingCharacterString;
        }

        public static bool IsDiscordAdmin(SocketGuildUser discordUser)
        {
            if (discordUser == null)
                return false;

            if (discordUser.Guild.OwnerId == discordUser.Id)
                return true;

            var perms = discordUser.GuildPermissions;

            return perms.Administrator
                || perms.ManageMessages
                || perms.KickMembers
                || perms.BanMembers
                || perms.ManageChannels;
        }

        //public static bool IsDiscordAdmin(SocketGuildUser discordUser)
        //{
        //    // Ensure the message is from a guild and is not from a bot
        //    if (discordUser == null)
        //        return false;
        //    else
        //    {
        //        // Define the moderator permissions you are looking for
        //        var moderatorPermissions = new GuildPermission[]
        //        {
        //            GuildPermission.ManageMessages,
        //            GuildPermission.KickMembers,
        //            GuildPermission.BanMembers,
        //            GuildPermission.ManageChannels,
        //            GuildPermission.Administrator
        //        };

        //        // Check if the user has any role with the defined moderator permissions
        //        bool isModerator = discordUser.Roles.Any(role => role.Permissions.ToList().Intersect(moderatorPermissions).Any());

        //        return isModerator;
        //    }
        //}

        public static bool IsDiscordMessage(UserGeneratedCommand command)
        {
            return !string.IsNullOrEmpty(command.guild);
        }
        public static bool IsDiscordMessage(MessageAddress command)
        {
            return !string.IsNullOrEmpty(command.guild);
        }

        public static SavedRollTable GetTableFromId(List<SavedRollTable> tables, string id)
        {
            SavedRollTable infoTable = tables.FirstOrDefault(a => a.TableId == id);

            return infoTable;
        }

        public static SavedJobsList GetJobsListFromChannel(List<SavedJobsList> jobsLists, string channelId)
        {
            SavedJobsList jobsList = jobsLists.FirstOrDefault(a => a.Channel.ToLower() == channelId.ToLower());
            return jobsList;
        }

        public static SavedSlotsSetting GetSlotsFromId(List<SavedSlotsSetting> slots, string id)
        {
            if (id == null || slots == null)
                return null;
            SavedSlotsSetting infoSlots = slots.FirstOrDefault(a => a.SlotsId.ToLower() == id.ToLower());

            return infoSlots;
        }

        public static SavedDeck GetDeckFromId(List<SavedDeck> decks, string id)
        {
            SavedDeck deck = decks.FirstOrDefault(a => a.DeckId == id);

            return deck;
        }

        //public static string GetCustomDeckName(string character)
        //{
        //    return character + "'s deck";
        //}

        public static string GetRollModifierString(int rollModifier)
        {
            return rollModifier != 0 ? (" " + (rollModifier > 0 ? "+" : "") + rollModifier.ToString()) : "";
        }

        public static T CreateType<T>() where T : new()
        {
            return new T();
        }

        public static string GetRouletteBetString(RouletteBet bet, int numberBet)
        {
            string rtn = "";

            switch(bet)
            {
                case RouletteBet.NONE:
                    rtn = "NONE";
                    break;
                case RouletteBet.Black:
                    rtn = "Black";
                    break;
                case RouletteBet.Red:
                    rtn = "Red";
                    break;
                case RouletteBet.Even:
                    rtn = "Even";
                    break;
                case RouletteBet.Odd:
                    rtn = "Odd";
                    break;
                case RouletteBet.First12:
                    rtn = "First 12";
                    break;
                case RouletteBet.Second12:
                    rtn = "Second 12";
                    break;
                case RouletteBet.Third12:
                    rtn = "Third 12";
                    break;
                case RouletteBet.OneToEighteen:
                    rtn = "First Half";
                    break;
                case RouletteBet.NineteenToThirtySix:
                    rtn = "Second Half";
                    break;
                case RouletteBet.SpecificNumber:
                    rtn = "Number " + numberBet;
                    break;
            }


            return rtn;
        }

        public static string RandomString(System.Random rnd, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[rnd.Next(s.Length)]).ToArray());
        }
    }
}
