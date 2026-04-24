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

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class BasicArrow : JiangXiaoCardModel
{
    // [優化] 定義 CardId 確保與 localization (cards.json) 匹配
    public const string CardId = "JIANGXIAOMOD-BASIC_ARROW";
    // public const string M = "M";

    public BasicArrow() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {        
        // ID 會自動關聯，基類會處理 PortraitPath
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModBOW);
        JJCustomVar("M", 1m);
        JJDamage(2m,ValueProp.Move);
    }

    // [優化] 關鍵字定義
    // public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModBOW];

    // protected override IEnumerable<DynamicVar> CanonicalVars => [
    //     new DamageVar(2m, ValueProp.Move), // 初始基礎傷害 2
    //     new DynamicVar("M", 1m)           // 抽牌數量
    // ];

    /// <summary>
    /// 根據「弓箭技藝」等級更新傷害。
    /// 提示：JiangXiaoCardModel 會在 OnCheckStats 時自動呼叫此方法，無需在 BeforeCombatStart 手動觸發。
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取特定的弓箭等級 (BasicArtsRelic)
        int bowRank = JiangXiaoUtils.GetBowRank(player);
        
        // 邏輯：基礎 2 + (弓箭等級 * 2)。若等級 1，傷害為 4；等級 2，傷害為 6。
        // [注意] 升級加成會由 UpgradeValue 另行累加，此處僅處理基礎縮放
        DynamicVars.Damage.BaseValue = 2m + (bowRank * 2m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 參數檢查
        if (cardPlay.Target == null) return;

        // 2. 執行攻擊動作
        // 使用 "vfx/vfx_arrow_impact" (如果有) 或通用遠程 VFX
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") // STS2 目前建議先用通用，未來可換成 arrow 類
            .Execute(choiceContext);

        // 3. 定向抽牌邏輯
        var player = this.Owner;
        var combatState = player?.PlayerCombatState;

        if (combatState != null)
        {
            var drawPile = combatState.DrawPile;

            // [邏輯優化] 尋找抽牌堆中「第一張」具有弓類關鍵字的牌
            // 排除自身（雖然攻擊後通常已在 PlayContainer，但排除是好習慣）
            var targetCard = drawPile.Cards.FirstOrDefault(c => 
                c != this && c.CanonicalKeywords.Contains(JiangXiaoModKeywords.JiangXiaoModBOW));

            if (targetCard != null)
            {
                // [STS2_API] 將卡牌從抽牌堆移動到手牌
                // 這會自動觸發相關 Hook 並處理 UI 動畫
                await CardPileCmd.Add(targetCard, PileType.Hand, CardPilePosition.Top, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升級效果：傷害基礎提升 4，耗能降低 1
        DynamicVars.Damage.UpgradeValueBy(4m);
        EnergyCost.UpgradeBy(-1);
    }
}