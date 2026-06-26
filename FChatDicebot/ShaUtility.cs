using Discord;
using Discord.WebSocket;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FChatDicebot
{
    public sealed class ShaUtility
    {
        public const string MG_PSK = "b4FVEURVQGIMTy70thdiq93Uii9tlQ";

        public static string ScramblePassword(string passwordInput, string secret)
        {
            return GetSha256SecurityHash(passwordInput, secret + "7qp5");
        }

        public static string GetHmac(double time)
        {
            return UrlSafe(GetSha256SecurityHash(time + "_MG", MG_PSK));
        }

        public static string UrlSafe(string input)
        {
            return input.Replace("+", "1").Replace("\\", "1").Replace("/", "1").Replace("&", "1").Replace(".", "1").Replace("?", "1");
        }

        public static string GetSha256SecurityHash(string message, string secret)
        {
            secret = secret ?? "";
            var encoding = new ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            string securityKey;

            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                securityKey = Convert.ToBase64String(hashmessage);
            }

            return securityKey;
        }
    }
}
