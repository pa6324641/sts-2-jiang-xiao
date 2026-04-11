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

    //基礎技藝
    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModUNARMED;
        public static bool IsJiangXiaoModUNARMED(this CardModel card)
    {
        return card.Keywords.Contains(JiangXiaoModUNARMED);
    }
        [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModBLADE;
        public static bool IsJiangXiaoModBLADE(this CardModel card)
    {
        return card.Keywords.Contains(JiangXiaoModBLADE);
    }
        [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModBOW;
        public static bool IsJiangXiaoModBOW(this CardModel card)
    {
        return card.Keywords.Contains(JiangXiaoModBOW);
    }
        [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModDAGGER;
        public static bool IsJiangXiaoModDAGGER(this CardModel card)
    {
        return card.Keywords.Contains(JiangXiaoModDAGGER);
    }
        [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModHALBERD;
        public static bool IsJiangXiaoModHALBERD(this CardModel card)
    {
        return card.Keywords.Contains(JiangXiaoModHALBERD);
    }
        [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModCOMBATKNIFE;
        public static bool IsJiangXiaoModCOMBATKNIFE(this CardModel card)
    {
        return card.Keywords.Contains(JiangXiaoModCOMBATKNIFE);
    }


}