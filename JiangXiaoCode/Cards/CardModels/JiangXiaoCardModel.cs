using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps; // 必須引用 ValueProp
using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JiangXiaoMod.Code.Cards.CardModels;

public abstract class JiangXiaoCardModel : CustomCardModel
{
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
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
    protected void JJPowerTip<T>() where T : PowerModel
    {
        _customTips.Add(HoverTipFactory.FromPower<T>());
    }

    //複製自動刷新
    public void UpdateStatsBasedOnRank() 
    {
        // 獲取當前持有者
        var player = Owner;

        // 1. 抓取通用的「星技品質」等級 (預設為 1) [cite: 6, 7]
        int skillRank = JiangXiaoUtils.GetSkillRank(player);

        // 2. 執行應用邏輯，將 player 傳入以便子類抓取特定技藝等級
        ApplyRankLogic(player, skillRank);
    }
    // 定義一個抽象或虛擬函數，強迫或允許子類實作自己的計算公式
    protected abstract void ApplyRankLogic(Player? player, int skillRank);

    // 生成時自動觸發，動態文本
    public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
    {
        UpdateStatsBasedOnRank();
        return base.AfterCardGeneratedForCombat(card, addedByPlayer);
    }
    public override void AfterCreated()
    {
        UpdateStatsBasedOnRank();
        base.AfterCreated();
    }
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

}