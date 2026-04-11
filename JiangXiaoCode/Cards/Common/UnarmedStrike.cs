using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions; // 引用工具類以獲取等級
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;

namespace JiangXiaoMod.Code.Cards.Common;


[Pool(typeof(JiangXiaoCardPool))]
public class UnarmedStrike : CustomCardModel
{
    public UnarmedStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {        
    }
    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModUNARMED];

    /// <summary>
    /// 定義卡片變量：Damage (傷害) 與 M (抽牌數)
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3m, ValueProp.Move),       // 基礎傷害 3
        new DynamicVar("M", 1m)  // 基礎抽牌 1
    ];

    /// <summary>
    /// 每當狀態更新時調用，用於根據「徒手等級」動態調整數值
    /// </summary>
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    private void UpdateStatsBasedOnRank()
    {
        // 從 Owner (Player) 獲取徒手等級
        int rank = JiangXiaoUtils.GetUnarmedRank(Owner);
        
        // [邏輯] 傷害：3 + (Rank * 3)
        // 注意：若 Rank 為 1，則傷害為 6
        DynamicVars.Damage.BaseValue = 3m + (rank * 3m);
        
        // [邏輯] 抽牌：1 + (Rank / 2)
        // C# 整數除法會自動捨去小數 (例如 Rank 1 除 2 = 0)
        DynamicVars["M"].BaseValue = 1m + (rank / 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 確保打出時數值是最新的
        UpdateStatsBasedOnRank();
        ArgumentNullException.ThrowIfNull(cardPlay.Target);


        // 1. 執行攻擊動作
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 執行抽牌動作
        int drawAmount = (int)DynamicVars["M"].BaseValue;
        if (drawAmount > 0)
        {
            await CardPileCmd.Draw(choiceContext, drawAmount, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 升級效果：基礎傷害再 +3 (可根據需求調整)
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}