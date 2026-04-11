using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Logging;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarPowerLevel : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 定義變數名稱，須與 relics.json 中的 {level}, {energy}, {points} 一致
    private const string VarLevel = "level";
    private const string VarEnergy = "energy";
    private const string VarPoints = "points";

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int LastAppliedLevel { get; set; } = 0;

    protected override string IconBaseName => "star_power_level";

    // 反射快取欄位，用於強制重新計算描述變量
    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// 獲取當前技能點數 (支援圖鑑預覽)
    /// </summary>
    public int GetPoints()
    {
        // [關鍵] 絕對不要訪問 this.Owner，因為在圖鑑模式下訪問它會觸發 AssertMutable 崩潰。
        // 我們直接透過 RunManager 獲取當前運行的狀態。
        var runState = RunManager.Instance?.DebugOnlyGetState();
        
        // 如果不在冒險中（例如在主選單、圖鑑），runState 會是 null
        if (runState == null) return 0;

        // 嘗試獲取當前玩家（通常是第一個玩家）
        var player = runState.Players.FirstOrDefault();
        if (player == null) return 0;

        // 從玩家的遺物清單中尋找「內視星圖」
        var mainRelic = player.Relics.FirstOrDefault(r => 
            r is InnerStarMap || 
            r.Id.Entry.Equals("INNER_STAR_MAP", StringComparison.OrdinalIgnoreCase)) as InnerStarMap;

        return mainRelic?.JiangXiaoMod_SkillPoints ?? 0;
    }

    public int GetLevel()
    {
        int points = GetPoints();

        // 等級判定閾值（維持原邏輯）
        if (points < 5000) return 1;
        if (points < 20000) return 2;
        if (points < 30000) return 3;
        if (points < 40000) return 4;
        if (points < 45000) return 5;
        return 6;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 💡 關鍵修正：確保在圖鑑模式下，即使 GetPoints 回傳 0，也要產出對應的變數
            int currentLevel = GetLevel();
            int currentPoints = GetPoints();

            // 若在圖鑑中希望顯示「基礎等級 1」
            yield return new DynamicVar(VarLevel, (decimal)currentLevel);
            yield return new DynamicVar(VarEnergy, (decimal)currentLevel); // 顯示能量
            yield return new DynamicVar(VarPoints, (decimal)currentPoints);
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        int level = GetLevel();
        // 需求：rank1=4, rank2=5, rank3=6... 公式為 Level + 3
        decimal targetMaxEnergy = (decimal)(level + 3);

        if (player.MaxEnergy < targetMaxEnergy)
        {
            decimal amountToAdd = targetMaxEnergy - player.MaxEnergy;
            if (amountToAdd > 0)
            {
                Flash();
                // 修正：STS2 中直接操作屬性並記錄日誌，確保數據同步
                player.MaxEnergy += (int)amountToAdd;
                
                // 額外獲得當前能量，確保當回合可用
                await PlayerCmd.GainEnergy(amountToAdd, player);
            }
        }
        
        await base.AfterPlayerTurnStart(choiceContext, player);
    }

    public override Task BeforeCombatStart()
    {
        LastAppliedLevel = GetLevel();
        RefreshDisplay(); 
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
    /// 強制刷新遺物顯示數據 (當點數變動時調用)
    /// </summary>
    public void RefreshDisplay()
    {
        // 清除 _dynamicVars 快取，迫使 UI 重新調用 CanonicalVars 獲取最新數值
        DynamicVarsField?.SetValue(this, null);
        this.Status = this.Status; 
    }
}