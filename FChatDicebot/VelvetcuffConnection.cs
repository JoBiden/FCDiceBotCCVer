using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using WebSocketSharp;
using FChatDicebot.Model;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;
using System.IO;

namespace FChatDicebot
{
    public class VelvetcuffConnection
    {
        private BotWebRequests WebRequests;
        private AccountSettings AccountSettings;

        private DateTime OriginDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public const double MaximumSecondsForVcToken = 60 * 60 * 8; //8 hour maximum seconds
        public const double MaximumSecondsForVcTransaction = 60 * 60 * 8; //8 hour, was 24 hour maximum seconds in 1.32c
        public double LastTokenRetrieved = 0;

        public const int TimeoutVcRequests = 60000; //1 minute max for all retries
        public const int TimeIntervalCheckOrdersSeconds = 30;

        public string CurrentOAuthVcKey = "";
        public string CurrentPaymentId = "";

        List<CheckAttempt> CheckThreads;

        public VelvetcuffConnection(BotWebRequests botWebRequests, AccountSettings accountSettings)
        {
            WebRequests = botWebRequests;
            AccountSettings = accountSettings;
            CheckThreads = new List<CheckAttempt>();
        }

        public void CheckVelvetcuffOrders(BotMain bot)
        {
        }

        public bool NeedNewVelvetucuffOauthToken()
        {
            return false;
        }

        public bool GetNewVelvetcuffOauthToken()
        {
            return true;
        }

        public bool CreateNewVcTransaction(int transactionAmount, string targetChar, string sourceChar, bool sendingMoney, string customMessage)
        {
            return true;
        }

        public bool CheckVcTransaction(string paymentId, out int currentPaymentStatus)
        {
            currentPaymentStatus = 0;
            return true;
        }

        public void CheckVcTransactionAndAct(VcChipOrder order, BotMain bot, bool delete)
        {
        }

        public bool DeleteVcTransaction(string transactionId)
        {
            return true;
        }
    }

    public class CheckAttempt
    {
        public string OrderId;
        public double StartTime;
        public Thread CheckThread;
    }
}
