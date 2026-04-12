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

namespace JiangXiaoMod.Code.Cards.Uncommon;


[Pool(typeof(JiangXiaoCardPool))]
public class StarPowerArrow : CustomCardModel
{
    // [設定] 4費，攻擊牌，罕見，指向敵方
    public StarPowerArrow() : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {        
    }

    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModBOW];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move) // 初始基礎傷害設為 6
    ];

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 核心邏輯：計算 (3 + 3 * 弓Rank) * 星力等級
    /// </summary>
    private void UpdateStatsBasedOnRank()
    {
        // 獲取兩種等級
        int bowRank = JiangXiaoUtils.GetBowRank(Owner);
        int skillRank = JiangXiaoUtils.GetSkillRank(Owner);
        
        // [邏輯解釋] 
        // 基礎傷害 = 3
        // 每弓箭等級 +3 基礎傷害 -> (3 + 3 * bowRank)
        // 最後乘以星力等級 -> * skillRank
        decimal baseCalc = 3m + (bowRank * 3m);
        DynamicVars.Damage.BaseValue = baseCalc * skillRank;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 確保打出時數值是最新的
        UpdateStatsBasedOnRank();
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 執行攻擊
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") // 建議根據星力主題更換特效
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升級建議：減少費用的同時，提升基礎成長數值
        // 原本 (3 + 3 * BowRank)，升級後變為 (6 + 4 * BowRank)?
        // 這裡暫定基礎值提升
        DynamicVars.Damage.UpgradeValueBy(3m); 
        EnergyCost.UpgradeBy(-1); // 4費 -> 3費
    }
}