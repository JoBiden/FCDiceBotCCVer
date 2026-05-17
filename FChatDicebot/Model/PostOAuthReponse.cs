using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    public class PostOAuthResponse
    {
        public string access_token;
        public long expires_in;
        public string token_type;
        public string refresh_token;
        public string scope;
    }

    public class PostOAuthRequest
    {
        public string grant_type;
        public string client_id;
        public string username;
        public string password;
    }
}
