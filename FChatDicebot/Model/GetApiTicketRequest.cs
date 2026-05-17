using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    public class GetApiTicketRequest
    {
        public string account;
        public string password;
        public string no_characters;
        public string no_friends;
        public string no_bookmarks;
        public string new_character_list;
    }

    public class GetApiTicketResponse
    {
        public Bookmark[] bookmarks;
        public Friend[] friends;
        public string[] characters;
        public string ticket;
        public string default_character;
    }

    public class Bookmark
    {
        public string name;
    }

    public class Friend
    {
        public string source_name;
        public string dest_name;
    }
}
