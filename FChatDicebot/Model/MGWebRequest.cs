using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    public class MGWebRequest
    {
        public double timestamp { get; set; }
        public int errorCode { get; set; }
        public string confirmHmac { get; set; }
        public string message { get; set; }
        public string request { get; set; }
    }
    //public class VerificationResult
    //{
    //    public int responseCode { get; set; }
    //    public int errorCode { get; set; }
    //    public string responseMessage { get; set; }

    //    public string Print()
    //    {
    //        return errorCode + "_" + responseCode + ": " + responseMessage;
    //    }
    //}
}
