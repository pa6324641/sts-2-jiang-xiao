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
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public class StarPowerArrow : JiangXiaoCardModel
{  
    // 數值平衡常數 (放在類別頂部方便調整)
    private const decimal DefaultBase = 3m;    // 未升級基礎
    private const decimal UpgradedBase = 6m;   // 升級後基礎
    private const decimal BowGrowth = 3m;      // 每級弓箭成長

    public StarPowerArrow() : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // 1. 添加標籤
        // CanonicalKeywords.Add(JiangXiaoModKeywords.JiangXiaoModBOW);
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModBOW);
        // 2. 初始化原生 Damage 變量 (會被 JJDamage 自動加入 _customVars)
        JJDamage(DefaultBase, ValueProp.Move);
    }

    /// <summary>
    /// 核心邏輯：將所有動態加成直接算進原生 {Damage} 中
    /// 公式：(基礎[3或6] + 弓Rank * 3) * 星力等級
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取弓箭等級
        int bowRank = JiangXiaoUtils.GetBowRank(player);
        
        // 1. 根據升級狀態決定「起始基礎值」
        decimal currentBase = IsUpgraded ? UpgradedBase : DefaultBase;
        
        // 2. 計算包含弓箭成長的「複合基礎值」
        decimal compoundBase = currentBase + (bowRank * BowGrowth);
        
        // 3. 直接更新原生 Damage 的 BaseValue (乘以星力等級)
        // 這樣在 Localization 中使用 {Damage:diff()} 就會直接顯示最終結果
        DynamicVars.Damage.BaseValue = compoundBase * skillRank;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出時確保數值最新
        UpdateStatsBasedOnRank();
        
        if (cardPlay.Target == null) return;

        // 執行攻擊，直接讀取計算後的 BaseValue
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            // .WithHitFx("vfx/vfx_hit_star") 
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 1. 費用 4 -> 3
        EnergyCost.UpgradeBy(-1);
        
        // 2. 刷新數值。
        // 因為 ApplyRankLogic 內部會判斷 IsUpgraded，
        // 所以呼叫 UpdateStatsBasedOnRank 時，傷害會自動從 3 起跳變成 6 起跳。
        UpdateStatsBasedOnRank();
    }
}