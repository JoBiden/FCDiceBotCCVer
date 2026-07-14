using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// Manages system-granted titles based on user achievements.
    /// Check after interactions, work, and volunteer activities.
    /// </summary>
    public static class ChateauSystemTitles
    {
        /// <summary>
        /// Check if a user has earned any new system titles and grant them.
        /// Returns a string with notification messages for any newly earned titles, or empty string if none.
        /// </summary>
        /// <param name="userName">The user to check for new titles</param>
        /// <returns>Notification text for earned titles, or empty string</returns>
        public static string CheckAndGrantTitles(string userName)
        {
            var grant = CheckAndGrantCountTitles(userName);
            if (grant == null) return string.Empty;
            return FormatTitleNotification(grant.DisplayName, grant.NewTitles);
        }

        /// <summary>
        /// Grant any newly-earned count / daily-climax titles for the user and return them as a
        /// <see cref="InteractionProcessors.GroupTitleGrant"/> (display name + the new title
        /// texts), or null if none were earned. This lets the group-resolution path fold a
        /// participant's count-title wins into the same consolidated "Title Time!" banner as the
        /// group-achievement titles, instead of emitting a separate per-person banner.
        /// <see cref="CheckAndGrantTitles"/> wraps this for the 1:1 / single-actor callers that
        /// just want the formatted string.
        /// </summary>
        public static InteractionProcessors.GroupTitleGrant CheckAndGrantCountTitles(string userName)
        {
            Profile profile = MonDB.getProfile(userName);
            if (profile == null) return null;

            List<string> newTitles = new List<string>();

            // Check all title criteria
            newTitles.AddRange(CheckInteractionCountTitles(profile));
            newTitles.AddRange(CheckDailyClimaxTitles(profile));

            // Future: Add more title check methods here as we implement them
            // newTitles.AddRange(CheckBondTitles(profile));
            // newTitles.AddRange(CheckEmploymentTitles(profile));
            // newTitles.AddRange(CheckTransformationTitles(profile));

            if (newTitles.Count == 0) return null;

            // Save profile with new titles.
            MonDB.setProfile(userName, profile);

            return new InteractionProcessors.GroupTitleGrant
            {
                UserName = userName,
                DisplayName = profile.displayName,
                NewTitles = newTitles,
            };
        }

        /// <summary>
        /// Check for titles earned by performing the same interaction multiple times
        /// </summary>
        private static List<string> CheckInteractionCountTitles(Profile profile)
        {
            List<string> earnedTitles = new List<string>();

            // Initialize titles list if null
            if (profile.titles == null)
            {
                profile.titles = new List<Title>();
            }

            // Define interaction count milestones
            // Format: (countKey, threshold, titleText)
            var interactionMilestones = new List<(string countKey, int threshold, string titleText)>
            {
                // CASUAL INTERACTION titles

                // Kissing titles
                ("kiss", 1, "Does this mean I'm pregnant?"),
                ("kiss", 10, "Kisser"),
                ("kiss", 50, "Cutie Kisser"),
                ("kiss", 100, "Kiss Connoisseur"),
                ("kiss", 500, "Continuous Kisser"),
                
                // Cuddling titles
                ("cuddle", 10, "Cuddler"),
                ("cuddle", 50, "Cutie Cuddler"),
                ("cuddle", 100, "Cuddle Puddle Champion"),
                ("cuddle", 500, "Constant Cuddler"),
                
                // Handholding titles
                ("handhold", 1, "Does this mean we're married?"),
                ("handhold", 10, "Hand Holder"),
                ("handhold", 50, "Caught Cooties"),
                ("handhold", 100, "Firmly Grasps It"),
                ("handhold", 500, "Won't Let Go"),
                
                // Spanking - given
                ("spankgive", 10, "Spanker"),
                ("spankgive", 50, "Swift Spanker"),
                ("spankgive", 100, "Blusher of Butts"),
                ("spankgive", 500, "Turkey Cooker"),
                
                // Spanking - taken
                ("spanktake", 1, "[eicon]once[/eicon]"),
                ("spanktake", 10, "Spanked"),
                ("spanktake", 50, "Sore Butt"),
                ("spanktake", 100, "Red Bottom"),
                ("spanktake", 500, "Buns of Steel"),
                
                // Bullying - given
                ("bullygive", 10, "Bully"),
                ("bullygive", 50, "Big Bully"),
                ("bullygive", 100, "Bigger Bully"),
                ("bullygive", 500, "Biggest Bully"),
                
                // Bullying - taken
                ("bullytake", 10, "Bullied"),
                ("bullytake", 50, "Victim"),
                ("bullytake", 100, "Asking For It"),
                ("bullytake", 500, "Bully Bait"),

                // Lapsit - giving (the sitter, on top)
                ("lapsitgive", 1, "Will Sit On You"),
                ("lapsitgive", 10, "Supported"),
                ("lapsitgive", 50, "S is for Sitter"),
                ("lapsitgive", 100, "Lap Pet"),
                ("lapsitgive", 500, "Disciple of Rin"),

                // Lapsit - taking (the lap, on bottom)
                ("lapsittake", 1, "Lap"),
                ("lapsittake", 10, "Supportive"),
                ("lapsittake", 50, "Chair"),
                ("lapsittake", 100, "Comfy"),
                ("lapsittake", 500, "Throne"),

                // Boobhat - giving (the chest providing the hat)
                ("boobhatgive", 1, "Boo(b)"),
                ("boobhatgive", 10, "Good Hat"),
                ("boobhatgive", 50, "Umbrella"),
                ("boobhatgive", 100, "Speaks With Their Chest"),
                ("boobhatgive", 500, "Heavy is the Crown"),

                // Boobhat - taking (the head wearing the hat)
                ("boobhattake", 1, "Guess Who?"),
                ("boobhattake", 10, "Nice Hat"),
                ("boobhattake", 50, "Boobrest"),
                ("boobhattake", 100, "Didn't Skip Neck Day"),
                ("boobhattake", 500, "Heavy is the Head"),

                // Lick - giving (the licker)
                ("lickgive", 1, ":P"),
                ("lickgive", 10, "Mlem"),
                ("lickgive", 50, "Talented Tongue"),
                ("lickgive", 100, "Licksalot"),
                ("lickgive", 500, "Got to the Center"),

                // Lick - taking (the licked)
                ("licktake", 1, "Licked"),
                ("licktake", 10, "Tasty"),
                ("licktake", 50, "Salt Lick"),
                ("licktake", 100, "Spit Shined"),
                ("licktake", 500, "Lollipop"),

                // Pet - giving (the one petting)
                ("petgive", 1, "Pat, Pat"),
                ("petgive", 10, "Petter"),
                ("petgive", 50, "Head Patter"),
                ("petgive", 100, "Pet Whisperer"),
                ("petgive", 500, "Gives The Best Scritches"),

                // Pet - taking (the one being petted)
                ("pettake", 1, "Good Pet"),
                ("pettake", 10, "Petted"),
                ("pettake", 50, "Headpat Enjoyer"),
                ("pettake", 100, "Very Good Pet"),
                ("pettake", 500, "Purrs On Command"),

                // INVOLVED INTERACTION titles

                // Milking - giving
                ("milkgive", 1, "Milker"),
                ("milkgive", 5, "Can Milk Those"),
                ("milkgive", 10, "Might Milk You"),
                ("milkgive", 25, "Master Milker"),

                // Milking - taking
                ("milktake", 1, "Mini Milk"),
                ("milktake", 5, "Mega Milk"),
                ("milktake", 10, "Giga Milk"),
                ("milktake", 25, "Cow"),

                // Payment Give - giving
                ("paymentGivegive", 1, "Not Broke"),
                ("paymentGivegive", 5, "Frequent Buyer"),
                ("paymentGivegive", 10, "Makes Money Talk"),
                ("paymentGivegive", 25, "Distributor of Wealth"),

                // Payment Give - taking
                ("paymentGivetake", 5, "Fortunate"),
                ("paymentGivetake", 10, "Beneficiary"),
                ("paymentGivetake", 25, "Swimming in Money"),

                // Payment Receive - giving
                ("paymentReceivegive", 5, "Demanding"),
                ("paymentReceivegive", 10, "Tab Keeper"),
                ("paymentReceivegive", 25, "Collects What Is Owed"),

                // Payment Receive - taking
                ("paymentReceivetake", 1, "Good For It"),
                ("paymentReceivetake", 5, "Payed Their Fair Share"),
                ("paymentReceivetake", 10, "Might Still Be In Debt"),
                ("paymentReceivetake", 25, "Out Of Milk Money"),

                // Feeding - giving
                ("feedgive", 5, "Can Cook"),
                ("feedgive", 10, "Provider"),
                ("feedgive", 25, "Happy In The Kitchen"),

                // Feeding - taking
                ("feedtake", 5, "Sampler"),
                ("feedtake", 10, "Big Appetite"),
                ("feedtake", 25, "Bulking"),

                // Golden - giving
                ("goldengive", 1, "Answered Nature's Call"),
                ("goldengive", 5, "Gotta Go Sometime"),
                ("goldengive", 10, "Didn't Piss [i]Themselves[/i]"),
                ("goldengive", 25, "Might Pee On You"),

                // Golden - taking
                ("goldentake", 5, "Yellow"),
                ("goldentake", 10, "Golden"),
                ("goldentake", 25, "Urinal"),
                
                // Pledges - active
                ("pledgesactive", 1, "Made A Promise"),
                ("pledgesactive", 5, "Will Do It Later"),
                ("pledgesactive", 10, "Will Do It Eventually"),
                ("pledgesactive", 25, "Lazy"),

                // Pledges - fulfilled
                ("pledgesfulfilled", 1, "Kept A Promise"),
                ("pledgesfulfilled", 5, "Did It"),
                ("pledgesfulfilled", 10, "Trustworthy"),
                ("pledgesfulfilled", 25, "Whose Word Is Law"),

                // Pledges - abandoned
                ("pledgesabandoned", 1, "Broke Their Promise"),
                ("pledgesabandoned", 5, "Didn't Do It"),
                ("pledgesabandoned", 10, "Gave Up"),
                ("pledgesabandoned", 25, "Chopped A Cherry Tree In A Past Life"),

                // Dressup - giving
                ("dressupgive", 5, "Good Eye"),
                ("dressupgive", 10, "Fashionable"),
                ("dressupgive", 25, "Trendsetter"),

                // Dressup - taking
                ("dressuptake", 1, "Not Naked"),
                ("dressuptake", 5, "All Dressed Up"),
                ("dressuptake", 10, "Full Wardrobe"),
                ("dressuptake", 25, "Never Out Of Style"),

                // Climax - giving (the partner role: helped someone else climax via
                // !climaxfor with you as the recipient, or !climax with you as the initiator)
                ("climaxgive", 5, "Generous Lover"),
                ("climaxgive", 10, "Cum Collector"),
                ("climaxgive", 25, "CliMAX"),

                // Climax - taking (the climaxer role: the person actually orgasming, via
                // !climaxfor as initiator, !climax as recipient, or any self-target)
                ("climaxtake", 1, "Drank The Juice"),
                ("climaxtake", 5, "Cum Again?"),
                ("climaxtake", 10, "Came Buckets"),
                ("climaxtake", 25, "Keeps On Cumming"),

                // Bond
                ("bond", 1, "New In Town"),
                ("bond", 5, "Friendly"),
                ("bond", 10, "Well Connected"),
                ("bond", 25, "Socialite"),


                // COMMITMENT INTERACTION titles

                // Marking - giving
                ("markgive", 1, "Left Their Mark"),
                ("markgive", 2, "Practically A Chain"),
                ("markgive", 5, "Popular Brand"),
                ("markgive", 10, "Mark It Leader"),

                // Marking - taking
                ("marktake", 1, "Marked"),
                ("marktake", 2, "Twice Claimed"),
                ("marktake", 5, "Mark Collector"),
                ("marktake", 10, "Sticker Book"),

                // Consuming - giving
                ("consumegive", 1, "Devourer"),
                ("consumegive", 2, "Went Back For Seconds"),
                ("consumegive", 3, "Room For More"),
                ("consumegive", 5, "Insatiable"),
                ("consumegive", 9, "Ate Nine"),

                // Consuming - taking
                ("consumetake", 1, "Devoured"),
                ("consumetake", 2, "Leftovers"),
                ("consumetake", 5, "Renewable Resource"),
                ("consumetake", 10, "Ended World Hunger"),

                // Petrifying - giving
                ("petrifygive", 1, "Petrifying"),
                ("petrifygive", 3, "Sculptor?"),
                ("petrifygive", 5, "Zen Gardener"),
                ("petrifygive", 10, "Honorary Medusa"),

                // Petrifying - taking
                ("petrifytake", 1, "Petrified"),
                ("petrifytake", 3, "Rolling Stone"),
                ("petrifytake", 5, "Weeping Angel"),
                ("petrifytake", 10, "Already Rock Hard"),

                // Planting - giving
                ("plantgive", 1, "Planter"),
                ("plantgive", 3, "Gardener"),
                ("plantgive", 5, "Green Thumb"),
                ("plantgive", 10, "Horticulturist"),
                ("plantgive", 41, "Ultimate Gardener"),

                // Planting - taking
                ("planttake", 1, "Just A Plant"),
                ("planttake", 3, "Fruitful"),
                ("planttake", 5, "Might Be A Weed"),
                ("planttake", 10, "Invasive Species"),

                // Objectifying - giving
                ("objectifygive", 1, "Objectifier"),
                // "Hat Trick" moved to the boobhat group-size titles; the objectify tier-3
                // title is now "Toy Maker" (see GetGroupSizeTitles below).
                ("objectifygive", 3, "Toy Maker"),
                ("objectifygive", 5, "Deanimator"),
                ("objectifygive", 10, "Collector"),

                // Objectifying - taking
                ("objectifytake", 1, "Just An Object"),
                ("objectifytake", 3, "Transformer"),
                ("objectifytake", 5, "Utility Knife"),
                ("objectifytake", 10, "Solid"),

                // Entitling - giving
                ("entitlegive", 1, "Nominator"),
                ("entitlegive", 3, "Rule Of Three"),
                ("entitlegive", 5, "Calls It Like They See It"),
                ("entitlegive", 10, "[eicon]Oprah[/eicon]"),

                // Entitling - taking
                ("entitletake", 1, "Entitled"),
                ("entitletake", 3, "Certified"),
                ("entitletake", 5, "Everyone's Favorite"),
                ("entitletake", 10, "Has Too Many Titles To Reasonably Display On One Dossier, Especially With This Exceptionally Long And Sesquipedalian Title Taking Up So Much Space"),

                // Breeding - giving
                ("breedgive", 1, "Proud Father"),
                ("breedgive", 3, "Stud"),
                ("breedgive", 5, "Might Be A Bard"),
                ("breedgive", 10, "Superior Genes"),

                // Breeding - taking
                ("breedtake", 1, "Proud Mother"),
                ("breedtake", 3, "Another In The Oven"),
                ("breedtake", 5, "Brooding"),
                ("breedtake", 8, "Octomom"),

                // Employing - giving
                ("employgive", 1, "Employer"),
                ("employgive", 3, "Has Three Stooges"),
                ("employgive", 5, "Head Of The Team"),
                ("employgive", 10, "Business Is Booming"),

                // Employing - taking
                ("employtake", 1, "Employed"),
                ("employtake", 3, "Jobhopper"),
                ("employtake", 5, "Many Hats"),
                ("employtake", 10, "Jack Of All Trades"),

                // Training - giving
                ("traingive", 1, "Tutor"),
                ("traingive", 5, "Teacher"),
                ("traingive", 10, "Mentor"),
                ("traingive", 100, "Fountain Of Knowledge"),

                // Training - taking
                ("traintake", 1, "Trained"),
                ("traintake", 5, "Well Trained"),
                ("traintake", 10, "Student"),
                ("traintake", 100, "Beginner's Mind"),

                //needs additional rows to handle purifying
                // Corrupting - giving
                ("corruptgive", 1, "Bad Influence"),
                ("corruptgive", 2, "Fools Them Twice"),
                ("corruptgive", 5, "Diabolical"),
                ("corruptgive", 10, "Source Of Corruption"),

                // Corrupting - taking
                ("corrupttake", 1, "Tasted Darkness"),
                ("corrupttake", 3, "Tainted"),
                ("corrupttake", 5, "Corrupted"),
                ("corrupttake", 10, "Past The Point Of No Return"),


                // CONSEQUENCE INTERACTION titles

                // Monsterizing - giving
                ("monsterizegive", 1, "Transformative"),
                ("monsterizegive", 5, "Maker Of Monsters"),
                ("monsterizegive", 10, "Understands The Assignment"),

                // Monsterizing - taking
                ("monsterizetake", 1, "Monstrous"),
                ("monsterizetake", 2, "Malleable"),
                ("monsterizetake", 10, "Shapeshifter"),

                // Infesting - giving
                ("infestgive", 1, "Infestor"),
                ("infestgive", 3, "Down With The Sickness"),
                ("infestgive", 10, "Plaguebearer"),

                // Infesting - taking
                ("infesttake", 1, "Infested"),
                ("infesttake", 2, "Crosscontaminated"),
                ("infesttake", 3, "Kodoku"),

                // Renaming - giving
                ("renamegive", 1, "Nomenclator"),
                ("renamegive", 3, "Good With Names"),
                ("renamegive", 10, "Will Call You Whatever They Like"),

                // Renaming - taking
                ("renametake", 1, "Fresh Start"),
                ("renametake", 2, "A Rose By Any Other Name"),
                ("renametake", 5, "Might As Well Be Anonymous"),
                ("renametake", 10, "Burner Identity"),

                // Odorizing - giving
                ("odorizegive", 1, "Perfumer"),
                ("odorizegive", 5, "Musky"),
                ("odorizegive", 10, "Addictive Scent"),

                // Odorizing - taking
                ("odorizetake", 1, "Needs A Shower"),
                ("odorizetake", 5, "Stinky"),
                ("odorizetake", 10, "Reeks"),

                // Cursing - giving
                ("cursegive", 1, "Hexer"),
                ("cursegive", 5, "And Your Little Dog Too"),
                ("cursegive", 10, "Might Curse Your Entire Bloodline"),

                // Cursing - taking
                ("cursetake", 1, "Cursed"),
                ("cursetake", 2, "Twice Cursed"),
                ("cursetake", 3, "Curse Magnet"),

                // Breaking - giving
                ("breakgive", 1, "Wrecker"),
                ("breakgive", 2, "Did I Do That?"),
                ("breakgive", 5, "Health Hazard"),
                ("breakgive", 10, "In A World Of Glass"),

                // Breaking - taking
                ("breaktake", 1, "Broken"),
                ("breaktake", 2, "Sleep It Off"),
                ("breaktake", 5, "Superglue Won't Fix That"),
                ("breaktake", 10, "Please Stop, They're Already Dead"),

                // Dosing - giving
                ("dosegive", 1, "Gateway Drug"),
                ("dosegive", 5, "Illicit Dealer"),
                ("dosegive", 10, "Has You Hooked"),

                // Dosing - taking
                ("dosetake", 1, "Drugged"),
                ("dosetake", 10, "High"),
                ("dosetake", 25, "High As A Kite"),
                ("dosetake", 50, "Might Be Addicted"),
                ("dosetake", 75, "Almost Definitely Addicted"),
                ("dosetake", 100, "Addicted"),
                ("dosetake", 200, "Duuuuuude"),
                ("dosetake", 420, "Blaze It"),

                // Future interactions can be added here as they're implemented
            };

            // Check each milestone
            foreach (var milestone in interactionMilestones)
            {
                // Check if user has reached this threshold
                if (profile.counts.ContainsKey(milestone.countKey) &&
                    profile.counts[milestone.countKey] >= milestone.threshold)
                {
                    // Check if they already have this title
                    bool alreadyHasTitle = profile.titles.Any(t =>
                        t.IsSystemTitle &&
                        t.titleText.Equals(milestone.titleText, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyHasTitle)
                    {
                        // Grant the title
                        Title newTitle = new Title
                        {
                            titleText = milestone.titleText,
                            givenBy = "Chateau",
                            grantedTime = DateTime.UtcNow
                        };
                        profile.titles.Add(newTitle);
                        earnedTitles.Add(milestone.titleText);
                    }
                }
            }

            return earnedTitles;
        }

        /// <summary>
        /// Check for titles earned by climaxing many times within the same Chateau day.
        /// Unlike the lifetime-count titles, these gate on today's entry in
        /// <see cref="Profile.dailyClimaxCounts"/> (set by ClimaxforProcessor for the
        /// person actually orgasming). Each tier is awarded once-ever; reaching the
        /// threshold again on a later day surfaces flavor in the completion text but
        /// does not re-award the title.
        /// </summary>
        private static List<string> CheckDailyClimaxTitles(Profile profile)
        {
            List<string> earnedTitles = new List<string>();

            if (profile.dailyClimaxCounts == null) return earnedTitles;
            if (profile.titles == null)
            {
                profile.titles = new List<Title>();
            }

            string todayKey = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            if (!profile.dailyClimaxCounts.TryGetValue(todayKey, out int todayCount)) return earnedTitles;

            var dailyMilestones = new List<(int threshold, string titleText)>
            {
                (3,  "Rule of Three"),
                (5,  "Can Go All Night"),
                (10, "Inhuman Stamina"),
            };

            foreach (var milestone in dailyMilestones)
            {
                if (todayCount < milestone.threshold) continue;

                bool alreadyHasTitle = profile.titles.Any(t =>
                    t.IsSystemTitle &&
                    t.titleText.Equals(milestone.titleText, StringComparison.OrdinalIgnoreCase));
                if (alreadyHasTitle) continue;

                profile.titles.Add(new Title
                {
                    titleText = milestone.titleText,
                    givenBy = "Chateau",
                    grantedTime = DateTime.UtcNow,
                });
                earnedTitles.Add(milestone.titleText);
            }

            return earnedTitles;
        }

        // ====================================================================
        // Group-achievement titles. Unlike the lifetime-count milestones above,
        // these are awarded once at a group interaction's resolution, based on
        // the size of the resolved group (or, for lapsit, a participant's stack
        // position). Thresholds are cumulative: a resolved group of a given size
        // grants every tier at or below it. The grant path lives in
        // InteractionProcessorBase.GrantGroupTitles / LapsitProcessor; these
        // tables and lookups are the single source of truth for the text.
        // ====================================================================

        /// <summary>
        /// Group-size titles keyed by the processor's InteractionType. Each tier is
        /// (minGroupSize, title) where group size counts every participant including the
        /// initiator. Symmetric interactions (kiss / cuddle / handhold) award these to every
        /// participant; directional one-way ones (spank / bully / lick / boobhat) award them
        /// to the initiator only. Boobhat's "Hat Trick" is the former objectifygive tier-3
        /// title, reassigned here.
        /// </summary>
        private static readonly Dictionary<string, (int size, string title)[]> GroupSizeTitleTables =
            new Dictionary<string, (int size, string title)[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["kiss"] = new[]
                {
                    (3, "Kiss Kiss"),
                    (5, "Kisstacular"),
                    (7, "Kissimanjaro"),
                    (9, "Kisspocalypse"),
                    (11, "Spartakiss"),
                },
                ["handhold"] = new[]
                {
                    (3, "Triangle"),
                    (5, "Pentagon"),
                    (7, "Septagon"),
                    (9, "Nonagon"),
                    (11, "Undecagon"),
                },
                ["cuddle"] = new[]
                {
                    (3, "Cuddle Puddle"),
                    (5, "Cuddle Pond"),
                    (7, "Cuddle Lake"),
                    (9, "Cuddle Sea"),
                    (11, "Cuddle Ocean"),
                },
                ["spank"] = new[]
                {
                    (3, "Echo"),
                    (5, "Thunder Storm"),
                    (8, "Seven Spanks"),
                    (11, "Strike!"),
                },
                ["bully"] = new[]
                {
                    (3, "Intimidating"),
                    (5, "Hazing"),
                    (7, "Demands Respect"),
                    (9, "Unbullyvable"),
                    (11, "Decibully"),
                },
                ["lick"] = new[]
                {
                    (3, "Free Sample"),
                    (5, "Indecisive"),
                    (7, "Can Tie A Knot In A Cherry Stem"),
                    (9, "Tongue In Chic"),
                    (11, "Lick A Ton"),
                },
                ["boobhat"] = new[]
                {
                    (4, "Hat Trick"),
                    (7, "Mad Hatter"),
                    (11, "Capping"),
                },
                ["pet"] = new[]
                {
                    (3, "Petting Zoo"),
                    (5, "Menagerie"),
                    (7, "Herder"),
                    (9, "Pack Leader"),
                    (11, "Whole Kennel"),
                },
            };

        /// <summary>
        /// Lapsit "below" titles: earned for the number of people stacked underneath a
        /// participant (their stack position). Cumulative, like the size tables.
        /// </summary>
        private static readonly (int othersBelow, string title)[] LapsitBelowTitles = new[]
        {
            (2, "Elevated"),
            (4, "Laps All The Way Down"),
            (6, "Can See Their House From Here"),
            (8, "King Of The World"),
            (10, "Lap of Babel"),
        };

        /// <summary>
        /// Lapsit "above" titles: earned for the number of people stacked on top of a
        /// participant. Cumulative. A mid-stack participant can earn both below and above.
        /// </summary>
        private static readonly (int othersAbove, string title)[] LapsitAboveTitles = new[]
        {
            (2, "Shortstacked"),
            (5, "Buried"),
            (10, "Foundational"),
        };

        /// <summary>
        /// Title texts earned by participating in a resolved group of the given size for the
        /// given interaction type — every cumulative tier at or below <paramref name="groupSize"/>,
        /// in ascending order. Empty when the type has no group titles or the group is below
        /// the first threshold. Pure lookup; does not touch the database.
        /// </summary>
        public static IReadOnlyList<string> GetGroupSizeTitles(string interactionType, int groupSize)
        {
            var earned = new List<string>();
            if (string.IsNullOrEmpty(interactionType)) return earned;
            if (!GroupSizeTitleTables.TryGetValue(interactionType, out var tiers)) return earned;

            foreach (var tier in tiers)
            {
                if (groupSize >= tier.size) earned.Add(tier.title);
            }
            return earned;
        }

        /// <summary>
        /// Title texts earned by a single lap-stack position: cumulative "below" titles for the
        /// number of people stacked under this participant, then cumulative "above" titles for
        /// the number stacked over them. A mid-stack participant can earn both. Pure lookup.
        /// </summary>
        public static IReadOnlyList<string> GetLapsitPositionTitles(int othersBelow, int othersAbove)
        {
            var earned = new List<string>();
            foreach (var tier in LapsitBelowTitles)
            {
                if (othersBelow >= tier.othersBelow) earned.Add(tier.title);
            }
            foreach (var tier in LapsitAboveTitles)
            {
                if (othersAbove >= tier.othersAbove) earned.Add(tier.title);
            }
            return earned;
        }

        /// <summary>
        /// Format the notification message for newly earned titles. Public so the group
        /// resolution path (which grants by size / lap-stack position rather than lifetime
        /// counts) can surface the same "Title Time!" banner for its awards.
        /// </summary>
        public static string FormatTitleNotification(string displayName, List<string> titles)
        {
            if (titles.Count == 0) return string.Empty;

            string message = "\n\n[b][color=purple]═══ Title Time! ═══[/color][/b]\n";

            if (titles.Count == 1)
            {
                message += $"{displayName} has earned the title [b]·{titles[0]}·[/b]!";
            }
            else
            {
                message += $"{displayName} has earned {titles.Count} new titles:\n";
                foreach (string title in titles)
                {
                    message += $"[b]·{title}·[/b]\n";
                }
                message = message.TrimEnd('\n');
            }

            message += "\n[sub]View all your titles with !titles[/sub]";

            return message;
        }

        /// <summary>
        /// Consolidate a resolved group's title grants into a single "Title Time!" banner: one
        /// line per distinct newly-earned title, naming everyone who earned it with a serial-comma
        /// list ("Alice" / "Alice and Bob" / "Alice, Bob, and Carol"). Titles are listed in
        /// first-earned order across <paramref name="grants"/>; names within a title keep grant
        /// order. Returns empty string when nobody earned anything new. Grants may mix
        /// group-achievement titles and (folded in by the caller) lifetime-count titles — a
        /// participant can appear in several grant entries; grouping by title merges them.
        /// </summary>
        public static string FormatGroupTitleNotification(IReadOnlyList<InteractionProcessors.GroupTitleGrant> grants)
        {
            if (grants == null) return string.Empty;

            // Invert to title -> earners, preserving first-appearance order for both.
            var orderedTitles = new List<string>();
            var earnersByTitle = new Dictionary<string, List<string>>();
            foreach (var grant in grants)
            {
                if (grant?.NewTitles == null) continue;
                foreach (string title in grant.NewTitles)
                {
                    if (!earnersByTitle.TryGetValue(title, out var earners))
                    {
                        earners = new List<string>();
                        earnersByTitle[title] = earners;
                        orderedTitles.Add(title);
                    }
                    // Defensive: a title shouldn't be earned twice by the same name, but guard.
                    if (!earners.Contains(grant.DisplayName)) earners.Add(grant.DisplayName);
                }
            }

            if (orderedTitles.Count == 0) return string.Empty;

            string message = "\n\n[b][color=purple]═══ Title Time! ═══[/color][/b]\n";
            foreach (string title in orderedTitles)
            {
                string names = InteractionProcessors.InteractionProcessorBase.JoinNamesSerial(earnersByTitle[title]);
                message += $"{names} earned the title [b]·{title}·[/b]!\n";
            }
            message = message.TrimEnd('\n');
            message += "\n[sub]View all your titles with !titles[/sub]";

            return message;
        }

        /// <summary>
        /// Manually grant a system title (for mod/admin use or special events)
        /// </summary>
        public static bool GrantSystemTitle(string userName, string titleText)
        {
            Profile profile = MonDB.getProfile(userName);
            if (profile == null) return false;

            // Initialize titles list if null
            if (profile.titles == null)
            {
                profile.titles = new List<Title>();
            }

            // Check if they already have this system title
            bool alreadyHasTitle = profile.titles.Any(t =>
                t.IsSystemTitle &&
                t.titleText.Equals(titleText, StringComparison.OrdinalIgnoreCase));

            if (alreadyHasTitle) return false;

            // Grant the title
            Title newTitle = new Title
            {
                titleText = titleText,
                givenBy = "Chateau",
                grantedTime = DateTime.UtcNow
            };
            profile.titles.Add(newTitle);
            MonDB.setProfile(userName, profile);

            return true;
        }

        /// <summary>
        /// Check if a user has a specific system title
        /// </summary>
        public static bool HasSystemTitle(string userName, string titleText)
        {
            Profile profile = MonDB.getProfile(userName);
            if (profile == null || profile.titles == null) return false;

            return profile.titles.Any(t =>
                t.IsSystemTitle &&
                t.titleText.Equals(titleText, StringComparison.OrdinalIgnoreCase));
        }
    }
}
