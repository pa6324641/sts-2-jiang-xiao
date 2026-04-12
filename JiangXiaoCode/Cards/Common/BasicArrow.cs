using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Cards;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JiangXiaoMod.Code.Cards.Common;


[Pool(typeof(JiangXiaoCardPool))]
public class BasicArrow : CustomCardModel
{
    public BasicArrow() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {        
    }

    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModBOW];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(2m, ValueProp.Move), // 基礎值 2，Rank 1 時為 2 + (1*2) = 4
        new DynamicVar("M", 1m)           // 搜尋並抽取 1 張牌
    ];

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 根據弓箭等級更新傷害數值
    /// </summary>
    private void UpdateStatsBasedOnRank()
    {
        // [STS2_Optimization] 呼叫工具類獲取當前弓箭等級 (JiangXiaoUtils 需包含 GetBowRank 方法)
        int rank = JiangXiaoUtils.GetBowRank(Owner);
        
        // 邏輯：4點傷害 + 每Rank增加2點 (起始 Rank 1 則為 4)
        // 此處設定為 2 + (rank * 2)
        DynamicVars.Damage.BaseValue = 2m + (rank * 2m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 確保數值最新
        UpdateStatsBasedOnRank();
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 2. 執行攻擊動作
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") 
            .Execute(choiceContext);

        // 3. 定向抽牌邏輯
        var player = this.Owner;
        // 使用 player.PlayerCombatState 獲取戰鬥狀態是正確的
        var combatState = player?.PlayerCombatState;

        if (combatState != null)
        {
            var drawPile = combatState.DrawPile;

            // [邏輯修正] 直接尋找抽牌堆中「第一張」具有 JiangXiaoModBOW 關鍵字的牌
            // 我們不需要判斷「這張卡是否在抽牌堆」，因為我們要抽的是「其他」箭矢或弓類牌
            var targetCard = drawPile.Cards.FirstOrDefault(c => 
                c.CanonicalKeywords.Contains(JiangXiaoModKeywords.JiangXiaoModBOW));

            if (targetCard != null)
            {
                // [STS2_API] 使用 CardPileCmd.Add 移至手牌
                // 根據手冊，這會自動從原牌堆（抽牌堆）移除並播放移動動畫
                await CardPileCmd.Add(targetCard, PileType.Hand, CardPilePosition.Top, this);
            }
        }
    }
    protected override void OnUpgrade()
    {
        // 升級建議：基礎值提升
        DynamicVars.Damage.UpgradeValueBy(2m);
        EnergyCost.UpgradeBy(-1);
    }
}