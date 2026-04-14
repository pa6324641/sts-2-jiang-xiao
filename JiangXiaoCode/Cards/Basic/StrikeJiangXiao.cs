using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class StrikeJiangXiao : CustomCardModel
{
    private const decimal BaseDmg = 6m;
    private const decimal UpgradeDmg = 3m;
    private const decimal RankBonus = 3m;

    // 構造函數保持純淨，只定義基礎屬性
    public StrikeJiangXiao() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.Star];
    
    // 這裡定義的 6m 是最基礎的顯示數值
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BaseDmg, ValueProp.Move)];
    
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    /// 核心邏輯：計算當前品質等級並修改基礎值
    /// </summary>
    public void UpdateStatsBasedOnRank()
    {
        // 安全檢查：確保 DynamicVars 已經被系統初始化
        if (DynamicVars?.Damage == null) return;

        int rank = JiangXiaoUtils.GetSkillRank(Owner);
        decimal currentBase = IsUpgraded ? (BaseDmg + UpgradeDmg) : BaseDmg;
        
        DynamicVars.Damage.BaseValue = currentBase + (rank - 1) * RankBonus;
    }

    // --- 在安全的生命週期勾子中執行更新 ---

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card == this) UpdateStatsBasedOnRank();
        return base.AfterCardChangedPiles(card, oldPileType, source);
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card == this) UpdateStatsBasedOnRank();
        return base.AfterCardDrawn(choiceContext, card, fromHandDraw);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        UpdateStatsBasedOnRank();

        // 這裡確保抓取的是更新後的 BaseValue
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升級時只修改能量，數值更新交給 UpdateStatsBasedOnRank
        EnergyCost.UpgradeBy(-1);
        UpdateStatsBasedOnRank(); 
    }
}