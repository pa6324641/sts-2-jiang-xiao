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

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public class DaggerAttack : JiangXiaoCardModel
{
    // 基礎數值定義
    private const decimal BaseHits = 1m;
    private const decimal BaseDamage = 1m;
    private const decimal UpgradeDamageBonus = 2m; // 升級增加基礎傷害，使 Rank 成長更強力

    public DaggerAttack() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        // 設置關鍵字：匕首
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModDAGGER);
        
        // 初始傷害設定
        JJDamage(BaseDamage, ValueProp.Move);
        
        // 設置自定義變量 M 代表攻擊次數
        JJCustomVar("M", BaseHits);
    }

    /// <summary>
    /// 核心等級邏輯：根據「匕首」技藝等級動態更新數值
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取「匕首」專用等級 (參考 JiangXiaoUtils)
        int daggerRank = JiangXiaoUtils.GetDaggerRank(player);

        // 1. 計算傷害：傷害 = 基礎(1) + 等級加成 + (升級紅利)
        // 當 Rank 為 2 且未升級時，傷害為 3
        decimal finalDmg = BaseDamage + (decimal)daggerRank;
        if (IsUpgraded)
        {
            finalDmg += UpgradeDamageBonus;
        }
        DynamicVars.Damage.BaseValue = finalDmg;

        // 2. 計算次數 M：M = 1 + 等級加成
        // 當 Rank 為 2 時，M 為 3
        DynamicVars["M"].BaseValue = BaseHits + (decimal)daggerRank;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 確保目標存在
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 獲取當前經過戰鬥加成（如力量）計算後的次數與傷害
        // 使用 PreviewValue 以確保 UI 顯示與實際效果一致
        int hitCount = (int)DynamicVars["M"].PreviewValue;
        
        // 執行多段攻擊
        for (int i = 0; i < hitCount; i++)
        {
            // 這裡使用 DamageCmd 執行單次攻擊
            // STS2 BaseLib 推薦在多段攻擊中逐次觸發，以正確觸發遺物或能力的「每次受到攻擊」效果
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash") // 匕首揮擊特效
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        // 升級效果：此處設計為增加傷害紅利，也可改為降低費用
        // 如果需要降低費用，可取消下方註釋：
        // EnergyCost.UpgradeBy(-1);

        // 手動觸發一次數值刷新，確保卡牌在商店或獎勵界面預覽正確
        UpdateStatsBasedOnRank();
    }
}