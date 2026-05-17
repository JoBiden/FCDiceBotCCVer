using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.Model;
using System.Net;
using Newtonsoft.Json;
using System.Threading;
using FChatDicebot.SavedData;
using System.IO;

namespace FChatDicebot
{
    public class BotWebRequests
    {
        private string GetApiTicketUrl = "https://f-list.net/json/getApiTicket.php";

        private bool awaitingReturn = false;

        public GetApiTicketResponse ApiTicketResult = null;
        public PostOAuthResponse VcOauthResult = null;
        public VCTransactionResponse VcCreateTransactionResponse = null;

        public bool CompletedDeletion = false;

        public bool AbandonWebRequestVc = false;
        public bool AbandonWebRequestFList = false;
        public bool AbandonVCCheckPayment = false;
        public bool AbandonVCCreatePayment = false;
        public bool AbandonDeleteVcTransaction = false;
        
        public void LoginToServerPublic(AccountSettings accountSettings)
        {
            Thread t = new Thread(() => LoginToServer(accountSettings, false, false));
            t.IsBackground = true;
            t.Start();
        }

        private void LoginToServer(AccountSettings accountSettings, bool friendsList, bool bookmarksList)
        {

            GetApiTicketRequest requestObject = new GetApiTicketRequest()
            {
                password = accountSettings.AccountPassword,
                account = accountSettings.AccountName,
                no_bookmarks = bookmarksList? "false" : "true",
                no_characters = "true",
                no_friends = friendsList ? "false" : "true"
            };

            string webserver = GetApiTicketUrl;
            //parameters are what is actually used to get the api ticket rather than the post body content
            string parameters = "?account=" + requestObject.account + "&password=" + requestObject.password + "&no_characters=" + 
                requestObject.no_characters + "&no_friends=" + requestObject.no_friends + "&no_bookmarks=" + 
                requestObject.no_bookmarks;

            WebRequest request = Utils.CreateWebRequest(webserver + parameters, requestObject);
            WebResponse webResponse = null;

            try
            {
                webResponse = (HttpWebResponse)request.GetResponse();

                if (webResponse != null)
                {
                    System.IO.StreamReader sr = new System.IO.StreamReader(webResponse.GetResponseStream());
                    string rtnValue = sr.ReadToEnd();

                    if (BotMain._debug)
                    {
                        Console.WriteLine("LoginToServer return message: " + rtnValue);
                        Utils.AddToLog("LoginToServer return message: " + rtnValue, null);
                    }

                    GetApiTicketResponse rpt = JsonConvert.DeserializeObject<GetApiTicketResponse>(rtnValue);
                    awaitingReturn = false;

                    if (rpt != null)
                    {
                        ApiTicketResult = rpt;
                    }
                    else
                    {
                        Console.WriteLine("Error: failed to parse result into GetApiTicketResponse");
                        Utils.AddToLog("Error: failed to parse result into GetApiTicketResponse", null);
                    }

                    string saaaaaa = rtnValue + " ";
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine("WebException on LoginToServer Call: " + ex.Message);
                if (!AbandonWebRequestFList)
                {
                    Thread.Sleep(2000);
                    Console.WriteLine("Retrying...");

                    Utils.AddToLog("WebException on LoginToServer Call: " + ex.Message, accountSettings);
                    LoginToServer(accountSettings, friendsList, bookmarksList);
                }
            }
        }


        public void LoginToVCPublic(AccountSettings accountSettings)
        {
            Thread t = new Thread(() => GetVCAuthTicket(accountSettings));
            t.IsBackground = true;
            t.Start();
        }

        private void GetVCAuthTicket(AccountSettings accountSettings)
        {
        }

        public void VCCreatePaymentHttp(string oAuthToken, int transactionAmount, string targetChar, string sourceChar, bool sendingMoney, string customMessage)
        {
        }

        public VCTransactionResponse VCCheckPaymentHttp(string oAuthToken, string transactionId)
        {
            return null;
        }

        public void VCDeletePaymentHttp(string oAuthToken, string transactionId)
        {
            
        }
    }
}
