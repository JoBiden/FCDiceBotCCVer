using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Shared transfer logic for <see cref="PaymentGiveProcessor"/> (paymentGive) and
    /// <see cref="PaymentReceiveProcessor"/> (paymentReceive) — the two are ~identical after
    /// the atomic-debit rewrite, differing only in which party pays and in their wording, so
    /// the money-moving code (the part Phase 1 carefully made mint-proof) lives here once.
    /// Each subclass stays a separately registered <see cref="IInteractionProcessor"/> under
    /// its own <see cref="IInteractionProcessor.InteractionType"/> key so existing pendings
    /// and the verb-specific wording (GetInteractionVerb/GetCompletionMessage/
    /// GetConsentWarning) are unaffected.
    /// </summary>
    public abstract class PaymentProcessorBase : InteractionProcessorBase
    {
        public override string InvestmentLevel => "involved";

        /// <summary>True for paymentGive (initiator pays recipient); false for
        /// paymentReceive (initiator bills recipient, i.e. recipient pays initiator).</summary>
        protected abstract bool IsGive { get; }

        /// <summary>
        /// The tail of the "insufficient funds" private message, appended after
        /// "{payer} no longer has enough {currency} " (e.g. "for that transfer." /
        /// "to cover that bill.").
        /// </summary>
        protected abstract string InsufficientFundsSuffix { get; }

        protected PaymentProcessorBase(IChateauDatabase database) : base(database)
        {
        }

        protected PaymentProcessorBase() : base()
        {
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("currency"));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string currency = command.pendingInteraction.identifier;
            int amount = command.pendingInteraction.extraParameters.FirstOrDefault().ToInt32();
            int magnitude = Math.Abs(amount);

            string payer = IsGive ? initiator : recipient;
            string payee = IsGive ? recipient : initiator;

            // Debit is atomic and guarded (>= magnitude unless the currency allows negative
            // balances), so this can never mint currency on self-pay (initiator == recipient
            // nets the same $inc twice back to zero) and never overdraws a currency that
            // isn't ious/nothing even if a second queued payment races this one to consent.
            bool debited = Database.TryDebitCurrency(payer, currency, magnitude, CurrencyRules.AllowsNegative(currency));
            if (!debited)
            {
                _lastInitiatorPrivateMessage = payer + " no longer has enough " + currency + " " + InsufficientFundsSuffix;
                Database.DeletePendingCommand(command.Id);
                return "NoInteraction";
            }

            Database.ChangeCurrency(payee, currency, magnitude);

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return InteractionType;
        }
    }
}
