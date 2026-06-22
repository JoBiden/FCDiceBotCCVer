using FChatDicebot.BotCommands;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Unit tests for the B6 employer-earnings feature: the ChateauWork.EmployerCut /
    /// ApplyEmployerKickback kickback helpers and the ChateauBusiness.BuildBusiness render
    /// helper. All pure — none touch the database.
    /// </summary>
    public class ChateauBusinessTests
    {
        // --- EmployerCut: 25% of the rolled reward, floored, min 1 when positive ---

        [Theory]
        [InlineData(0, 0)]
        [InlineData(-5, 0)]
        [InlineData(1, 1)]   // floor(0.25) = 0 -> min 1
        [InlineData(2, 1)]   // floor(0.50) = 0 -> min 1
        [InlineData(3, 1)]   // floor(0.75) = 0 -> min 1
        [InlineData(4, 1)]   // floor(1.00) = 1
        [InlineData(7, 1)]   // floor(1.75) = 1
        [InlineData(8, 2)]   // floor(2.00) = 2
        [InlineData(100, 25)]
        public void EmployerCut_AppliesFloorAndMinimum(int rewardAmount, int expectedCut)
        {
            Assert.Equal(expectedCut, ChateauWork.EmployerCut(rewardAmount));
        }

        // --- ApplyEmployerKickback: wallet + ledger mutation, curse voiding, worker-facing line ---

        [Fact]
        public void ApplyEmployerKickback_UncursedEmployer_CreditsWalletAndLedger_AndReturnsLine()
        {
            Profile employer = new ProfileBuilder().WithDisplayName("Boss").Build();

            string line = ChateauWork.ApplyEmployerKickback("worker", employer, "Boss", 100, "copper");

            Assert.Equal(25, employer.currencies["copper"]);
            Assert.Equal(25, employer.employeeEarnings["worker"]["copper"]);
            Assert.Contains("Boss", line);
            Assert.Contains("[b]25 copper[/b]", line);
            Assert.Contains("MANOR", line);
        }

        [Fact]
        public void ApplyEmployerKickback_AddsToExistingBalances()
        {
            Profile employer = new ProfileBuilder()
                .WithCurrency("copper", 10)
                .WithEmployeeEarnings("worker", "copper", 5)
                .Build();

            ChateauWork.ApplyEmployerKickback("worker", employer, "Boss", 100, "copper");

            Assert.Equal(35, employer.currencies["copper"]);          // 10 + 25
            Assert.Equal(30, employer.employeeEarnings["worker"]["copper"]); // 5 + 25
        }

        [Fact]
        public void ApplyEmployerKickback_EmployerPovertyCursed_VoidsEverything()
        {
            Profile employer = new ProfileBuilder().WithDisplayName("Boss").Build();
            CurseInstance.SaveAll(employer, new List<CurseInstance>
            {
                new CurseInstance { Curse = "poverty", AppliedBy = "Someone", AppliedAt = DateTime.UtcNow }
            });

            string line = ChateauWork.ApplyEmployerKickback("worker", employer, "Boss", 100, "copper");

            Assert.Equal(string.Empty, line);
            Assert.False(employer.currencies.ContainsKey("copper"));
            Assert.False(employer.employeeEarnings.ContainsKey("worker"));
        }

        [Fact]
        public void ApplyEmployerKickback_ZeroRolledReward_PaysNothing()
        {
            Profile employer = new ProfileBuilder().WithDisplayName("Boss").Build();

            string line = ChateauWork.ApplyEmployerKickback("worker", employer, "Boss", 0, "copper");

            Assert.Equal(string.Empty, line);
            Assert.False(employer.currencies.ContainsKey("copper"));
            Assert.False(employer.employeeEarnings.ContainsKey("worker"));
        }

        // --- BuildBusiness: rendering the employeeEarnings ledger ---

        private static string IdentityResolver(string userName) => userName;

        [Fact]
        public void BuildBusiness_NullProfile_ReturnsEmptyState()
        {
            Assert.Equal(ChateauBusiness.EmptyStateMessage, ChateauBusiness.BuildBusiness(null, IdentityResolver));
        }

        [Fact]
        public void BuildBusiness_NoEarnings_ReturnsEmptyState()
        {
            Profile profile = new ProfileBuilder().Build();
            Assert.Equal(ChateauBusiness.EmptyStateMessage, ChateauBusiness.BuildBusiness(profile, IdentityResolver));
        }

        [Fact]
        public void BuildBusiness_SingleEmployeeSingleCurrency_RendersRowAndTotal()
        {
            Profile profile = new ProfileBuilder()
                .WithEmployeeEarnings("alice", "copper", 42)
                .Build();

            string output = ChateauBusiness.BuildBusiness(profile, un => "Alice");

            Assert.Contains("Alice: [b]42 copper[/b]", output);
            Assert.Contains("Total earned from all employees: [b]42 copper[/b]", output);
        }

        [Fact]
        public void BuildBusiness_MultipleEmployees_SortsByTotalDescAndSumsGrandTotals()
        {
            // Alice: 42 copper + 3 silver (total 45); Bob: 18 copper (total 18).
            Profile profile = new ProfileBuilder()
                .WithEmployeeEarnings("alice", "copper", 42)
                .WithEmployeeEarnings("alice", "silver", 3)
                .WithEmployeeEarnings("bob", "copper", 18)
                .Build();

            var names = new Dictionary<string, string> { { "alice", "Alice" }, { "bob", "Bob" } };
            string output = ChateauBusiness.BuildBusiness(profile, un => names[un]);

            int iAlice = output.IndexOf("Alice:", StringComparison.Ordinal);
            int iBob = output.IndexOf("Bob:", StringComparison.Ordinal);
            Assert.True(iAlice >= 0 && iBob >= 0);
            Assert.True(iAlice < iBob, "Higher earner Alice should render before Bob");

            // Currencies within a row are alphabetical.
            Assert.Contains("Alice: [b]42 copper[/b] | [b]3 silver[/b]", output);
            Assert.Contains("Bob: [b]18 copper[/b]", output);

            // Grand totals sum across employees, per currency.
            Assert.Contains("Total earned from all employees: [b]60 copper[/b] | [b]3 silver[/b]", output);
        }

        [Fact]
        public void BuildBusiness_PrunesNonPositiveEntries()
        {
            // Bob's only entry is zero -> Bob is dropped entirely; a zero currency on Alice is pruned.
            Profile profile = new ProfileBuilder()
                .WithEmployeeEarnings("alice", "copper", 10)
                .WithEmployeeEarnings("alice", "silver", 0)
                .WithEmployeeEarnings("bob", "copper", 0)
                .Build();

            var names = new Dictionary<string, string> { { "alice", "Alice" }, { "bob", "Bob" } };
            string output = ChateauBusiness.BuildBusiness(profile, un => names[un]);

            Assert.Contains("Alice: [b]10 copper[/b]", output);
            Assert.DoesNotContain("silver", output);
            Assert.DoesNotContain("Bob", output);
            Assert.Contains("Total earned from all employees: [b]10 copper[/b]", output);
        }

        [Fact]
        public void BuildBusiness_MissingDisplayName_FallsBackToUserNameKey()
        {
            Profile profile = new ProfileBuilder()
                .WithEmployeeEarnings("ghost_user", "copper", 5)
                .Build();

            // Resolver returns null (profile gone) -> the stored userName key is shown.
            string output = ChateauBusiness.BuildBusiness(profile, un => null);

            Assert.Contains("ghost_user: [b]5 copper[/b]", output);
        }
    }
}
