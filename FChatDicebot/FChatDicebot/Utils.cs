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

        public static string PrintList(List<string> stringList)
        {
            if (stringList == null || stringList.Count == 0)
                return "";

            string rtnString = "";

            foreach (string i in stringList)
            {
                if (rtnString.Length > 0)
                    rtnString += ", ";
                rtnString += i.ToString();
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
            return s.Replace("}", "").Replace("{", "").Replace("\\", "").Replace("\"", "").Replace("\'", "").Replace("|","").Replace(",","");
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

        public static string GetChannelIdFromInputs(string[] inputs)
        {
            string returnString = "";
            if (inputs == null || inputs.Length == 0)
                return returnString;

            string combinedInputs = CombineStringArray(inputs);

            if (combinedInputs.Contains("[/session]"))
            {
                string combined1 = combinedInputs.Replace("[session=", "").Replace("[/session]", "");

                int startIndex = combined1.IndexOf(']') + 1;
                if (startIndex + 24 >= combined1.Length)
                {
                    string s = combined1.Substring(startIndex, 24);

                    returnString = s;
                }
            }

            return returnString;
        }

        public static string GetFullStringOfInputs(string [] inputs)
        {
            string returnString = "";
            if (inputs == null || inputs.Length == 0)
                return returnString;

            for (int i = 0; i < inputs.Length - 1; i++ )
            {
                inputs[i] = inputs[i] + " ";
            }

            returnString = CombineStringArray(inputs);

            return returnString;
        }

        public static string LimitStringToNCharacters(string inputString, int maxCharacters)
        {
            return (inputString.Length > maxCharacters ? inputString.Substring(0, maxCharacters) : inputString);
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
            foreach(string s in allCards)
            {
                string cardName = SanitizeInput(s.Trim());
                if (s.Count() > 100)
                    cardName = s.Take(100).ToString();
                cards.Add(new DeckCard() { specialName = cardName });
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

        public static string GetTotalFileName(string folderName, string fileName)
        {
            string fileNameWithTest = BotMain._testVersion ? BotMain.TestFilePrefix + fileName : fileName;
            return folderName + "\\" + fileNameWithTest;
        }

        public static bool IsCharacterAdmin(List<string> adminCharacters, string character)
        {
            return adminCharacters.Contains(character);
        }

        public static bool IsCharacterTrusted(List<ChannelCharacter> trustedCharacters, string character, string channel)
        {
            return trustedCharacters.Count(a => a.Channel == channel && a.Character == character) > 0;
        }

        public static bool BotMessageIsChatMessage(BotMessage message)
        {
            return message.messageType == BotMessageFactory.MSG || message.messageType == BotMessageFactory.PRI;
        }

        public static string GetCharacterUserTags(string characterName)
        {
            return "[user]" + characterName + "[/user]";
        }

        public static string GetCharacterIconTags(string characterName)
        {
            return "[icon]" + characterName + "[/icon]";
        }

        public static string GetCharacterStringFromSpecialName(string characterName)
        {
            string cardDrawingCharacterString = GetCharacterUserTags(characterName);
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

        public static SavedRollTable GetTableFromId(List<SavedRollTable> tables, string id)
        {
            SavedRollTable infoTable = tables.FirstOrDefault(a => a.TableId == id);

            return infoTable;
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

        public static string GetCustomDeckName(string character)
        {
            return character + "'s deck";
        }

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

        public static string AnOrA(string needsAnOrA)
        {
            List<string> vowels = new List<string>() { "a", "e", "i", "o", "u", "A", "E", "I", "O", "U"};
            if (vowels.Contains(needsAnOrA.Substring(0, 1))) //string starts with a vowel
            {
                return "an";
            }
            //string does not start with vowel
            return "a";
        }

        public static string sortedListDisplayText(List<string> listToDisplay)
        {
            List<string> displayList = new List<string>();
            foreach (string displayText in listToDisplay)
            {
                displayList.Add(displayText);
            }
            displayList.Sort();
            return String.Join(", ", displayList);
        }

        public static string LocationToText(string location, string initiatorDisplayName, string recipientDisplayName)
        {
            Dictionary<string, string> locationText = new Dictionary<string, string>
                    {
                        { "theirroom", "in " + recipientDisplayName + "'s room" },
                        { "myroom", "in " + initiatorDisplayName + "'s room" },
                        { "theiroffice", "in " + recipientDisplayName + "'s office" },
                        { "myoffice", "in " + initiatorDisplayName + "'s office" },
                        { "theircave", "in " + recipientDisplayName + "'s cavernous abode" },
                        { "mycave", "in " + initiatorDisplayName + "'s cavernous abode" },
                        { "theirworkshop", "in " + recipientDisplayName + "'s workshop" },
                        { "myworkshop", "in " + initiatorDisplayName + "'s workshop" },
                        { "magiclibrary", "in the magic library" },
                        { "guestroom", "in a guest room" },
                        { "queenstudy", "in the Queen's study" },
                        { "cuddlepuddle", "in the cuddlepuddle" },
                        { "rootgarden", "in the root garden" },
                        { "storage", "in the storage room" },
                        { "treasury", "in the treasury" },
                        { "loom", "by the grand loom" },
                        { "topfloor", "somewhere on the top floor" },
                        { "basement", "somewhere in the basement" },
                        { "outside", "somewhere outside" },
                        { "groundfloor", "somewhere on the ground floor" },
                        { "secondfloor", "somewhere on the second floor" },
                        { "milkingstall", "in the milking stalls" },
                        { "cellar", "in the cellar" },
                        { "gloryhole", "in the gloryholes" },
                        { "throne", "in the throneroom" },
                        { "chapel", "in the chapel" },
                        { "aquarium", "in the aquarium" },
                        { "lounge", "in the lounge" },
                        { "giftshop", "in the giftshop" },
                        { "labs", "in the labs" },
                        { "kitchen", "in the kitchen" },
                        { "entrance", "in the entrancehall" },
                        { "library", "in the library" },
                        { "reception", "in reception" },
                        { "stables", "in the stables" },
                        { "forest", "out in the forest" },
                        { "beastpens", "out in the beastpens" },
                        { "garden", "out in the garden" },
                        { "fields", "out in the fields" },
                        { "hotsprings", "in the hotsprings" },
                        { "restroom", "in one of the restrooms" },
                        { "arena", "out in the arena" }
                    };
            if (locationText.ContainsKey(location)){
                return locationText[location];
            } 
            else
            {
                return "somewhere ineffable [spoiler]that means [user]Queen Contract[/user] needs to update Utils.LocationToText to include " + location + ", go tell her to fix it!";
            }
        }

        public static string AttireToText(string attire)
        {
            Dictionary<string, string> attireText = new Dictionary<string, string>
                    {
                        { "bottomless", "a bottomless outfit" },
                        { "oriental", "oriental garb"},
                        { "regal", "regal attire"},
                        { "nude", "their birthday suit"},
                        { "wedding", "wedding attire"},
                        { "stripper", "stripper clothes"},
                        { "piercings", "piercings and only piercings"},
                        { "gyaru", "gyaru garb"},
                        { "bondage", "some form of bondage"},
                        { "idol", "an idol costume"},
                        { "bunnygirl", "a bunnygirl leotard"},
                        { "shibari", "complicated shibari ropes"},
                        { "maidoutfit", "a maid outfit"},
                        { "swimsuit", "a swimsuit"},
                        { "slutty", "something super slutty"},
                        { "punk", "something punk"},
                        { "office", "office attire"},
                        { "casual", "casual clothes"},
                        { "bimbo", "bimbo stuff"},
                        { "armored", "protective armor"},
                        { "practical", "something practical"},
                        { "traditional", "something traditional"},
                        { "bitchsuit", "piddlefours"},
                        { "socks", "just their socks"},
                        { "lingerie", "sexy lingerie"},
                        { "cosplay", "a custom cosplay"},
                        { "formal", "formalwear"},
                        { "topless", "a topless outfit"},
                        { "workout", "workout clothes"},
                        { "ruined", "some ruined fabric"},
                        { "western", "western attire"},
                        { "latex", "a latex outfit"},

                    };
            if (attireText.ContainsKey(attire))
            {
                return attireText[attire];
            }
            else
            {
                return "something ineffable [spoiler]that means [user]Queen Contract[/user] needs to update Utils.AttireToText to include " + attire + ", go tell her to fix it!";
            }
        }

        public static string SubstanceToText(string substance)
        {
            Dictionary<string, string> substanceText = new Dictionary<string, string>
                    {
                        { "fire", "literal fire"},
                        { "water", "water"},
                        { "earth", "elemental earth"},
                        { "air", "air"},
                        { "golden", "golden fluid"},
                        { "cum", "cum"},
                        { "goo", "slimy goo"},
                        { "lustessence", "[color=purple]Lust Essence[/color]"},
                        { "pollen", "pollen"},
                        { "sap", "sap"},
                        { "pre", "precum"},
                        { "saliva", "saliva"},
                        { "sweat", "sweat"},
                        { "drug", "sort of drug"},
                        { "drink", "drink"},
                        { "food", "food"},
                        { "energy", "pure energy"}
                    };
            if (substanceText.ContainsKey(substance))
            {
                return substanceText[substance];
            }
            else
            {
                return "something ineffable [spoiler]that means [user]Queen Contract[/user] needs to update Utils.SubstanceToText to include " + substance + ", go tell her to fix it!";
            }
        }
        public static string BodypartToText(string bodypart)
        {

            if (bodypart == "lowerback")
            {
                return "lower back";
            }
            else
            {
                return bodypart;
            }
        }
       

    }
}
