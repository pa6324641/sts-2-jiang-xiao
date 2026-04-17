using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps; // 必須引用 ValueProp
using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;

namespace JiangXiaoMod.Code.Cards.CardModels;

public abstract class JiangXiaoCardModel : CustomCardModel
{
    private readonly List<DynamicVar> _customVars = new();
    private readonly List<IHoverTip> _customTips = new();
    private readonly HashSet<CardKeyword> _customKeywords = new();
    private readonly HashSet<CardTag> _customTags = new();

    protected override IEnumerable<DynamicVar> CanonicalVars => _customVars;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => _customTips;
    public override HashSet<CardKeyword> CanonicalKeywords => _customKeywords;
    protected override HashSet<CardTag> CanonicalTags => _customTags;

    protected JiangXiaoCardModel(int cost, CardType type, CardRarity rarity, TargetType target, bool show = true)
        : base(cost, type, rarity, target, show) { }

    // --- 強化版變量添加方法 ---

    /// <summary>
    /// 最通用方法：直接添加任何 DynamicVar 實例 (包含 DamageVar, BlockVar)
    /// </summary>
    protected void JJVar(DynamicVar variable)
    {
        _customVars.Add(variable);
    }

    /// <summary>
    /// 專門添加傷害：自動封裝 DamageVar
    /// </summary>
    protected void JJDamage(decimal value, ValueProp prop = ValueProp.Move)
    {
        _customVars.Add(new DamageVar(value, prop));
    }

    /// <summary>
    /// 專門添加格擋：自動封裝 BlockVar
    /// </summary>
    protected void JJBlock(decimal value, ValueProp prop = ValueProp.Move)
    {
        _customVars.Add(new BlockVar(value, prop));
    }

    /// <summary>
    /// 添加自定義數值 (如 "M", "N")
    /// </summary>
    protected void JJCustomVar(string key, decimal value)
    {
        _customVars.Add(new DynamicVar(key, value));
    }

    // --- 其他工具方法 ---
    protected void JJTag(CardTag tag) => _customTags.Add(tag);
    protected void JJKeywordAndTip(CardKeyword kw)
    {
        _customKeywords.Add(kw);
        _customTips.Add(HoverTipFactory.FromKeyword(kw));
    }
    protected void JJStaticTip(StaticHoverTip tip) => _customTips.Add(HoverTipFactory.Static(tip));

}