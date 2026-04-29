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

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class DaggerAllOutStrike : JiangXiaoCardModel
{
    protected override bool HasEnergyCostX => true;

    public DaggerAllOutStrike() : base(-1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModDAGGER);
        
        // 註冊 X 變量
        JJCustomVar("X", -1m);
        
        // 初始化傷害，ValueProp.Move 增加打擊感
        JJDamage(0m, ValueProp.Move);
    }

    /// <summary>
    /// [核心準則]：所有傷害數值來源必須由此處定義，確保圖鑑、手牌、預覽完全一致。
    /// 公式：(X + 匕首Rank + 星力等級) * 匕首Rank
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 1. 獲取核心數值 (匕首等級、星力等級)
        int daggerRank = JiangXiaoUtils.GetDaggerRank(player);
        int starLevel = JiangXiaoUtils.GetPowerLevel(player); 

        // 2. 確定當前 X 值 (戰鬥中取當前能量，非戰鬥取 0 或圖鑑默認)
        decimal xValue = 0m;
        if (player?.PlayerCombatState != null)
        {
            xValue = (decimal)player.PlayerCombatState.Energy;
            // 更新描述中的 X 顯示
            DynamicVars["X"].BaseValue = xValue;
        }
        else
        {
            // 非戰鬥狀態 (圖鑑預覽)
            DynamicVars["X"].BaseValue = -1m; // 顯示為 "X"
            xValue = 0m; 
        }

        // 3. 核心公式計算
        // [修正]：確保公式與 OnPlay 絕對一致
        decimal totalBaseDmg = (xValue + (decimal)daggerRank + (decimal)starLevel) * (decimal)daggerRank;
        
        // 4. 將計算結果寫入 Damage.BaseValue
        // 這樣 STS2 引擎會自動幫你計算：BaseValue + 力量 + 遺物加成 = 最終 PreviewValue
        DynamicVars.Damage.BaseValue = totalBaseDmg;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 1. 取得實際消耗的 X 能量
        var xSpent = ResolveEnergyXValue();
        
        // 2. [關鍵步奏]：手動更新一次數據，確保 Damage.BaseValue 使用的是「實際扣除前」或「傳入」的 X 值
        // 這裡我們直接獲取即時數值進行最終攻擊結算
        int daggerRank = JiangXiaoUtils.GetDaggerRank(Owner);
        int starLevel = JiangXiaoUtils.GetPowerLevel(Owner);
        
        // 再次設定 BaseValue，確保攻擊指令讀取的是正確的 X
        DynamicVars.Damage.BaseValue = (xSpent + (decimal)daggerRank + (decimal)starLevel) * (decimal)daggerRank;

        // 3. 執行攻擊
        // [STS2_Optimization]：使用 .Attack(DynamicVars.Damage) 而非傳入數值
        // 這樣引擎會自動抓取當前卡片的加成（力量、易傷等）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // 瞄準時強制刷新，確保敵人受到的預覽傷害準確
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        UpdateStatsBasedOnRank();
        return base.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource);
    }

    protected override void OnUpgrade()
    {
        // 升級時通常會提升數值，若有需要可在這裡增加升級邏輯
        DynamicVars.Damage.UpgradeValueBy(2m);
        UpdateStatsBasedOnRank();
    }
}