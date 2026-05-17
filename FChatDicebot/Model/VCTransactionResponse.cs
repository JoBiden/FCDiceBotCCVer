using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    public class Payer
    {
        public string referringCharacterId { get; set; }
        public double upperWalletLimit { get; set; }
        public double lowerWalletLimit { get; set; }
        public double maxTransactionAmount { get; set; }
        public DateTime creationTime { get; set; }
        public double money { get; set; }
        public int characterClass { get; set; }
        public string title { get; set; }
        public bool isBookmarkedBy { get; set; }
        public string avatarOutline { get; set; }
        public object characterNameColor { get; set; }
        public object characterNameAnimation { get; set; }
        public double taxRate { get; set; }
        public bool hasBreakerJob { get; set; }
        public bool hasFighterJob { get; set; }
        public bool hasFindomJob { get; set; }
        public bool hasMakerJob { get; set; }
        public bool isActive { get; set; }
        public int level { get; set; }
        public int paragonLevel { get; set; }
        public bool isOnline { get; set; }
        public bool isAvailable { get; set; }
        public object chatStatusText { get; set; }
        public bool isMine { get; set; }
        public string id { get; set; }
    }

    public class Payee
    {
        public string referringCharacterId { get; set; }
        public double upperWalletLimit { get; set; }
        public double lowerWalletLimit { get; set; }
        public double maxTransactionAmount { get; set; }
        public DateTime creationTime { get; set; }
        public double money { get; set; }
        public int characterClass { get; set; }
        public string title { get; set; }
        public bool isBookmarkedBy { get; set; }
        public object avatarOutline { get; set; }
        public object characterNameColor { get; set; }
        public object characterNameAnimation { get; set; }
        public double taxRate { get; set; }
        public bool hasBreakerJob { get; set; }
        public bool hasFighterJob { get; set; }
        public bool hasFindomJob { get; set; }
        public bool hasMakerJob { get; set; }
        public bool isActive { get; set; }
        public int level { get; set; }
        public int paragonLevel { get; set; }
        public bool isOnline { get; set; }
        public bool isAvailable { get; set; }
        public object chatStatusText { get; set; }
        public bool isMine { get; set; }
        public string id { get; set; }
    }

    public class ChatLog
    {
        public string chatLogs { get; set; }
        public bool isMine { get; set; }
        public int id { get; set; }
    }

    public class VCTransactionResponse
    {
        public double amount { get; set; }
        public double taxesAmount { get; set; }
        public double savingsAmount { get; set; }
        public double houseRewardForPayer { get; set; }
        public double houseRewardForPayee { get; set; }
        public Payer payer { get; set; }
        public Payee payee { get; set; }
        public string customMessage { get; set; }
        public int transactionTypeRaw { get; set; }
        public List<object> lineItems { get; set; }
        public bool isLoserAnonymous { get; set; }
        public bool isWinnerAnonymous { get; set; }
        public bool isImmediatePayment { get; set; }
        public DateTime acceptedDate { get; set; }
        public int status { get; set; }
        public int transactionType { get; set; }
        public object quickActionType { get; set; }
        public int likes { get; set; }
        public int dislikes { get; set; }
        public int totalLikes { get; set; }
        public bool hasChatLogs { get; set; }
        public int chatLogLength { get; set; }
        public List<object> whoLikedThis { get; set; }
        public ChatLog chatLog { get; set; }
        public object fightVictoryType { get; set; }
        public bool isPayer { get; set; }
        public bool isPayee { get; set; }
        public bool areLogsAnonymous { get; set; }
        public object submittedForReview { get; set; }
        public bool isShowcasedByPayer { get; set; }
        public bool isShowcasedByPayee { get; set; }
        public bool containsExtremeContent { get; set; }
        public List<object> tags { get; set; }
        public DateTime creationTime { get; set; }
        public object linkedEvent { get; set; }
        public object specialEffectForPayer { get; set; }
        public object specialEffectForPayee { get; set; }
        public int season { get; set; }
        public bool isMine { get; set; }
        public string id { get; set; }
    }
    //    RequestStatus
    //{
    //sent = 0
    //accepted = 1
    //denide = 2
    //cancelled = 3
    //}

}
