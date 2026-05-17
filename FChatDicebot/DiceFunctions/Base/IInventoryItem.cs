using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//NOTE: currently not used for anything but potions
namespace FChatDicebot.DiceFunctions
{
    public interface IInventoryItem
    {
        double GetRarity();

        string GetRarityString();

        string GetName();

        string GetName(bool hideDetails);

        ItemCategory GetItemCategory();

        int GetGoldValue();

        bool IsHidden();

        string Activate();

        string ToString();
    }

    public enum ItemCategory
    {
        NONE,
        Asset,
        Outfit,
        Potion, //currently only one used
        Scroll,
        Food,
        Weapon,
        Armor,
        Ring,
        Amulet,
        Cloak,
        Accessory
    }
}
