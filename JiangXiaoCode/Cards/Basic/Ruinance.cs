using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class Ruinance : CustomCardModel
{
    private const decimal BaseBlock = 3m;
    private const decimal UpgradeBlock = 3m;
    private const decimal BaseResist = 3m;
    private const decimal RankBonus = 3m;

    public Ruinance() : base(1, CardType.Power, CardRarity.Basic, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    // [修正] DynamicVar 只接受 2 個參數：Key 和 數值
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(BaseBlock, ValueProp.Move),
        new DynamicVar("M", BaseResist) 
    ];

    public override HashSet<CardKeyword> CanonicalKeywords => [
        JiangXiaoModKeywords.Star, 
        JiangXiaoModKeywords.Passive
    ];

    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    /// 更新格擋與忍耐數值，確保 UI 顯示正確
    /// </summary>
    public void UpdateStatsBasedOnRank()
    {
        if (DynamicVars == null) return;

        int rank = JiangXiaoUtils.GetSkillRank(Owner);
        
        // 更新格擋
        decimal currentBlockBase = IsUpgraded ? (BaseBlock + UpgradeBlock) : BaseBlock;
        if (DynamicVars.Block != null)
        {
            DynamicVars.Block.BaseValue = currentBlockBase;
        }

        // [修正] 使用索引器獲取自定義變量 "M"，並進行空值檢查
        var resistVar = DynamicVars["M"];
        if (resistVar != null)
        {
            resistVar.BaseValue = BaseResist + (rank - 1) * RankBonus;
        }
    }

    // --- 生命週期掛鉤 ---

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

    public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        await base.BeforeHandDrawLate(player, choiceContext, combatState);

        if (player?.PlayerCombatState != null && player.PlayerCombatState.DrawPile.Cards.Contains(this))
        {
            await CardCmd.AutoPlay(choiceContext, this, player.Creature);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        UpdateStatsBasedOnRank();

        // 1. 獲得格擋
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 施加忍耐能力
        // [修正] 安全地讀取 DynamicVar "M"
        var resistVar = DynamicVars["M"];
        int finalResist = resistVar != null ? (int)resistVar.BaseValue : (int)BaseResist;

        await PowerCmd.Apply<RuinancePower>(Owner.Creature, finalResist, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        if (DynamicVars?.Block != null)
        {
            DynamicVars.Block.UpgradeValueBy(UpgradeBlock);
        }
        UpdateStatsBasedOnRank();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Passive),
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<RuinancePower>()
    ];
}