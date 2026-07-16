using Discord;
using FChatDicebot.SavedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FChatDicebot
{
    public static class TextFormat
    {
        public static string SIfPlural(int number)
        {
            return number == 1 ? "" : "s";
        }

        /// <summary>
        /// Naive English noun pluralization: -s/-x/-z/-ch/-sh take "es", consonant+y takes
        /// "ies", everything else takes "s" (kiss → kisses, bully → bullies, cuddle → cuddles).
        /// </summary>
        public static string PluralizeNoun(string noun)
        {
            if (string.IsNullOrEmpty(noun))
                return noun;
            string lower = noun.ToLowerInvariant();
            if (lower.EndsWith("s") || lower.EndsWith("x") || lower.EndsWith("z")
                || lower.EndsWith("ch") || lower.EndsWith("sh"))
                return noun + "es";
            if (lower.Length >= 2 && lower.EndsWith("y") && "aeiou".IndexOf(lower[lower.Length - 2]) < 0)
                return noun.Substring(0, noun.Length - 1) + "ies";
            return noun + "s";
        }

        public static string CapitalizeFirst(string s)
        {
            if (s != null && s.Length > 1)
            {
                return s[0].ToString().ToUpper() + s.Substring(1);
            }
            if (s != null && s.Length == 1)
            {
                return s[0].ToString().ToUpper();
            }
            else
                return s;
        }

        public static string GetCharacterUserTags(string characterName)
        {
            return string.Format("[user]{0}[/user]", characterName);
                //OpenFormat(FormatType.UserTag, BotMain.ChatRoom) + characterName + CloseFormat(FormatType.UserTag, BotMain.ChatRoom);
        }

        public static string GetCharacterIconTags(string characterName)
        {
            return string.Format("[icon]{0}[/icon]", characterName);
            //return OpenFormat(FormatType.UserIcon, BotMain.ChatRoom) + characterName + CloseFormat(FormatType.UserIcon, BotMain.ChatRoom);
        }

        public static string GetPlayerNameFromInputs(BotMain bot, string[] rawTerms, bool excludeGameNames)
        {
            //get player name to remove
            string allInputs = Utils.GetUserNameFromFullInputs(rawTerms);

            if (allInputs.Contains("_noreplace_"))
            {
                allInputs.Replace("_noreplace_", "");
            }
            else
            {
                if(excludeGameNames)
                {
                    if(allInputs.Contains("game:"))
                    {
                        string before = allInputs.Substring(0, allInputs.IndexOf("game:"));
                        string after = allInputs.Substring(before.Length);
                        if(after.IndexOf(" ") < after.Length - 1)
                        {
                            after = after.Substring(after.IndexOf(" "));
                            allInputs = before + after;
                        }
                    }
                    else
                    {
                        foreach (string s in bot.DiceBot.PossibleGames.Select(a => a.GetGameName()))
                        {
                            allInputs = allInputs.Replace(s, "");
                        }
                    }
                }
            }

            allInputs = allInputs.Trim();
            return allInputs;
        }

        //TODO: fix this to take in a chat message format
        public static string Emoji(string emojiname)//, ChatRoom room)
        {
            if (BotMain.ChatRoom == ChatRoom.FChat)
            {
                return "[eicon]" + emojiname + "[/eicon]";
            }
            else if (BotMain.ChatRoom == ChatRoom.Discord)
            {
                //TODO: somehow verify it's a supported emoji
                //TODO :: need some kind of table for what each icon/ emoji is called on each platform
                return "";
            }
            return "";
        }

        public static string ApplyNumberCommasIfNecessary(string chatMessage, ChannelSettings settings)
        {
            if (settings != null && settings.ShowCommasInNumbers)
                return ApplyNumberCommas(chatMessage);
            else
                return chatMessage;
        }

        public static string ApplyNumberCommas(string chatMessage)
        {
            try
            {

                if (string.IsNullOrEmpty(chatMessage))
                {
                    return chatMessage;
                }

                // Regular expression to match sequences of digits
                var regex = new Regex(@"\d+");

                // Replace each match with a formatted version that includes commas
                return regex.Replace(chatMessage, match =>
                {
                    // Format the number with commas
                    return long.Parse(match.Value).ToString("N0");
                });
            }
            catch (Exception exc)
            {
                Console.Write("exception on ApplyNumberCommas: " + exc.ToString());
            }

            return chatMessage;
        }

        public static string SpoilerAllIfEnabled(string chatMessage, ChannelSettings settings)
        {
            if (settings != null && settings.SpoilerAllOutputs && !string.IsNullOrEmpty(chatMessage) && !chatMessage.ToLower().Contains("[spoiler]"))
                return "[spoiler]" + chatMessage + "[/spoiler]";
            else
                return chatMessage;
        }

        public static string SubstituteInCurrencyName(string chatMessage, ChannelSettings settings)
        {
            if (settings != null && !string.IsNullOrEmpty(settings.CurrencyName))
                return ApplyCurrencyName(chatMessage, settings.CurrencyName, settings.CurrencyNamePlural);
            else
                return ApplyCurrencyName(chatMessage, BotMain.DefaultCurrencyName, BotMain.DefaultCurrencyNamePlural); //chatMessage;
        }

        public static string ApplyCurrencyName(string chatMessage, string currencyName, string currencyNamePlural)
        {
            string currencyNameCap = !string.IsNullOrEmpty(currencyName)? currencyName[0].ToString().ToUpper() + currencyName.Substring(1) : currencyName;
            string currencyNameCapPlural = !string.IsNullOrEmpty(currencyNamePlural) ? currencyNamePlural[0].ToString().ToUpper() + currencyNamePlural.Substring(1) : currencyNamePlural;

            return chatMessage
                .Replace(BotMain.CurrencyPlaceholder + "s", currencyNamePlural)
                .Replace(BotMain.CurrencyPlaceholderCapital + "s", currencyNameCapPlural)
                .Replace(BotMain.CurrencyPlaceholder, currencyName)
                .Replace(BotMain.CurrencyPlaceholderCapital, currencyNameCap);
        }

        //make after-filter instead of directly changing every instance?

        public static string ChangeToDiscordText(string fchatMessage, IUser user)
        {
            string newMessage = fchatMessage.Replace("[b]", "**").Replace("[/b]", "**");
            newMessage = newMessage.Replace("[i]", "_").Replace("[/i]", "_");
            newMessage = newMessage.Replace("[sup]", "").Replace("[/sup]", "");//super and subtext REMOVED (no discord equivalent)
            newMessage = newMessage.Replace("[sub]", "").Replace("[/sub]", "");
            //newMessage = newMessage.Replace("[sup]", "_").Replace("[/sup]", "_");//super and subtext go to italic (no discord equivalent)
            //newMessage = newMessage.Replace("[sub]", "_").Replace("[/sub]", "_");
            //newMessage = newMessage.Replace("[color=red]", "**").Replace("[color=blue]", "**").Replace("[color=green]", "**")
            //    .Replace("[color=yellow]", "**").Replace("[color=gray]", "**").Replace("[color=black]", "**")
            //    .Replace("[/color]", "**"); //replace color with bold (no discord equivalent)
            List<string> colorTags = new List<string>() { "[color=white]", "[color=red]", "[color=orange]",
                "[color=pink]", "[color=purple]", "[color=cyan]",
                "[color=blue]", "[color=green]", "[color=yellow]", "[color=gray]", "[color=black]", "[/color]" };
            foreach (string s in colorTags)
            {
                newMessage = newMessage.Replace(s, "");//remove all color tags
            }

            newMessage = newMessage.Replace("[user]Ambitious Syndra[/user]", "@ghostlyvision");

            //newMessage = ReplaceEiconsWithText(newMessage); //replace selected eicons with text
            //newMessage = RemoveEiconTags(newMessage);//remove all eicons
            newMessage = ReplaceEiconsWithDiscordEmotes(newMessage);
            newMessage = newMessage.Replace("[s]", "~~").Replace("[/s]", "~~");
            newMessage = newMessage.Replace("[u]", "__").Replace("[/u]", "__");
            newMessage = newMessage.Replace("[spoiler]", "||").Replace("[/spoiler]", "||");
            if(user != null)
            {
                newMessage = newMessage.Replace("[user]" + user.Username, user.Mention).Replace("[/user]", "");
                newMessage = newMessage.Replace("[icon]" + user.Username, user.Mention).Replace("[/icon]", "");
            }
            else
            {
                newMessage = newMessage.Replace("[user]", "").Replace("[/user]", "");
                newMessage = newMessage.Replace("[icon]", "").Replace("[/icon]", "");
            }

            //remove these leftover tags
            newMessage = newMessage.Replace("[user]", "").Replace("[/user]", "");
            newMessage = newMessage.Replace("[icon]", "").Replace("[/icon]", "");

            //eicons should be handled differently...

            return newMessage;
        }

        public static string ReplaceEiconsWithText(string input)
        {
            return input.Replace("[eicon]dbpoker1[/eicon]", "POKER")
                .Replace("[eicon]dbblackjack1[/eicon]", "BLACKJACK")
                .Replace("[eicon]dbbottle1[/eicon]", "BOTTLE SPIN")
                .Replace("[eicon]dbhighroll1[/eicon]", "BOTTLE SPIN")
                .Replace("[eicon]dbkingsgame1b[/eicon]", "KING'S GAME")
                .Replace("[eicon]dbliardice1[/eicon]", "LIAR'S DICE")
                .Replace("[eicon]dbmafia1[/eicon]", "MAFIA")
                .Replace("[eicon]dbrps1[/eicon]", "ROCK PAPER SCISSORS")
                .Replace("[eicon]dbroulette1[/eicon]", "ROULETTE")
                .Replace("[eicon]dbslamroll1[/eicon]", "SLAM ROLL")
                .Replace("[eicon]dbslots1[/eicon]", "SLOTS");
        }

        public static string ReplaceEiconsWithDiscordEmotes(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Regex to match [eicon]content[/eicon]
            string pattern = @"\[eicon\](.*?)\[/eicon\]";

            // Use Regex.Replace with a MatchEvaluator to replace each eicon dynamically
            return Regex.Replace(input, pattern, match =>
            {
                string eiconContent = match.Groups[1].Value; // This is the "texthere" between the tags
                string replacement = GetDiscordEmote(eiconContent); // Get the Discord emote for that content
                return replacement; // Replace the whole tag with the emote
            });
        }

        public static string RemoveEiconTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Regex pattern to match [eicon]...[/eicon] including the tags
            string pattern = @"\[eicon\].*?\[/eicon\]";

            // Replace all matches with an empty string
            return Regex.Replace(input, pattern, string.Empty);
        }

        public static Dictionary<string, string> DiscordEmotes = null;

        public static string GetDiscordEmote(string flistEicon)
        {
            InitializeDiscordEmotes();

            string rtn = "";
            DiscordEmotes.TryGetValue(flistEicon, out rtn);
            return rtn;
        }

        public static void InitializeDiscordEmotes()
        {
            if (DiscordEmotes != null)
                return;

            DiscordEmotes = new Dictionary<string, string>();
            DiscordEmotes.Add("dbblackjack1", "<:dbblackjack1:1346304246760345662>");
            DiscordEmotes.Add("dbblackjack2", "<:dbblackjack2:1346304265202962433>");
            DiscordEmotes.Add("dbbottle1", "<:SpinTheBottle1:1346264278029570100>");
            DiscordEmotes.Add("dbbottle2", "<:SpinTheBottle2:1346264315245887498>");
            DiscordEmotes.Add("db-d6-1", "<:d6_1:1376316994458091550>");
            DiscordEmotes.Add("db-d6-2", "<:d6_2:1376317095121522708>");
            DiscordEmotes.Add("db-d6-3", "<:d6_3:1376317131200921610>");
            DiscordEmotes.Add("db-d6-4", "<:d6_4:1376317147315306628>");
            DiscordEmotes.Add("db-d6-5", "<:d6_5:1376317164700827698>");
            DiscordEmotes.Add("db-d6-6", "<:d6_6:1376317182132224120>");
            DiscordEmotes.Add("db-d6gold-1", "<:d6gold1:1376317248456884278>");
            DiscordEmotes.Add("db-d6gold-2", "<:d6gold2:1376317345537986610>");
            DiscordEmotes.Add("db-d6gold-3", "<:d6gold3:1376317361782788197>");
            DiscordEmotes.Add("db-d6gold-4", "<:d6gold4:1376317376441618543>");
            DiscordEmotes.Add("db-d6gold-5", "<:d6gold5:1376317390891257886>");
            DiscordEmotes.Add("db-d6gold-6", "<:d6gold6:1376317407815139359>");
            //DiscordEmotes.Add("dbd6-1", "<:RegularDice1:1345916908997709905>");
            //DiscordEmotes.Add("dbd6-2", "<:RegularDice2:1345916939880501333>");
            //DiscordEmotes.Add("dbd6-3", "<:RegularDice3:1345916969487962154>");
            //DiscordEmotes.Add("dbd6-4", "<:RegularDice4:1345916994003669205>");
            //DiscordEmotes.Add("dbd6-5", "<:RegularDice5:1345917020675379200>");
            //DiscordEmotes.Add("dbd6-6", "<:RegularDice6:1345917048613765131>");
            //DiscordEmotes.Add("dbgoldd6-1", "<:GoldDice1:1345916743435944041>");
            //DiscordEmotes.Add("dbgoldd6-2", "<:GoldDice2:1345916770933936219>");
            //DiscordEmotes.Add("dbgoldd6-3", "<:GoldDice3:1345916792169693284>");
            //DiscordEmotes.Add("dbgoldd6-4", "<:GoldDice4:1345916820200231075>");
            //DiscordEmotes.Add("dbgoldd6-5", "<:GoldDice5:1345916838864748634>");
            //DiscordEmotes.Add("dbgoldd6-6", "<:GoldDice6:1345916858997543032>");
            DiscordEmotes.Add("d4-1", "<:d4_1:1346988080418132021>");
            DiscordEmotes.Add("d4-2", "<:d4_2:1346988114463162388>");
            DiscordEmotes.Add("d4-3", "<:d4_3:1346988171354706082>");
            DiscordEmotes.Add("d4-4", "<:d4_4:1346988183359066152>");
            DiscordEmotes.Add("d4gold-1", "<:d4gold1:1346988201889239123>");
            DiscordEmotes.Add("d4gold-2", "<:d4gold2:1346988215084519536>");
            DiscordEmotes.Add("d4gold-3", "<:d4gold3:1346988228191981648>");
            DiscordEmotes.Add("d4gold-4", "<:d4gold4:1346988238715486269>");
            DiscordEmotes.Add("d8-1", "<:d8_1:1346988255383519232>");
            DiscordEmotes.Add("d8-2", "<:d8_2:1346988266628452413>");
            DiscordEmotes.Add("d8-3", "<:d8_3:1346988275856052326>");
            DiscordEmotes.Add("d8-4", "<:d8_4:1346988286446407832>");
            DiscordEmotes.Add("d8-5", "<:d8_5:1346988297658044447>");
            DiscordEmotes.Add("d8-6", "<:d8_6:1346988310983086161>");
            DiscordEmotes.Add("d8-7", "<:d8_7:1346988324065251448>");
            DiscordEmotes.Add("d8-8", "<:d8_8:1346988341429665873>");
            DiscordEmotes.Add("d8gold-1", "<:d8gold1:1346988354687864905>");
            DiscordEmotes.Add("d8gold-2", "<:d8gold2:1346988374287843429>");
            DiscordEmotes.Add("d8gold-3", "<:d8gold3:1346988384651841587>");
            DiscordEmotes.Add("d8gold-4", "<:d8gold4:1346988400460431511>");
            DiscordEmotes.Add("d8gold-5", "<:d8gold5:1346988412812529784>");
            DiscordEmotes.Add("d8gold-6", "<:d8gold6:1346988424640335923>");
            DiscordEmotes.Add("d8gold-7", "<:d8gold7:1346988440138547321>");
            DiscordEmotes.Add("d8gold-8", "<:d8gold8:1346988454185013258>");
            DiscordEmotes.Add("db-d10-1", "<:d10_1:1376317484579291288>");
            DiscordEmotes.Add("db-d10-2", "<:d10_2:1376317507077410826>");
            DiscordEmotes.Add("db-d10-3", "<:d10_3:1376317529152159745>");
            DiscordEmotes.Add("db-d10-4", "<:d10_4:1376317556696027176>");
            DiscordEmotes.Add("db-d10-5", "<:d10_5:1376317579983061132>");
            DiscordEmotes.Add("db-d10-6", "<:d10_6:1376317602489565304>");
            DiscordEmotes.Add("db-d10-7", "<:d10_7:1376317622341210242>");
            DiscordEmotes.Add("db-d10-8", "<:d10_8:1376317641303785493>");
            DiscordEmotes.Add("db-d10-9", "<:d10_9:1376317659741814906>");
            DiscordEmotes.Add("db-d10-10", "<:d10_10:1376317680348299374>");
            DiscordEmotes.Add("db-d10-0", "<:d10_0:1376317459279384596>");
            DiscordEmotes.Add("db-d10gold-1", "<:d10gold1:1376317826192638082>");
            DiscordEmotes.Add("db-d10gold-2", "<:d10gold2:1376317853095038986>");
            DiscordEmotes.Add("db-d10gold-3", "<:d10gold3:1376317881964433460>");
            DiscordEmotes.Add("db-d10gold-4", "<:d10gold4:1376317905653858425>");
            DiscordEmotes.Add("db-d10gold-5", "<:d10gold5:1376317936976920606>");
            DiscordEmotes.Add("db-d10gold-6", "<:d10gold6:1376317959995396207>");
            DiscordEmotes.Add("db-d10gold-7", "<:d10gold7:1376317983294619769>");
            DiscordEmotes.Add("db-d10gold-8", "<:d10gold8:1376318002273718314>");
            DiscordEmotes.Add("db-d10gold-9", "<:d10gold9:1376318024654520320>");
            DiscordEmotes.Add("db-d10gold-10", "<:d10gold10:1376318046582472765>");
            DiscordEmotes.Add("db-d10gold-0", "<:d10gold0:1376317804462084107>");
            //DiscordEmotes.Add("dbredd10-0", "<:TenDice0:1345917112341893120>");
            //DiscordEmotes.Add("dbredd10-1", "<:TenDice1:1345917147754401885>");
            //DiscordEmotes.Add("dbredd10-2", "<:TenDice2:1345917172962299986>");
            //DiscordEmotes.Add("dbredd10-3", "<:TenDice3:1345917202313777212>");
            //DiscordEmotes.Add("dbredd10-4", "<:TenDice4:1345917227291119698>");
            //DiscordEmotes.Add("dbredd10-5", "<:TenDice5:1345917253874614305>");
            //DiscordEmotes.Add("dbredd10-6", "<:TenDice6:1345917278402904064>");
            //DiscordEmotes.Add("dbredd10-7", "<:TenDice7:1345917302192738354>");
            //DiscordEmotes.Add("dbredd10-8", "<:TenDice8:1345917328155480199>");
            //DiscordEmotes.Add("dbredd10-9", "<:TenDice9:1345917352977502295>");
            //DiscordEmotes.Add("dbredd10-10", "<:TenDice10:1345917375408766976>");
            //DiscordEmotes.Add("dbgoldd10-0", "<:TenDiceGold0:1345917402457833482>");
            //DiscordEmotes.Add("dbgoldd10-1", "<:TenDiceGold1:1345917427862470666>");
            //DiscordEmotes.Add("dbgoldd10-2", "<:TenDiceGold2:1345919034796806144>");
            //DiscordEmotes.Add("dbgoldd10-3", "<:TenDiceGold3:1345919062181416980>");
            //DiscordEmotes.Add("dbgoldd10-4", "<:TenDiceGold4:1345919795383636038>");
            //DiscordEmotes.Add("dbgoldd10-5", "<:TenDiceGold5:1345919818221486102>");
            //DiscordEmotes.Add("dbgoldd10-6", "<:TenDiceGold6:1345919842884259900>");
            //DiscordEmotes.Add("dbgoldd10-7", "<:TenDiceGold7:1345919863125704825>");
            //DiscordEmotes.Add("dbgoldd10-8", "<:TenDiceGold8:1345919881077456948>");
            //DiscordEmotes.Add("dbgoldd10-9", "<:TenDiceGold9:1345919905483984907>");
            //DiscordEmotes.Add("dbgoldd10-10", "<:TenDiceGold10:1345919928250794115>");
            DiscordEmotes.Add("d12-1", "<:d12_1:1376319331771875458>");
            DiscordEmotes.Add("d12-2", "<:d12_2:1376319356396503080>");
            DiscordEmotes.Add("d12-3", "<:d12_3:1376319376872968382>");
            DiscordEmotes.Add("d12-4", "<:d12_4:1376319399346307173>");
            DiscordEmotes.Add("d12-5", "<:d12_5:1376319422570041525>");
            DiscordEmotes.Add("d12-6", "<:d12_6:1376319451754008587>");
            DiscordEmotes.Add("d12-7", "<:d12_7:1376319474478612591>");
            DiscordEmotes.Add("d12-8", "<:d12_8:1376319503708848260>");
            DiscordEmotes.Add("d12-9", "<:d12_9:1376319524735025244>");
            DiscordEmotes.Add("d12-10", "<:d12_10:1376319548520792236>");
            DiscordEmotes.Add("d12-11", "<:d12_11:1376319591059558410>");
            DiscordEmotes.Add("d12-12", "<:d12_12:1376319611460390993>");
            DiscordEmotes.Add("d12gold-1", "<:d12gold1:1376319788460015707>");
            DiscordEmotes.Add("d12gold-2", "<:d12gold2:1376319809737986068>");
            DiscordEmotes.Add("d12gold-3", "<:d12gold3:1376319831174938655>");
            DiscordEmotes.Add("d12gold-4", "<:d12gold4:1376319854902116372>");
            DiscordEmotes.Add("d12gold-5", "<:d12gold5:1376319878159401011>");
            DiscordEmotes.Add("d12gold-6", "<:d12gold6:1376319899374452827>");
            DiscordEmotes.Add("d12gold-7", "<:d12gold7:1376319925299183656>");
            DiscordEmotes.Add("d12gold-8", "<:d12gold8:1376319945960329237>");
            DiscordEmotes.Add("d12gold-9", "<:d12gold9:1376319967581966367>");
            DiscordEmotes.Add("d12gold-10", "<:d12gold10:1376319990860611594>");
            DiscordEmotes.Add("d12gold-11", "<:d12gold11:1376320015845822634>");
            DiscordEmotes.Add("d12gold-12", "<:d12gold12:1376320057252253816>");
            DiscordEmotes.Add("d20-1", "<:d20_1:1346988471646162944>");
            DiscordEmotes.Add("d20-2", "<:d20_2:1346988486888259585>");
            DiscordEmotes.Add("d20-3", "<:d20_3:1346988501144436798>");
            DiscordEmotes.Add("d20-4", "<:d20_4:1346988520794750976>");
            DiscordEmotes.Add("d20-5", "<:d20_5:1346988535881797712>");
            DiscordEmotes.Add("d20-6", "<:d20_6:1346988557902020700>");
            DiscordEmotes.Add("d20-7", "<:d20_7:1346988581599580244>");
            DiscordEmotes.Add("d20-8", "<:d20_8:1346988599874420736>");
            DiscordEmotes.Add("d20-9", "<:d20_9:1346988613350723635>");
            DiscordEmotes.Add("d20-10", "<:d20_10:1346988627829330011>");
            DiscordEmotes.Add("d20-11", "<:d20_11:1346988642887008326>");
            DiscordEmotes.Add("d20-12", "<:d20_12:1346988657344778250>");
            DiscordEmotes.Add("d20-13", "<:d20_13:1346988675338338315>");
            DiscordEmotes.Add("d20-14", "<:d20_14:1346988689221357710>");
            DiscordEmotes.Add("d20-15", "<:d20_15:1346988704123584562>");
            DiscordEmotes.Add("d20-16", "<:d20_16:1346988727309963304>");
            DiscordEmotes.Add("d20-17", "<:d20_17:1346988745622159441>");
            DiscordEmotes.Add("d20-18", "<:d20_18:1346988765012557896>");
            DiscordEmotes.Add("d20-19", "<:d20_19:1346988779650547815>");
            DiscordEmotes.Add("d20-20", "<:d20_20:1346988796650197072>");
            DiscordEmotes.Add("d20gold-1", "<:d20gold1:1346988812756324362>");
            DiscordEmotes.Add("d20gold-2", "<:d20gold2:1346988825901273139>");
            DiscordEmotes.Add("d20gold-3", "<:d20gold3:1346988839612452915>");
            DiscordEmotes.Add("d20gold-4", "<:d20gold4:1346988855932223579>");
            DiscordEmotes.Add("d20gold-5", "<:d20gold5:1346988869375098880>");
            DiscordEmotes.Add("d20gold-6", "<:d20gold6:1346988883975475280>");
            DiscordEmotes.Add("d20gold-7", "<:d20gold7:1346988897191858327>");
            DiscordEmotes.Add("d20gold-8", "<:d20gold8:1346988909397016667>");
            DiscordEmotes.Add("d20gold-9", "<:d20gold9:1346988922621661198>");
            DiscordEmotes.Add("d20gold-10", "<:d20gold10:1346988934575554560>");
            DiscordEmotes.Add("d20gold-11", "<:d20gold11:1346988948953632910>");
            DiscordEmotes.Add("d20gold-12", "<:d20gold12:1346988963461595156>");
            DiscordEmotes.Add("d20gold-13", "<:d20gold13:1346988978330538004>");
            DiscordEmotes.Add("d20gold-14", "<:d20gold14:1346988992783978560>");
            DiscordEmotes.Add("d20gold-15", "<:d20gold15:1346989005614616636>");
            DiscordEmotes.Add("d20gold-16", "<:d20gold16:1346989021653504081>");
            DiscordEmotes.Add("d20gold-17", "<:d20gold17:1346989036983554048>");
            DiscordEmotes.Add("d20gold-18", "<:d20gold18:1346989053983199242>");
            DiscordEmotes.Add("d20gold-19", "<:d20gold19:1346989065928704150>");
            DiscordEmotes.Add("d20gold-20", "<:d20gold20:1346989078280933547>");
            DiscordEmotes.Add("dbheads", "<:dbheads:1346304474788139058>");
            DiscordEmotes.Add("dbtails", "<:dbtails:1346304489795227703>");
            DiscordEmotes.Add("dbhighroll1", "<:HighrollGameIcons1d:1346262689646903357>");
            DiscordEmotes.Add("dbhighroll2", "<:HighrollGameIcons2d:1346262871545479338>");
            DiscordEmotes.Add("dbkingsgame1b", "<:KingsGame1b:1346262584449437696>");
            DiscordEmotes.Add("dbkingsgame2b", "<:KingsGame2b:1346262613792653412>");
            DiscordEmotes.Add("dbliardice1", "<:LiarsDice1:1346262912314114209>");
            DiscordEmotes.Add("dbliardice2", "<:LiarsDice2:1346262934895988746>");
            DiscordEmotes.Add("dbmafia1", "<:Mafia1:1346262959176810496>");
            DiscordEmotes.Add("dbmafia2", "<:Mafia2:1346262991506509886>");
            DiscordEmotes.Add("dbpoker1", "<:Poker1:1346263030916190238>");
            DiscordEmotes.Add("dbpoker2", "<:Poker2:1346263061115441203>");
            DiscordEmotes.Add("dbpoker3", "<:Poker3:1346263092157222943>");
            DiscordEmotes.Add("dbroulette1", "<:RouletteN1:1346263548023804057>");
            DiscordEmotes.Add("dbroulette2", "<:RouletteN2:1346263575525851188>");
            DiscordEmotes.Add("dbrps1", "<:rockpaperscissors1:1346263475860803734>");
            DiscordEmotes.Add("dbrps2", "<:rockpaperscissors2:1346263504302116865>");
            DiscordEmotes.Add("dbslamroll1", "<:SlamRoll1:1346263614075437230>");
            DiscordEmotes.Add("dbslamroll2", "<:SlamRoll2:1346263643209076766>");
            DiscordEmotes.Add("dbalpha1", "<:AlphaRoyale1:1456295964225699861>");
            DiscordEmotes.Add("dbalpha2", "<:AlphaRoyale2:1456295984056500234>");
            DiscordEmotes.Add("dbslota", "<:SlotWatermelon:1346263873711505489>");
            DiscordEmotes.Add("dbslotb", "<:SlotBanana:1346263699543035977>");
            DiscordEmotes.Add("dbslotc", "<:SlotDiamond:1346263779503243274>");
            DiscordEmotes.Add("dbslotd", "<:SlotCherry:1346263729385242688>");
            DiscordEmotes.Add("dbslote", "<:SlotCoin:1346263755679469739>");
            DiscordEmotes.Add("dbslotf", "<:Slot7:1346263670442954863>");
            DiscordEmotes.Add("dbslotg", "<:SlotPineapple:1346263836352839792>");
            DiscordEmotes.Add("dbslota2", "<:SlotsRidingCrop:1346264193636110357>");
            DiscordEmotes.Add("dbslotb2", "<:SlotsHandcuff:1346264127387078776>");
            DiscordEmotes.Add("dbslotc2", "<:SlotsBallgag:1346264009015300179>");
            DiscordEmotes.Add("dbslotd2", "<:SlotsBlindfold:1346264036190191657>");
            DiscordEmotes.Add("dbslote2", "<:SlotsChastityCage:1346264064531103966>");
            DiscordEmotes.Add("dbslotf2", "<:SlotsCollar:1346264099591426152>");
            DiscordEmotes.Add("dbslotg2", "<:SlotsPlug:1346264157971939388>");
            DiscordEmotes.Add("dbslots1", "<:Slots1:1346263940149284924>");
            DiscordEmotes.Add("dbslots2", "<:Slots2:1346263971165896825>");
            DiscordEmotes.Add("potionblue1", "<:PotionBlue:1346303188961067119>");
            DiscordEmotes.Add("potionblue2", "<:PotionBlue2:1346303206606508033>");
            DiscordEmotes.Add("potiongreen1", "<:PotionGreen:1346303222981328906>");
            DiscordEmotes.Add("potiongreen2", "<:PotionGreen2:1346303236583198801>");
            DiscordEmotes.Add("potiongreen3", "<:PotionGreen3:1346303247022948502>");
            DiscordEmotes.Add("potiongreen4", "<:PotionGreen4:1346303257521295423>");
            DiscordEmotes.Add("potionorange1", "<:PotionOrange1:1346303268409573386>");
            DiscordEmotes.Add("potionorange2", "<:PotionOrange2:1346303279746908160>");
            DiscordEmotes.Add("potionorange3", "<:PotionOrange3:1346303292568764536>");
            DiscordEmotes.Add("potionpink1", "<:PotionPink1:1346303306611560529>");
            DiscordEmotes.Add("potionpink2", "<:PotionPink2:1346303320708481105>");
            DiscordEmotes.Add("potionpurple1", "<:PotionPurple1:1346303335774425210>");
            DiscordEmotes.Add("potionpurple2", "<:PotionPurple2:1346303347245711412>");
            DiscordEmotes.Add("potionred1", "<:PotionRed:1346303368192069672>");
            DiscordEmotes.Add("potionred2", "<:PotionRed2:1346303382301970463>");
            DiscordEmotes.Add("potionred3", "<:PotionRed3:1346303394029244501>");
            DiscordEmotes.Add("potionred4", "<:PotionRed4:1346303404850544744>");
            DiscordEmotes.Add("potionredvase1", "<:PotionVase1:1346303415717728337>");
            DiscordEmotes.Add("potionredvase2", "<:PotionVase2:1346303427189411890>");
            DiscordEmotes.Add("potion1", "<:potion1:1346306236840153088>");
            DiscordEmotes.Add("potion2", "<:potion2:1346306248794050638>");
            DiscordEmotes.Add("potion3", "<:potion3:1346306258893930518>");
            DiscordEmotes.Add("potion4", "<:potion4:1346306268372926505>");
            DiscordEmotes.Add("potion5", "<:potion5:1346306284768464996>");
            DiscordEmotes.Add("potion6", "<:potion6:1346306292725317642>");
            DiscordEmotes.Add("potion7", "<:potion7:1346306302330146968>");
            DiscordEmotes.Add("potion8", "<:potion8:1346306313201651763>");
            DiscordEmotes.Add("potion9", "<:potion9:1346306323855310919>");
            DiscordEmotes.Add("potion10", "<:potion10:1346306334957506633>");
            DiscordEmotes.Add("potion11", "<:potion11:1346306344780562463>");
            DiscordEmotes.Add("potion12", "<:potion12:1346306353966092338>");
            DiscordEmotes.Add("potion13", "<:potion13:1346306363784957972>");
            DiscordEmotes.Add("potion14", "<:potion14:1346306373196972055>");

            DiscordEmotes.Add("bbishopd", "<:BBishopD:1346265690797117440>");
            DiscordEmotes.Add("bbishopl", "<:BBishopL:1346265704961413150>");
            DiscordEmotes.Add("bkingd", "<:BKingD:1346265730706046977>");
            DiscordEmotes.Add("bkingl", "<:BKingL:1346265745234985071>");
            DiscordEmotes.Add("bknightd", "<:BKnightD:1346265770551672932>");
            DiscordEmotes.Add("bknightl", "<:BKnightL:1346265783034056754>");
            DiscordEmotes.Add("bpawnd", "<:BPawnD:1346265820052979783>");
            DiscordEmotes.Add("bpawnl", "<:BPawnL:1346265839518744606>");
            DiscordEmotes.Add("bqueend", "<:BQueenD:1346267193800790069>");
            DiscordEmotes.Add("bqueenl", "<:BQueenL:1346267209663643678>");
            DiscordEmotes.Add("brookd", "<:BRookD:1346267261681406012>");
            DiscordEmotes.Add("brookl", "<:BRookL:1346267279553204246>");
            DiscordEmotes.Add("squared", "<:SquareD:1346267318958952479>");
            DiscordEmotes.Add("squarel", "<:SquareL:1346267396268232808>");
            DiscordEmotes.Add("wbishopd", "<:WBishopD:1346267473745412097>");
            DiscordEmotes.Add("wbishopl", "<:WBishopL:1346267496914747402>");
            DiscordEmotes.Add("wkingd", "<:WKingD:1346267545161830504>");
            DiscordEmotes.Add("wkingl", "<:WKingL:1346267558340329512>");
            DiscordEmotes.Add("wknightd", "<:WKnightD:1346267594545561642>");
            DiscordEmotes.Add("wknightl", "<:WKnightL:1346267612996305007>");
            DiscordEmotes.Add("wpawnd", "<:WPawnD:1346267646223450163>");
            DiscordEmotes.Add("wpawnl", "<:WPawnL:1346267661109301350>");
            DiscordEmotes.Add("wqueend", "<:WQueenD:1346267706466238484>");
            DiscordEmotes.Add("wqueenl", "<:WQueenL:1346267722350198854>");
            DiscordEmotes.Add("wrookd", "<:WRookD:1346267750603030568>");
            DiscordEmotes.Add("wrookl", "<:WRookL:1346267764481855700>");

            DiscordEmotes.Add("cardjb", "<:bjoker:1346269689076256849>");
            DiscordEmotes.Add("cardjr", "<:rjoker:1346269708537823362>");
            DiscordEmotes.Add("cardh1", "<:heartace:1346272890223333386>");
            DiscordEmotes.Add("cardh2", "<:heart2:1346272919856087081>");
            DiscordEmotes.Add("cardh3", "<:heart3:1346272936977502229>");
            DiscordEmotes.Add("cardh4", "<:heart4:1346272952823451678>");
            DiscordEmotes.Add("cardh5", "<:heart5:1346272975162314752>");
            DiscordEmotes.Add("cardh6", "<:heart6:1346273028018929706>");
            DiscordEmotes.Add("cardh7", "<:heart7:1346273042707382372>");
            DiscordEmotes.Add("cardh8", "<:heart8:1346273062487720006>");
            DiscordEmotes.Add("cardh9", "<:heart9:1346273113666486423>");
            DiscordEmotes.Add("cardh10", "<:heart10:1346273134285946943>");
            DiscordEmotes.Add("cardh11", "<:heartJ:1346273171829030933>");
            DiscordEmotes.Add("cardh12", "<:heartQ:1346273385402994759>");
            DiscordEmotes.Add("cardh13", "<:heartK:1346273413563416647>");
            DiscordEmotes.Add("cardd1", "<:diamondace:1346272552535719966>");
            DiscordEmotes.Add("cardd2", "<:diamond2:1346272567706648709>");
            DiscordEmotes.Add("cardd3", "<:diamond3:1346272585066741815>");
            DiscordEmotes.Add("cardd4", "<:diamond4:1346272601420464149>");
            DiscordEmotes.Add("cardd5", "<:diamond5:1346272613403590749>");
            DiscordEmotes.Add("cardd6", "<:diamond6:1346272626888413268>");
            DiscordEmotes.Add("cardd7", "<:diamond7:1346272647117410375>");
            DiscordEmotes.Add("cardd8", "<:diamond8:1346272659993919599>");
            DiscordEmotes.Add("cardd9", "<:diamond9:1346272681049325609>");
            DiscordEmotes.Add("cardd10", "<:diamond10:1346272696110944438>");
            DiscordEmotes.Add("cardd11", "<:diamondJ:1346272716151590922>");
            DiscordEmotes.Add("cardd12", "<:diamondQ:1346272738636988519>");
            DiscordEmotes.Add("cardd13", "<:diamondK:1346272752419471411>");
            DiscordEmotes.Add("cardc1", "<:clubAce:1346269729786036245>");
            DiscordEmotes.Add("cardc2", "<:club2:1346269741463109774>");
            DiscordEmotes.Add("cardc3", "<:club3:1346269752829673502>");
            DiscordEmotes.Add("cardc4", "<:club4:1346269770147696650>");
            DiscordEmotes.Add("cardc5", "<:club5:1346269785742119019>");
            DiscordEmotes.Add("cardc6", "<:club6:1346269796731195503>");
            DiscordEmotes.Add("cardc7", "<:club7:1346269807003176970>");
            DiscordEmotes.Add("cardc8", "<:club8:1346269817312903179>");
            DiscordEmotes.Add("cardc9", "<:club9:1346269831074156554>");
            DiscordEmotes.Add("cardc10", "<:club10:1346269838712246292>");
            DiscordEmotes.Add("cardc11", "<:clubJ:1346269849306792018>");
            DiscordEmotes.Add("cardc12", "<:clubQ:1346269862837747823>");
            DiscordEmotes.Add("cardc13", "<:clubK:1346269880609017936>");
            DiscordEmotes.Add("cardp1", "<:SpadeAce:1346273449303343174>");
            DiscordEmotes.Add("cardp2", "<:Spade2:1346273463353999411>");
            DiscordEmotes.Add("cardp3", "<:Spade3:1346273477879009343>");
            DiscordEmotes.Add("cardp4", "<:Spade4:1346273491657424916>");
            DiscordEmotes.Add("cardp5", "<:Spade5:1346273504722685962>");
            DiscordEmotes.Add("cardp6", "<:Spade6:1346273520501391461>");
            DiscordEmotes.Add("cardp7", "<:Spade7:1346273534380478484>");
            DiscordEmotes.Add("cardp8", "<:Spade8:1346273550088146956>");
            DiscordEmotes.Add("cardp9", "<:Spade9:1346273568664584265>");
            DiscordEmotes.Add("cardp10", "<:Spade10:1346273583348846673>");
            DiscordEmotes.Add("cardp11", "<:SpadeJ:1346273599052320819>");
            DiscordEmotes.Add("cardp12", "<:SpadeQ:1346273617822089216>");
            DiscordEmotes.Add("cardp13", "<:SpadeK:1346273634037268661>");

            DiscordEmotes.Add("dbdungeon1", "<:dbdungeon1:1512238859394486273>");
            DiscordEmotes.Add("dbdungeon2", "<:dbdungeon2:1512238860384342076>");
            DiscordEmotes.Add("dbacidtrap", "<:dbacidtrap:1512238224322334843>");
            DiscordEmotes.Add("dbbladetrap", "<:dbbladetrap:1512238225479958558>");
            DiscordEmotes.Add("dbcamp", "<:dbcamp:1512238285777272833>");
            DiscordEmotes.Add("dbcockatrice", "<:dbcockatrice:1512238287463518258>");
            DiscordEmotes.Add("dbcrushingwalltrap", "<:dbcrushingwalltrap:1512238289036378174>");
            DiscordEmotes.Add("dbdemon", "<:dbdemon:1512238290273570837>");
            DiscordEmotes.Add("dbdisintegrationtrap", "<:dbdisintegrationtrap:1512238336427950203>");
            DiscordEmotes.Add("dbdragon", "<:dbdragon:1512238337618874490>");
            DiscordEmotes.Add("dbemptyroom", "<:dbemptyroom:1512238339166703717>");
            DiscordEmotes.Add("dbgiantbeast", "<:dbgiantbeast:1512238341066850354>");
            DiscordEmotes.Add("dbgiantmushroom", "<:dbgiantmushroom:1512238390060257300>");
            DiscordEmotes.Add("dbgiantsnake", "<:dbgiantsnake:1512238391339651252>");
            DiscordEmotes.Add("dbgoldengolem", "<:dbgoldengolem:1512238394338709755>");
            DiscordEmotes.Add("dbgremlinhorde", "<:dbgremlinhorde:1512238432959856700>");
            DiscordEmotes.Add("dbharpy", "<:dbharpy:1512238433857310912>");
            DiscordEmotes.Add("dbminotaur", "<:dbminotaur:1512238436025892894>");
            DiscordEmotes.Add("dborcgang", "<:dborcgang:1512238437661413427>");
            DiscordEmotes.Add("dbpittrap", "<:dbpittrap:1512238496146915328>");
            DiscordEmotes.Add("dbpoisonfrog", "<:dbpoisonfrog:1512238497170329630>");
            DiscordEmotes.Add("dbpoisongastrap", "<:dbpoisongastrap:1512238498399125765>");
            DiscordEmotes.Add("dbshadowwarrior", "<:dbshadowwarrior:1512238500219719700>");
            DiscordEmotes.Add("dbshop", "<:dbshop:1512238538861707375>");
            DiscordEmotes.Add("dbskeletonknight", "<:dbskeletonknight:1512238540195369101>");
            DiscordEmotes.Add("dbtreasureroom", "<:dbtreasureroom:1512238545060888787>");
            DiscordEmotes.Add("dbgiantworm", "<:dbgiantworm:1512238392895737937>");
            DiscordEmotes.Add("dbwraith", "<:dbwraith:1512238546134765768>");
            //todo: card eicons
        }



        //public static string OpenFormat(FormatType format, ChatRoom room)
        //{
        //    switch(format)
        //    {
        //        case FormatType.Bold:
        //            if (room == ChatRoom.FChat)
        //                return "[b]";
        //            else if (room == ChatRoom.Discord)
        //                return "**";
        //            break;
        //        case FormatType.Italic:
        //            if (room == ChatRoom.FChat)
        //                return "[i]";
        //            else if (room == ChatRoom.Discord)
        //                return "_";
        //            break;
        //        case FormatType.Strikethrough:
        //            if (room == ChatRoom.FChat)
        //                return "[s]";
        //            else if (room == ChatRoom.Discord)
        //                return "~~";
        //            break;
        //        case FormatType.Underline:
        //            if (room == ChatRoom.FChat)
        //                return "[u]";
        //            else if (room == ChatRoom.Discord)
        //                return "__";
        //            break;
        //        case FormatType.Subtext:
        //            if (room == ChatRoom.FChat)
        //                return "[sub]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.Supertext:
        //            if (room == ChatRoom.FChat)
        //                return "[sup]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorBlue:
        //            if (room == ChatRoom.FChat)
        //                return "[color=blue]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorYellow:
        //            if (room == ChatRoom.FChat)
        //                return "[color=yellow]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorRed:
        //            if (room == ChatRoom.FChat)
        //                return "[color=red]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorGreen:
        //            if (room == ChatRoom.FChat)
        //                return "[color=green]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorGray:
        //            if (room == ChatRoom.FChat)
        //                return "[color=gray]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorBlack:
        //            if (room == ChatRoom.FChat)
        //                return "[color=black]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.Spoiler:
        //            if (room == ChatRoom.FChat)
        //                return "[spoiler]";
        //            else if (room == ChatRoom.Discord)
        //                return "||";
        //            break;
        //        case FormatType.UserIcon:
        //            if (room == ChatRoom.FChat)
        //                return "[icon]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.UserTag:
        //            if (room == ChatRoom.FChat)
        //                return "[user]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //    }
        //    return "^?o^";
        //}


        //public static string CloseFormat(FormatType format, ChatRoom room)
        //{
        //    switch (format)
        //    {
        //        case FormatType.Bold:
        //            if (room == ChatRoom.FChat)
        //                return "[/b]";
        //            else if (room == ChatRoom.Discord)
        //                return "**";
        //            break;
        //        case FormatType.Italic:
        //            if (room == ChatRoom.FChat)
        //                return "[/i]";
        //            else if (room == ChatRoom.Discord)
        //                return "_";
        //            break;
        //        case FormatType.Strikethrough:
        //            if (room == ChatRoom.FChat)
        //                return "[/s]";
        //            else if (room == ChatRoom.Discord)
        //                return "~~";
        //            break;
        //        case FormatType.Underline:
        //            if (room == ChatRoom.FChat)
        //                return "[/u]";
        //            else if (room == ChatRoom.Discord)
        //                return "__";
        //            break;
        //        case FormatType.Subtext:
        //            if (room == ChatRoom.FChat)
        //                return "[/sub]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.Supertext:
        //            if (room == ChatRoom.FChat)
        //                return "[/sup]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorBlue:
        //            if (room == ChatRoom.FChat)
        //                return "[/color]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorYellow:
        //            if (room == ChatRoom.FChat)
        //                return "[/color]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorRed:
        //            if (room == ChatRoom.FChat)
        //                return "[/color]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorGreen:
        //            if (room == ChatRoom.FChat)
        //                return "[/color]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorGray:
        //            if (room == ChatRoom.FChat)
        //                return "[/color]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.ColorBlack:
        //            if (room == ChatRoom.FChat)
        //                return "[/color]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.Spoiler:
        //            if (room == ChatRoom.FChat)
        //                return "[/spoiler]";
        //            else if (room == ChatRoom.Discord)
        //                return "||";
        //            break;
        //        case FormatType.UserIcon:
        //            if (room == ChatRoom.FChat)
        //                return "[/icon]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //        case FormatType.UserTag:
        //            if (room == ChatRoom.FChat)
        //                return "[/user]";
        //            else if (room == ChatRoom.Discord)
        //                return "";
        //            break;
        //    }
        //    return "^?c^";
        //}

    }

    public enum FormatType
    {
        Bold,
        Italic,
        Strikethrough,
        Underline,
        Subtext,
        Supertext,
        ColorBlue,
        ColorYellow,
        ColorRed,
        ColorGreen,
        ColorGray,
        ColorBlack,
        Spoiler,
        UserIcon,
        UserTag

    }

    public enum ChatRoom
    {
        None,
        FChat,
        Discord,
        Html
    }
}
