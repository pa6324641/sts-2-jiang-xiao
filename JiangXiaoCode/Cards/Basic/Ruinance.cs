using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
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
public sealed class Ruinance : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-RUINANCE";
    private const string MVarKey = "M";

    private const decimal BaseBlockVal = 3m;
    private const decimal UpgradeBlockBonus = 3m;
    private const decimal BaseResistVal = 3m;
    private const decimal RankBonusVal = 3m;

    public Ruinance() : base(1, CardType.Power, CardRarity.Basic, TargetType.Self)
    {
        // 使用基類輔助方法統一添加星技與被動提示
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJKeywordAndTip(JiangXiaoModKeywords.Passive);
        JJBlock(BaseBlockVal, ValueProp.Move);
        JJCustomVar(MVarKey, BaseResistVal);
    }

    // public override bool GainsBlock => true;

    // protected override IEnumerable<DynamicVar> CanonicalVars => [
    //     new BlockVar(BaseBlockVal, ValueProp.Move),
    //     new DynamicVar(MVarKey, BaseResistVal) 
    // ];

    /// <summary>
    /// 合併後的數值邏輯：統一由 ApplyRankLogic 根據 IsUpgraded 與 rank 計算最終值
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 1. 計算格擋：基礎 3，強化後基礎變 6
        decimal currentBlockBase = BaseBlockVal + (IsUpgraded ? UpgradeBlockBonus : 0m);
        DynamicVars.Block.BaseValue = currentBlockBase;

        // 2. 計算忍耐(M)：基礎 3 + (等級-1) * 3
        DynamicVars[MVarKey].BaseValue = BaseResistVal + (skillRank - 1) * RankBonusVal;
    }

    /// <summary>
    /// 強化時僅觸發重算
    /// </summary>
    protected override void OnUpgrade() => UpdateStatsBasedOnRank();

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 被動星技邏輯：第一回合若在抽牌堆則自動打出
    /// </summary>
    // public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    // {
    //     if (player?.PlayerCombatState != null && 
    //         player.PlayerCombatState.DrawPile.Cards.Contains(this) && 
    //         combatState.RoundNumber == 1)
    //     {
    //         // 自動播放前確保數值已根據當前星技品質更新
    //         UpdateStatsBasedOnRank();
    //         await CardCmd.AutoPlay(choiceContext, this, player.Creature);
    //     }
    // }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 再次確保數值正確（預防動態等級變動）
        UpdateStatsBasedOnRank();

        // 1. 獲得格擋
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 施加忍耐能力 (RuinancePower)
        int finalResist = (int)DynamicVars[MVarKey].BaseValue;
        await PowerCmd.Apply<RuinancePower>(Owner.Creature, finalResist, Owner.Creature, this);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Passive),
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<RuinancePower>()
    ];
}