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
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Character;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarSkillQuality : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override string IconBaseName => "star_skill_quality";

    public enum QualityRank { Bronze = 1, Silver = 2, Gold = 3, Platinum = 4, Diamond = 5, CandleMoon = 6, ScorchingSun = 7 }

    private const string VarRank = "rank"; 
    private const string VarMult = "multiplier";

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    public int SkillRank => (int)GetRank();

    public override bool ShouldReceiveCombatHooks => true;

    /// <summary>
    /// 核心修正：安全地獲取技能點數，完全避開 this.Owner 以防止圖鑑崩潰
    /// </summary>
    public int GetPoints()
    {
        // [關鍵] 不使用 Owner 屬性，改用 RunManager 獲取當前運行的狀態
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null) return 0; // 圖鑑模式下會直接返回 0

        var player = runState.Players.FirstOrDefault();
        if (player == null) return 0;

        // 尋找內視星圖獲取點數
        var mainRelic = player.Relics.FirstOrDefault(r => 
            r is InnerStarMap || 
            r.Id.Entry.Equals("INNER_STAR_MAP", StringComparison.OrdinalIgnoreCase)) as InnerStarMap;
            
        return mainRelic?.JiangXiaoMod_SkillPoints ?? 0;
    }

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

    public float GetValueMultiplier()
    {
        return 1.0f + (SkillRank - 1) * 0.5f;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 在圖鑑模式下，GetPoints() 為 0，SkillRank 會回傳 Bronze (1)
            // 這能讓圖鑑正常顯示初始狀態的數值
            int currentRank = SkillRank;
            yield return new DynamicVar(VarRank, (decimal)currentRank);
            yield return new DynamicVar(VarMult, (decimal)(GetValueMultiplier() * 100));
        }
    }

    public static float GetCurrentMultiplier(IRunState runState)
    {
        var player = runState?.Players.FirstOrDefault();
        var relic = player?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        return relic?.GetValueMultiplier() ?? 1.0f;
    }

    public override Task BeforeCombatStart()
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        DynamicVarsField?.SetValue(this, null);
        this.Status = this.Status; 
    }
}