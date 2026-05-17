using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    public class LineItem
    {
        public int id { get; set; }
        public bool isMine { get; set; }
        public DateTime creationTime { get; set; }
        public double amount { get; set; }
        public int action { get; set; }
    }

    public class VCTransactionRequest
    {
        public string initiatingCharacterId { get; set; }
        public string targetedCharacterId { get; set; }
        public bool isSending { get; set; }
        public int transactionType { get; set; }
        public string customMessage { get; set; }
        public double amount { get; set; }
        public string chatLogs { get; set; }
        public bool containsExtremeContent { get; set; }
        public bool isSimulation { get; set; }
        public bool areLogsAnonymous { get; set; }
        //public int linkedEventId { get; set; }
        public List<LineItem> lineItems { get; set; }
        public bool isLoserAnonymous { get; set; }
        public bool isWinnerAnonymous { get; set; }
        //public string externalAppId { get; set; }
        //public string externalAppActionId { get; set; }
        public bool canUseSavings { get; set; }
        public bool isTaxFree { get; set; }
        public bool sendFromSavings { get; set; }
        public bool sendToSavings { get; set; }
        public bool sendSavingsToWallet { get; set; }
        public bool sendNotification { get; set; }
    }
}
