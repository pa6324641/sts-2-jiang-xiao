using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using BaseLib.Abstracts;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Rare;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public class ZhongLing : JiangXiaoCardModel
{
    // 定義變數標籤
    private const string VarHeal = "Heal";
    private const string VarHits = "Magic";

    public ZhongLing() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        // [STS2_Optimization] 使用基類輔助方法初始化
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        
        // 初始數值設定 (對應 1 級星技)
        JJCustomVar(VarHeal, 6m);
        JJCustomVar(VarHits, 4m);
    }

    // 懸浮提示：承印
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<ChengYin>()
    ];

    protected override void OnUpgrade()
    {
        // 費用 2 -> 1
        EnergyCost.UpgradeBy(-1);
        // 升級後重新計算基於等級的數值
        UpdateStatsBasedOnRank(); 
    }

    /// <summary>
    /// 核心數值縮放邏輯：當星技等級改變時觸發
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 1. 計算基礎值 (處理升級與未升級的區分)
        decimal baseHeal = IsUpgraded ? 9m : 6m;
        decimal baseHits = IsUpgraded ? 5m : 4m;

        // 2. 根據等級縮放 (每級提升：治療+3, 次數+1)
        int rankBonus = Math.Max(0, skillRank - 1);
        decimal finalHeal = baseHeal + (rankBonus * 3m);
        decimal finalHits = baseHits + (rankBonus * 1m);

        // 3. 更新 DynamicVars，確保 UI 描述同步
        if (DynamicVars.TryGetValue(VarHeal, out var hVar)) hVar.BaseValue = finalHeal;
        if (DynamicVars.TryGetValue(VarHits, out var mVar)) mVar.BaseValue = finalHits;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null) return;

        // 1. 獲取數值 (這部分邏輯保持優化後的，確保等級縮放有效)
        decimal healAmt = DynamicVars.TryGetValue(VarHeal, out var hV) ? hV.PreviewValue : 6m;
        int hitCount = (int)(DynamicVars.TryGetValue(VarHits, out var mV) ? mV.PreviewValue : 4);

        // 2. 目標篩選
        var alliesWithChengYin = combat.Allies
            .Where(c => c.Powers.Any(p => p is ChengYinPower))
            .ToList();
        
        var finalPool = alliesWithChengYin.Any() 
            ? alliesWithChengYin.Cast<Creature>().ToList() 
            : combat.Allies.Concat(combat.Enemies).Cast<Creature>().ToList();

        if (finalPool.Count == 0) return;

        // 1. 獲取狀態 (使用 ? 確保安全)
        var runState = RunManager.Instance.DebugOnlyGetState();

        // 2. 進行安全檢查：如果狀態或 RNG 序列不存在，則中斷執行
        if (runState?.Rng?.CombatTargets == null)
        {
            // 這通常不會發生在戰鬥中，但加上檢查可以消除編譯錯誤 CS8602
            return; 
        }
        // 3. 此時編譯器知道 rng 一定不為 null
        var rng = runState.Rng.CombatTargets;

        // 4. 執行多次隨機治療
        for (int i = 0; i < hitCount; i++)
        {
            var target = finalPool[rng.NextInt(finalPool.Count)];

            // 執行治療動作，showEffect 為 true 會產生視覺特效
            await CreatureCmd.Heal(target, healAmt, true);

            // 視覺間隔
            if (hitCount > 1 && i < hitCount - 1)
            {
                await Task.Delay(100);
            }
        }
    }
}