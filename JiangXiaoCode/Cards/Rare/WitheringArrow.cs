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
using MegaCrit.Sts2.Core.Models.Powers; // 確保引用了 Power 命名空間

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class WitheringArrow : CustomCardModel
{
    // [設定] 3費，攻擊牌，稀有，指向敵方
    public WitheringArrow() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {        
    }

    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModBOW];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3m, ValueProp.Move), // 基礎傷害 3
        new DynamicVar("M", 3m),          // 中毒 (M) 基礎 3
        new DynamicVar("D", 3m)           // 災厄 (D) 基礎 3
    ];

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 根據弓箭等級縮放：傷害 +3/Rank, 中毒 +5/Rank, 災厄 +5/Rank
    /// </summary>
    private void UpdateStatsBasedOnRank()
    {
        int rank = JiangXiaoUtils.GetBowRank(Owner);
        
        // [邏輯] 若 Rank 為 1：傷害=6, 中毒=8, 災厄=8
        DynamicVars.Damage.BaseValue = 3m + (rank * 3m);
        DynamicVars["M"].BaseValue = 3m + (rank * 5m);
        DynamicVars["D"].BaseValue = 3m + (rank * 5m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 更新最新數值
        UpdateStatsBasedOnRank();
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var player = Owner;
        if (player?.Creature == null) return;

        // 2. 執行攻擊
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") 
            .Execute(choiceContext);

        // 3. 施加中毒與災厄
        int poisonAmt = (int)DynamicVars["M"].BaseValue;
        int doomAmt = (int)DynamicVars["D"].BaseValue;

        if (poisonAmt > 0)
        {
            // [STS2_API] 使用 PowerCmd.Apply 施加狀態
            await PowerCmd.Apply<PoisonPower>(cardPlay.Target, poisonAmt, player.Creature, this);
        }

        if (doomAmt > 0)
        {
            // 這裡假設 DoomPower 是您 Mod 中定義的類別或 STS2 內建類別
            await PowerCmd.Apply<DoomPower>(cardPlay.Target, doomAmt, player.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升級建議：提升成長係數或降低費用
        // 此處設定為提升基礎中毒與災厄 (+2)
        DynamicVars["M"].UpgradeValueBy(2m);
        DynamicVars["D"].UpgradeValueBy(2m);
        // 或者：EnergyCost.UpgradeBy(-1); // 降為 2 費
    }
}