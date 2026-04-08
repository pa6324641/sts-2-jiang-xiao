using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs; 
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands; 
using MegaCrit.Sts2.Core.Context;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Reflection; // 必須引用以進行快取清理

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarPowerLevel : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    private const string VarLevel = "level";
    private const string VarEnergy = "energy";
    private const string VarPoints = "points"; // 用於對應 InnerStarMap 的技能點顯示

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int LastAppliedLevel { get; set; } = 0;

    protected override string IconBaseName => "star_power_level";

    // 優化效能：將反射欄位設為靜態，避免每次刷新重複取得
    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    // 核心修正：更穩健的遺物搜尋方式 (支援預覽與圖鑑)
    public int GetPoints()
    {
        // STS2 穩健寫法：優先找 Owner，若為空則從全域 RunManager 找當前玩家
        var relics = Owner?.Relics ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault()?.Relics;
        
        if (relics != null)
        {
            var mainRelic = relics.FirstOrDefault(r => 
                r is InnerStarMap || 
                r.Id.Entry.Equals("INNER_STAR_MAP", StringComparison.OrdinalIgnoreCase)) as InnerStarMap;

            if (mainRelic != null)
            {
                return mainRelic.JiangXiaoMod_SkillPoints;
            }
        }
        return 0;
    }

    public int GetLevel()
    {
        int points = GetPoints();

        // 等級判定閾值
        if (points < 5000) return 1;  // 星塵
        if (points < 20000) return 2; // 星雲
        if (points < 30000) return 3; // 星河
        if (points < 40000) return 4; // 星海
        if (points < 45000) return 5; // 星空
        return 6;                     // 星盡
    }

    public int GetEnergyBonus() => GetLevel(); // 等級 1 加 1 點，等級 2 加 2 點...

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            int currentLevel = GetLevel();
            yield return new DynamicVar(VarLevel, (decimal)currentLevel);
            yield return new DynamicVar(VarEnergy, (decimal)GetEnergyBonus());
            yield return new DynamicVar(VarPoints, (decimal)GetPoints());
        }
    }

    //當前能量
    // public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    // {
    //     int bonus = GetEnergyBonus();
    //     if (bonus > 0)
    //     {
    //         Flash();
    //         await PlayerCmd.GainEnergy((decimal)bonus, player);
    //     }
    //     await base.AfterPlayerTurnStart(choiceContext, player);
    // }

    
    // public override decimal ModifyMaxEnergy(Player player, decimal amount)
    // {
    //     return base.ModifyMaxEnergy(player, amount);
    // }

    //最大能量
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        int level = GetLevel();
        // 根據需求：rank1=4, rank2=5, rank3=6... 公式為 Level + 3
        decimal targetMaxEnergy = (decimal)(level + 3);

        // 判斷是否需要補足最大能量
        if (player.MaxEnergy < targetMaxEnergy)
        {
            decimal amountToAdd = targetMaxEnergy - player.MaxEnergy;

            if (amountToAdd > 0)
            {
                Flash();
                
                // 呼叫我們改名後的輔助方法
                await AddMaxEnergyLevel(player, amountToAdd);
                
                // 同步增加當前能量，讓玩家這回合就能用到
                await PlayerCmd.GainEnergy(amountToAdd, player);
            }
        }
        
        await base.AfterPlayerTurnStart(choiceContext, player);
    }
    public static Task AddMaxEnergyLevel(Player player, decimal amount)
    {
        if (player != null)
        {
            // STS2 中 MaxEnergy 是 decimal，直接累加即可
            player.MaxEnergy += (int)amount;
        }
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        LastAppliedLevel = GetLevel();
        RefreshDisplay(); // 進入戰鬥時刷新一次，確保數值正確
        return Task.CompletedTask;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        LastAppliedLevel = GetLevel();
        RefreshDisplay();
    }

    public override bool ShouldReceiveCombatHooks => true;

    /// <summary>
    /// 強制刷新遺物顯示與數據
    /// </summary>
    public void RefreshDisplay()
    {
        // 使用靜態欄位清空緩存，讓 UI 重新調用 CanonicalVars
        DynamicVarsField?.SetValue(this, null);
        this.Status = this.Status;
    }
}