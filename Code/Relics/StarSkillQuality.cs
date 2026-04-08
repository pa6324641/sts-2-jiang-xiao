using System;
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Relics;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using System.Reflection;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarSkillQuality : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override string IconBaseName => "star_skill_quality";

    // 🌟 恢復品質階級 Enum，並明確綁定 1~7 的數值
    public enum QualityRank { Bronze = 1, Silver = 2, Gold = 3, Platinum = 4, Diamond = 5, CandleMoon = 6, ScorchingSun = 7 }

    private const string VarRank = "rank"; 
    private const string VarMult = "multiplier";

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    // 🌟 讓卡片可以直接調用此屬性來獲取當前等級的數字 (1-7)
    public int SkillRank => (int)GetRank();

    // 🌟 必須開啟這個，遺物才能接收戰鬥開始/結束等 Hooks
    public override bool ShouldReceiveCombatHooks => true;

    public StarSkillQuality() : base()
    {
    }

    // 穩健抓取 InnerStarMap 的點數
    public int GetPoints()
    {
        var player = Owner ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();
        var mainRelic = player?.Relics?.FirstOrDefault(r => r is InnerStarMap) as InnerStarMap;
        return mainRelic?.JiangXiaoMod_SkillPoints ?? 0;
    }

    // 🌟 核心邏輯：回傳 QualityRank 枚舉，供外部卡片做語義化判斷
    public QualityRank GetRank()
    {
        int points = GetPoints();
        
        if (points < 5000) return QualityRank.Bronze;
        if (points < 10000) return QualityRank.Silver;
        if (points < 20000) return QualityRank.Gold;
        if (points < 30000) return QualityRank.Platinum;
        if (points < 40000) return QualityRank.Diamond;
        if (points < 50000) return QualityRank.CandleMoon;
        return QualityRank.ScorchingSun;
    }

    // 計算倍率 (1級=1.0, 2級=1.5, 3級=2.0...)
    public float GetValueMultiplier()
    {
        return 1.0f + (SkillRank - 1) * 0.5f;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 將 Enum 轉為整數 (1~7) 拋給 JSON 的 choose 語法
            yield return new DynamicVar(VarRank, (decimal)SkillRank);
            
            // 傳遞整數百分比，例如 1.5f -> 150
            yield return new DynamicVar(VarMult, (decimal)(GetValueMultiplier() * 100));
        }
    }

    // 靜態工具函數：方便在沒有遺物實例的情況下透過 runState 獲取倍率
    public static float GetCurrentMultiplier(IRunState runState)
    {
        var player = runState?.Players.FirstOrDefault();
        var relic = player?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        return relic?.GetValueMultiplier() ?? 1.0f;
    }

    // 🌟 新增：戰鬥開始前強制刷新，確保讀檔或進入新戰鬥時數值正確
    public override Task BeforeCombatStart()
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }

    // 🌟 新增：剛獲得該遺物時強制刷新
    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        RefreshDisplay();
    }

    /// <summary>
    /// 強制刷新遺物顯示與數據
    /// </summary>
    public void RefreshDisplay()
    {
        // 1. 清除私有動態變數快取，強制重新讀取 CanonicalVars
        DynamicVarsField?.SetValue(this, null);
        
        // 2. 觸發狀態更新，告訴 STS2 該刷新 UI 了
        this.Status = this.Status; 
    }
}