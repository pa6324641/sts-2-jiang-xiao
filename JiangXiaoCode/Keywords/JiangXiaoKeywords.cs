using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Linq; // 確保有引用 Linq

namespace JiangXiaoMod.Code.Keywords;

public static class JiangXiaoModKeywords
{
    // [STS2_BaseLib] 使用 CustomEnum 標記，BaseLib 會自動根據變量名註冊關鍵字
    // 例如：JiangXiaoModUNARMED 會對應到 localization 中的 JIANGXIAOMOD-JIANGXIAOMODUNARMED

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Passive;
    public static bool IsPassive(this CardModel card) => card.CanonicalKeywords.Contains(Passive);

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword RuinancePower;
    public static bool IsRuinancePower(this CardModel card) => card.CanonicalKeywords.Contains(RuinancePower);

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Star;
    public static bool IsStar(this CardModel card) => card.CanonicalKeywords.Contains(Star);

    // --- 基礎技藝類標籤 ---

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModUNARMED;
    public static bool IsJiangXiaoModUNARMED(this CardModel card) => card.CanonicalKeywords.Contains(JiangXiaoModUNARMED);

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModBLADE;
    public static bool IsJiangXiaoModBLADE(this CardModel card) => card.CanonicalKeywords.Contains(JiangXiaoModBLADE);

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModBOW;
    public static bool IsJiangXiaoModBOW(this CardModel card) => card.CanonicalKeywords.Contains(JiangXiaoModBOW);

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModDAGGER;
    public static bool IsJiangXiaoModDAGGER(this CardModel card) => card.CanonicalKeywords.Contains(JiangXiaoModDAGGER);

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModHALBERD;
    public static bool IsJiangXiaoModHALBERD(this CardModel card) => card.CanonicalKeywords.Contains(JiangXiaoModHALBERD);

    [CustomEnum] [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword JiangXiaoModCOMBATKNIFE;
    public static bool IsJiangXiaoModCOMBATKNIFE(this CardModel card) => card.CanonicalKeywords.Contains(JiangXiaoModCOMBATKNIFE);
}