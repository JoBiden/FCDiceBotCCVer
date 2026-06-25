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

        public bool AbandonWebRequestFList = false;

        //private bool awaitingReturnMonsterGenerator = false;

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
                no_bookmarks = bookmarksList ? "false" : "true",
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
            catch (System.Net.ProtocolViolationException exc)
            {
                Console.WriteLine("ProtocolViolationException on LoginToServer Call: " + exc.Message);
                if (!AbandonWebRequestFList)
                {
                    Thread.Sleep(10000);
                    Console.WriteLine("Retrying...");

                    Utils.AddToLog("WebException on LoginToServer Call: " + exc.Message, accountSettings);
                    LoginToServer(accountSettings, friendsList, bookmarksList);
                }
            }
        }

        #region Monster Generator
        public string MGRetrieveMonster(int monsterId, string monsterName, string psk, bool shortStats, int tryNumber = 0)
        {
            string response = null;
            if (BotMain._debug)
                Console.WriteLine("Monster retrieve MG start");

            double timestamp = DoubleTime.GetCurrentTimestampSeconds();
            string endpointUsed = "RetrieveMonster";
            string monsterNameSent = string.IsNullOrEmpty(monsterName) ? "_" : monsterName;

            string hmac = ShaUtility.GetHmac(timestamp);// ShaUtility.GetSha256SecurityHash(timestamp + "_MG", MG_PSK);

            string allParameters = string.Format("?id={0}&name={1}&hmac={2}&time={3}&shortblock={4}", monsterId, monsterNameSent, hmac, timestamp, shortStats);

            if (monsterId <= 0 && string.IsNullOrEmpty(monsterName))
            {
                string errorString = "Error: no monsterId or monsterName found";// on MgretrieveMonsterid";
                Console.WriteLine(errorString);
                response = errorString;
                return response;
            }

            // Create a new 'HttpWebRequest' Object to the mentioned URL.
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("https://monstergenerator.javissoft.com/api/" + endpointUsed + "/" + allParameters);

            myHttpWebRequest.Method = "GET";

            try
            {
                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

                if (myHttpWebResponse != null)
                {
                    System.IO.StreamReader sr = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream());
                    string rtnValue = sr.ReadToEnd();

                    //clean up return
                    rtnValue = rtnValue.Replace("\"", "").Replace("\\n", "\n").Replace("\\r", "\r");

                    response = "some output\n\nsomemoreoutput";

                    response = rtnValue;

                    sr.Close();
                }
                myHttpWebResponse.Close();
            }
            catch (WebException ex)
            {
                Console.WriteLine("WebException on MGRetrieveMonsterId Call: " + ex.Message);
                if (tryNumber == 0)
                {
                    Thread.Sleep(2000);
                    Console.WriteLine("Retrying MGRetrieveMonsterId Call...");
                    return MGRetrieveMonster(monsterId, monsterName, psk, shortStats, tryNumber + 1);
                }
                response = "webexception on retrieve monster";
            }
            return response;
        }

        public string MGGenerateMonster(bool shortStats, double targetCr, string psk, int tryNumber = 0)
        {
            string response = null;
            if (BotMain._debug)
                Console.WriteLine("Monster retrieve MG start");

            double timestamp = DoubleTime.GetCurrentTimestampSeconds();
            string endpointUsed = "GenerateMonster";

            string hmac = ShaUtility.GetHmac(timestamp);// ShaUtility.GetSha256SecurityHash(timestamp + "_MG", MG_PSK);

            GenerateMonsterRequest generateRequest = new GenerateMonsterRequest()
            {
                confirmHmac = hmac,
                timestamp = timestamp,
                shortBlock = shortStats,
                targetChallengeRating = targetCr
            };


            // Create a new 'HttpWebRequest' Object to the mentioned URL.
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("https://monstergenerator.javissoft.com/api/" + endpointUsed + "/"); //PostGenerateMonster

            myHttpWebRequest.Method = "POST";

            myHttpWebRequest.ContentType = "application/json";
            string jsonString = "";
            try
            {
                jsonString = JsonConvert.SerializeObject(generateRequest);
            }
            catch (Exception exc)
            {
                Console.WriteLine("error occured on jsonconvert: " + exc.Message);
                return "error occured on content jsonconvert";
            }

            byte[] bytes = Encoding.ASCII.GetBytes(jsonString);

            Stream os = null;
            try
            {
                myHttpWebRequest.ContentLength = bytes.Length;
                os = myHttpWebRequest.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);
                os.Close();
                Console.WriteLine("wrote stream " + jsonString);
            }
            catch (WebException ex)
            {
                Console.WriteLine("error occred on content write " + ex.Message);
                return "error occured on content request stream write";
            }

            try
            {
                // Assign the response object of 'HttpWebRequest' to a 'HttpWebResponse' variable.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

                if (myHttpWebResponse != null)
                {
                    System.IO.StreamReader sr = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream());
                    string rtnValue = sr.ReadToEnd();

                    GenerateMonsterReply reply = JsonConvert.DeserializeObject<GenerateMonsterReply>(rtnValue);

                    //clean up return
                    rtnValue = reply.monsterPrintout.Replace("\"", "").Replace("\\n", "\n").Replace("\\r", "\r");

                    response = rtnValue;

                    sr.Close();
                }
                myHttpWebResponse.Close();
            }
            catch (WebException ex)
            {
                Console.WriteLine("WebException on MGRetrieveMonsterId Call: " + ex.Message);
                if (tryNumber == 0)
                {
                    Thread.Sleep(2000);
                    Console.WriteLine("Retrying MGRetrieveMonsterId Call...");
                    return MGGenerateMonster( shortStats, targetCr, psk, tryNumber + 1);
                }
                response = "webexception on retrieve monster";
            }
            return response;
        }
        #endregion

    }
}
