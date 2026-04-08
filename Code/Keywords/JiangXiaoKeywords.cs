using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Keywords;

public static class JiangXiaoModKeywords
{
    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Passive;

    public static bool IsPassive(this CardModel card)
    {
        return card.Keywords.Contains(Passive);
    }

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword RuinancePower;
        public static bool IsRunancepower(this CardModel card)
    {
        return card.Keywords.Contains(RuinancePower);
    }

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Star;
        public static bool IsStar(this CardModel card)
    {
        return card.Keywords.Contains(Star);
    }

    // [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    // public static CardKeyword ChengYinPower;
    //     public static bool IsChengYinPower(this CardModel card)
    // {
    //     return card.Keywords.Contains(ChengYinPower);
    // }

    // [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    // public static CardKeyword DawnPower;
    //     public static bool IsDawnPower(this CardModel card)
    // {
    //     return card.Keywords.Contains(DawnPower);
    // }

    // [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    // public static CardKeyword YearningHaloPower;
    //     public static bool IsYearningHaloPower(this CardModel card)
    // {
    //     return card.Keywords.Contains(YearningHaloPower);
    // }
}