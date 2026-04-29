using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions;
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
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public class DaggerBurst : JiangXiaoCardModel
{
    protected override bool HasEnergyCostX => true;
    private const decimal DamagePerRank = 1m;
    private const decimal UpgradeBonus = 2m;

    public DaggerBurst() : base(-1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModDAGGER);
        
        // 註冊變量，預設為 -1 以便在圖鑑顯示 "X"
        JJCustomVar("X", -1m);
        JJDamage(0m, ValueProp.Move);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 1. 獲取 Rank 加成
        int daggerRank = JiangXiaoUtils.GetDaggerRank(player);
        decimal rankBonus = (daggerRank * DamagePerRank) + (IsUpgraded ? UpgradeBonus : 0);

        // 2. 判斷戰鬥狀態
        // player?.PlayerCombatState 在圖鑑、商店預覽、選牌獎勵時通常為 null
        if (player?.PlayerCombatState != null)
        {
            int currentEnergy = player.PlayerCombatState.Energy;
            DynamicVars["X"].BaseValue = (decimal)currentEnergy;
            
            // 戰鬥中：顯示 能量 + Rank加成 (例如：3能量 + 2Rank = 顯示 5)
            DynamicVars.Damage.BaseValue = (decimal)currentEnergy + rankBonus;
        }
        else
        {
            // 圖鑑中：設定為 -1 作為信號
            DynamicVars["X"].BaseValue = -1m;
            
            // 此時 Damage 只顯示 Rank 帶來的基礎部分
            DynamicVars.Damage.BaseValue = rankBonus;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        var x = ResolveEnergyXValue();
        
        // 實際計算：X + Rank基礎傷害
        int daggerRank = JiangXiaoUtils.GetDaggerRank(Owner);
        decimal baseDmgFromRank = (daggerRank * DamagePerRank) + (IsUpgraded ? UpgradeBonus : 0);
        decimal finalDmgPerHit = x + baseDmgFromRank;

        if (x > 0)
        {
            await DamageCmd.Attack(finalDmgPerHit)
                .FromCard(this)
                .WithHitCount(x) 
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    // 保持你發現的強制刷新機制，這對瞄準時更新能量非常有幫助
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        UpdateStatsBasedOnRank();
        return base.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource);
    }

    protected override void OnUpgrade()
    {
        UpdateStatsBasedOnRank();
    }
}