using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class PotionGenerator
    {
        public Random Random;

        //Notes about potion rarity for printout::
        //public const double Common = 2, Uncommon = 1, Rare = .33, UltraRare = .02; //but these are more of a range...
        //if (enchantment.Rarity > 1)
        //    rarity = "Common";
        //else if (enchantment.Rarity == 1)
        //    rarity = "[color=green]Uncommon[/color]";
        //else if (enchantment.Rarity > .2 && enchantment.Rarity < 1)
        //    rarity = "[color=blue]Rare[/color]";
        //else if (enchantment.Rarity > .05 && enchantment.Rarity <= .2)
        //    rarity = "[color=purple]Ultra-Rare[/color]";
        //else if (enchantment.Rarity <= .05)

        public PotionGenerator(Random r)
        {
            Random = r;

            LoadPotionGenerationInfo();
        }

        private void LoadPotionGenerationInfo()
        {
            LoadPotionGenerationFromDisk(BotMain.FileFolder, BotMain.PotionGenerationFileName);
        }

        private void LoadPotionGenerationFromDisk(string fileFolder, string fileName)
        {
            string path = Utils.GetTotalFileName(fileFolder, fileName);
            try
            {
                BotMain.VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if(BotMain._debug)
                        Console.WriteLine("LoadPotionGenerationFromDisk file text " + fileText == null ? "null" : fileText.Take(100) + "...");

                    PotionGenerationInfo generationInfo = JsonConvert.DeserializeObject<PotionGenerationInfo>(fileText);

                    //AllColors = generationInfo.AllColors;
                    //AllAdjectives = generationInfo.AllAdjectives;
                    //AllPotionNouns = generationInfo.AllPotionNouns;
                    //AllEicons = generationInfo.AllEicons;
                    //AllFlavors = generationInfo.AllFlavors;
                    //AllOrigins = generationInfo.AllOrigins;
                    AllTriggerWords = generationInfo.AllTriggerWords;
                    AllEnchantments = generationInfo.AllEnchantments;
                    CommonEnchantments = generationInfo.CommonEnchantments;

                    if (BotMain._debug)
                        Console.WriteLine("loaded PotionGenerationInfo successfully.");
                }
                else
                {
                    Console.WriteLine("PotionGenerationInfo file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load PotionGenerationInfo for " + path + "\n" + exc.ToString());
            }
        }

        private List<string> AllColors = new List<string>() { 
            "white", "pink", "blue", "gold", "silver", 
            "green", "purple", "orange", "red", "brown", 
            "tan", "teal", "peach", "coral", "yellow", "seafoam", 
            "rainbow", "bronze", "purple", "hazel", "lilac", 
            "black", "grail mud", "pearl", "lavender", "fuschia", "prussian blue", "burgundy",
            "olive", "platinum", "maroon", "beige", "lime green", "olive green",
            "dark blue", "light blue", "dark gray", "gray", "light gray", "dark green", 
            "light green", "dark orange", "light orange", "dark red",
            "light red", "dark brown", "light brown", "dark pink",
            "light pink", "dark yellow", "light yellow", "dark purple",
            "light purple", "dark teal", "light teal"};

        private List<string> AllAdjectives = new List<string>() {
            "swirling","glowing","bland-looking","spotted","bright",
            "suspicious","clear","colorful","rainbow-streaked","clumpy",
            "muddy","stale","fizzing","fresh","lovely",
            "gleaming","fragile","unstable","old","new",
            "gorgeous","fancy","elegant","inexpensive","rich",
            "light","odd","odorous","long","short",
            "small","open","smoky","striped","tasty",
            "huge","greasy"
        };

        private List<string> AllPotionNouns = new List<string>() {
            "potion","potion","potion","potion","potion",
            "vial","vial","elixir","elixir","flask","flask",
            //"mug","shot glass","glass","pudding",
            "oil","tonic","serum"
        };

        private List<string> AllEicons = new List<string>() {
            "potion1","potion2","potion3","potion4","potion5",
            "potion6","potion7","potion8","potion9","potion10",
            "potion11","potion12","potion13","potion14",
            "potionred1","potionred2","potionred3","potionred4","potionpink1",
            "potiongreen1","potiongreen2","potiongreen3","potiongreen4","potionpink2",
            "potionpurple1","potionpurple2","potionorange1","potionorange2","potionorange3",
            "potionblue1","potionblue2","potionvase1","potionvase2"
        };

        private List<string> AllFlavors = new List<string>() {
            "chocolate","strawberry","steak","banana","apple",
            "cotton candy","cola","milk","sprite","orange",
            "coffee","pumpkin","maple","vanilla","tofu",
            "pumpkin spice","candy cane","caramel apple","caramel corn","nutmeg",
            "pecan pie","eggnog","turkey","deviled eggs","sweet potato",
            "chicken","bubblegum","mead","honey","cheesecake",
            "pineapple","grapefruit","grape","raisin","mud",
            "boogers","saliva","sardines","salmon","burnt toast",
            "pine wood","spoiled milk","plain yogurt","mineral water"
        };

        private List<string> AllOrigins = new List<string>() {
            "Potion generated!","Please take this potion.","Potion created.","I brewed this for you.","You'll want this one.",
            "Use with caution.","I think the formula was right.","This one is a favorite.","I'm not sure where this one came from.","Not another one of these...",
            "Potions are easy.","Maybe one day I'll run out of these.","Hopefully this helps.","You can thank me later.","A potion for all seasons.",
            "Don't tell anyone you got it here.","Another freebie, eh?","Potion ready~", "Potion here!","Tell me how this one feels.",
            "I got this one on sale.","Potion acquired.","Oh good, you want it.","This is what I had lying around.","Another button push, another potion.",
            "Made with love.","Take this, I have too many.","Potion generated.","Your potion has arrived."
        };

        private List<string> AllTriggerWords = new List<string>();

        private List<Enchantment> AllEnchantments = new List<Enchantment>();

        private List<Enchantment> CommonEnchantments = new List<Enchantment>();

        public List<Enchantment> GetAllEnchantments(BotMain botMain, bool includeCustom, string channel)
        {
            if(includeCustom)
            {
                var potionos = botMain.GetChannelPotions(channel);
                List<Enchantment> enchantments = new List<Enchantment>(AllEnchantments);
                enchantments.AddRange(potionos);
                return enchantments;
            }
            else
            {
                return AllEnchantments;
            }
        }

        public List<Enchantment> GetCommonEnchantments()
        {
            return CommonEnchantments;
        }

        public List<string> GetTriggerWords()
        {
            return AllTriggerWords;
        }

        private Potion GeneratePotionBase()
        {
            Potion potion = new Potion("potion", ItemCategory.Potion, 1, 1);

            potion.strength = GetRandomStrength(Random);
            potion.printFormat = Random.Next(8);
            potion.color = GetRandomColor(Random);
            potion.adjective = GetRandomAdjective(Random);
            potion.potionNoun = GetRandomPotionNoun(Random);

            potion.flavor = GetRandomFlavor(Random);
            potion.eicon = GetRandomEicon(Random);
            potion.RandomSeed = Random.Next(Int32.MaxValue - 1) + 1;

            return potion;
        }

        public Potion GeneratePotionWithSpecificEffect(List<Enchantment> channelPotions, bool allowFlavor, bool allowLewd, bool requireLewd, string potionEffectSearch)
        {
            Potion potion = GeneratePotionBase();

            Enchantment specific = GetSpecificEnchantment(channelPotions, potionEffectSearch);

            potion.enchantment = specific;
            if (specific != null && !string.IsNullOrEmpty(specific.OverrideEicon))
                potion.eicon = specific.OverrideEicon;

            return potion;
        }

        public Enchantment GetSpecificEnchantment(List<Enchantment> channelPotions, string potionEffectSearch)
        {
            Enchantment found = channelPotions == null ? null : channelPotions.FirstOrDefault(a => a.prefix.ToLower() == potionEffectSearch || a.suffix.ToLower() == potionEffectSearch);

            return found == null? AllEnchantments.FirstOrDefault(a => a.prefix.ToLower() == potionEffectSearch || a.suffix.ToLower() == potionEffectSearch) : found;
        }

        public Potion GeneratePotion(List<Enchantment> channelPotions, bool useDefaultPotions, bool allowFlavor, bool allowLewd, bool requireLewd)
        {
            Potion potion = GeneratePotionBase();

            potion.enchantment = GetRandomEnchantment(Random, channelPotions, useDefaultPotions, allowFlavor, allowLewd, requireLewd);

            if (potion.enchantment != null && !string.IsNullOrEmpty(potion.enchantment.OverrideEicon))
                potion.eicon = potion.enchantment.OverrideEicon;

            return potion;
        }

        public string GetPotionGenerationOutputString(Potion p, bool includeOrigin)
        {
            if (p == null)
                return "Error: Potion was null.";
            System.Random thisRandom = new System.Random(p.RandomSeed > 0 ? p.RandomSeed : Random.Next(100000));

            String potionString = p.ToString();
            String returnString = SubstituteWords(potionString, thisRandom);
            if (p.enchantment == null)
                return returnString;
            String originString = GetRandomOrigin(thisRandom);
            returnString = (includeOrigin? originString + "\n" : "") + returnString;
            return returnString;
        }


        private int GetRandomStrength(System.Random Random)
        {
            double seed = Random.NextDouble();
            int strength = 1;
            if (seed <= .3)
                strength = 1; //30%
            else if (seed <= .6)
                strength = 2; //30%
            else if (seed <= .8)
                strength = 3; //20%
            else if (seed <= .93)
                strength = 4; //13%
            else
                strength = 5; //7%
            return strength;
        }

        public string SubstituteWords(string input, System.Random random)
        {
            string randomWord = GetRandomWord(random);
            string randomColor = GetRandomColor(random);
            string randomColor2 = GetUniqueRandomColor(random, new List<string>() { randomColor });
            string randomFlavor = GetRandomFlavor(random);

            string returnString = input.Replace("#Color#", randomColor)
                    .Replace("#Color2#", randomColor2)
                    .Replace("#Flavor#", randomFlavor)
                    .Replace("#Word#", randomWord);
            if (input.Contains("#Word2#"))
            {
                string randomWord2 = GetRandomWord(random);
                string randomWord3 = GetRandomWord(random);
                string randomWord4 = GetRandomWord(random);
                string randomWord5 = GetRandomWord(random);
                string randomWord6 = GetRandomWord(random);
                string randomWord7 = GetRandomWord(random);
                string randomWord8 = GetRandomWord(random);
                string randomWord9 = GetRandomWord(random);
                string randomWord10 = GetRandomWord(random);
                returnString = returnString.Replace("#Word2#", randomWord2).Replace("#Word3#", randomWord3).Replace("#Word4#", randomWord4)
                    .Replace("#Word5#", randomWord5).Replace("#Word6#", randomWord6).Replace("#Word7#", randomWord7).Replace("#Word8#", randomWord8)
                    .Replace("#Word9#", randomWord9).Replace("#Word10#", randomWord10);
            }

            return returnString;
        }

        public string GetUniqueRandomColor(Random r, List<string> existing)
        {
            string newColor = GetRandomColor(r);
            int safety = 100;
            while(existing.Contains(newColor) && safety > 0)
            {
                safety--;
                newColor = GetRandomColor(r);
            }
            return newColor;
        }

        public string GetRandomColor(Random r)
        {
            return AllColors[r.Next(AllColors.Count)];
        }

        public string GetRandomAdjective(Random r)
        {
            return AllAdjectives[r.Next(AllAdjectives.Count)];
        }

        public string GetRandomEicon(Random r)
        {
            return AllEicons[r.Next(AllEicons.Count)];
        }

        public string GetRandomPotionNoun(Random r)
        {
            return AllPotionNouns[r.Next(AllPotionNouns.Count)];
        }

        public string GetRandomFlavor(Random r)
        {
            return AllFlavors[r.Next(AllFlavors.Count)];
        }

        public string GetRandomWord(Random r)
        {
            return AllTriggerWords[r.Next(AllTriggerWords.Count)];
        }

        public string GetRandomOrigin(Random r)
        {
            return AllOrigins[r.Next(AllOrigins.Count)];
        }

        public Enchantment GetRandomEnchantment(Random r, List<Enchantment> channelPotion, bool useDefaultPotions, bool allowFlavor, bool allowLewd, bool requireLewd)
        {
            Enchantment first = GetRandomEnchantment(r, channelPotion, useDefaultPotions, allowLewd, requireLewd);
            int safety = 0;

            while (!allowFlavor && first.suffix.ToLower().Contains("flavor"))
            {
                safety++;
                first = GetRandomEnchantment(r, channelPotion, useDefaultPotions, allowLewd, requireLewd);
            }

            return first;
        }

        private Enchantment GetRandomEnchantment(Random r, List<Enchantment> channelPotions, bool useDefaultPotions, bool allowKinky, bool requireKinky)
        {
            double seed = r.NextDouble();

            List<Enchantment> relevantCommonEnchantments = new List<Enchantment>(CommonEnchantments);
            List<Enchantment> relevantAllEnchantments = new List<Enchantment>(AllEnchantments);

            if (!useDefaultPotions)
            {
                relevantCommonEnchantments = new List<Enchantment>();
                relevantAllEnchantments = new List<Enchantment>();
            }
            if(channelPotions != null)
            {
                relevantAllEnchantments.AddRange(channelPotions);
            }

            if (requireKinky)
            {
                relevantCommonEnchantments = relevantCommonEnchantments.Where(ba => ba.kinky).ToList();
                relevantAllEnchantments = relevantAllEnchantments.Where(ba => ba.kinky).ToList();
            }
            if (!allowKinky)
            {
                relevantCommonEnchantments = relevantCommonEnchantments.Where(ba => !ba.kinky).ToList();
                relevantAllEnchantments = relevantAllEnchantments.Where(ba => !ba.kinky).ToList();
            }

            if(relevantAllEnchantments.Count() == 0)
            {
                return new Enchantment(false, "Watery", "of Water", "This is just some water... What happened? [sub](no matching potions found)[/sub]");
            }

            if (seed <= .33 && useDefaultPotions) //was .40
            {
                return relevantCommonEnchantments[r.Next(relevantCommonEnchantments.Count)];
            }
            else
            {
                Enchantment xxx = relevantAllEnchantments[r.Next(relevantAllEnchantments.Count)];

                while (xxx.Rarity < 1)
                {
                    double roll = r.NextDouble();
                    if (roll <= xxx.Rarity)
                        break;

                    xxx = relevantAllEnchantments[r.Next(relevantAllEnchantments.Count)];
                }

                return xxx;
            }
        }
    }


    public class PotionGenerationInfo
    {
        public List<string> AllColors;
        public List<string> AllAdjectives;
        public List<string> AllPotionNouns;
        public List<string> AllEicons;
        public List<string> AllFlavors;
        public List<string> AllOrigins;
        public List<string> AllTriggerWords;
        public List<Enchantment> AllEnchantments;
        public List<Enchantment> CommonEnchantments;

    }
}
