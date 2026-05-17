using FChatDicebot.Model;
using MongoDB.Bson;
using System;

namespace FChatDicebot.Tests.Builders
{
    /// <summary>
    /// Builder for creating Interaction test data with a fluent API.
    /// </summary>
    public class InteractionBuilder
    {
        private Interaction _interaction;

        public InteractionBuilder()
        {
            _interaction = new Interaction
            {
                Id = ObjectId.GenerateNewId(),
                initiator = TestConfiguration.GenerateTestUsername("Initiator"),
                recipient = TestConfiguration.GenerateTestUsername("Recipient"),
                type = "test",
                identifier = "default",
                investmentLevel = "standard",
                extraParameters = new BsonArray(),
                interactionTime = DateTime.UtcNow
            };
        }

        public InteractionBuilder WithInitiator(string initiator)
        {
            _interaction.initiator = initiator;
            return this;
        }

        public InteractionBuilder WithRecipient(string recipient)
        {
            _interaction.recipient = recipient;
            return this;
        }

        public InteractionBuilder WithType(string type)
        {
            _interaction.type = type;
            return this;
        }

        public InteractionBuilder WithIdentifier(string identifier)
        {
            _interaction.identifier = identifier;
            return this;
        }

        public InteractionBuilder WithInvestmentLevel(string investmentLevel)
        {
            _interaction.investmentLevel = investmentLevel;
            return this;
        }

        public InteractionBuilder WithExtraParameter(BsonValue parameter)
        {
            _interaction.extraParameters.Add(parameter);
            return this;
        }

        public InteractionBuilder WithInteractionTime(DateTime time)
        {
            _interaction.interactionTime = time;
            return this;
        }

        public Interaction Build()
        {
            return _interaction;
        }

        public Interaction BuildAndSave(Database.IChateauDatabase database)
        {
            database.AddInteraction(_interaction);
            return _interaction;
        }
    }
}