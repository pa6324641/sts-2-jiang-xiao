using System;
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;
using System.Reflection;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Map;
using System.Drawing;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarSkillQuality : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    // protected override string IconBaseName => "star_skill_quality";

    // 這裡定義的 Rank 順序與你的點數判定邏輯一致
    public enum QualityRank { Bronze = 1, Silver = 2, Gold = 3, Platinum = 4, Diamond = 5, CandleMoon = 6, ScorchingSun = 7 }

    private const string VarRank = "rank"; 
    private const string VarMult = "multiplier";

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    public int SkillRank => (int)GetRank();

    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();

    /// <summary>
    /// 安全地獲取技能點數 (相容圖鑑、讀檔預覽、多人模式)
    /// </summary>
    public int GetPoints()
    {
        // 優先從運行狀態獲取，若無則嘗試從當前 Owner 獲取（這能覆蓋大多數情況）
        var runState = RunManager.Instance?.DebugOnlyGetState();
        var player = runState?.Players.FirstOrDefault() ?? Owner;
        
        if (player == null) return 0;

        // 從目標玩家的遺物中尋找核心遺物 InnerStarMap
        // 原本需要寫 if (null) 的邏輯現在簡化為：
        var mainRelic = JiangXiaoUtils.GetStarMap(player);

        if (mainRelic != null)
        {
            // 直接訪問介面定義的屬性，不管是基礎版還是升級版都通用
            int points = mainRelic.JiangXiaoMod_SkillPoints;
            return points;
        }
        else
        {
            return 0;
        }
    }
    

    public QualityRank GetRank()
    {
        int points = GetPoints();
        
        // 依照你設定的階級閾值
        if (points < 5000) return QualityRank.Bronze;
        if (points < 10000) return QualityRank.Silver;
        if (points < 20000) return QualityRank.Gold;
        if (points < 30000) return QualityRank.Platinum;
        if (points < 40000) return QualityRank.Diamond;
        if (points < 50000) return QualityRank.CandleMoon;
        return QualityRank.ScorchingSun;
    }

    // public float GetValueMultiplier()
    // {
    //     // 公式：1.0, 1.5, 2.0... 每階提升 50%
    //     return 1.0f + (SkillRank - 1) * 0.5f;
    // }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            int currentRank = SkillRank;
            // 傳遞 Rank 給 choose 格式化器，傳遞 Multiplier * 100 給描述顯示百分比
            yield return new DynamicVar(VarRank, (decimal)currentRank);
            // yield return new DynamicVar(VarMult, (decimal)(GetValueMultiplier() * 100));
        }
    }

    /// <summary>
    /// 供卡牌或其他系統靜態調用的倍率接口
    /// </summary>
    // public static float GetCurrentMultiplier(IRunState? runState)
    // {
    //     var player = runState?.Players.FirstOrDefault();
    //     var relic = player?.Relics.OfType<StarSkillQuality>().FirstOrDefault();
    //     return relic?.GetValueMultiplier() ?? 1.0f;
    // }

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

    public override bool ShouldReceiveCombatHooks => true;

    public void RefreshDisplay()
    {
        // 清除快取以強制刷新敘述中的 {rank} 與 {multiplier}
        DynamicVarsField?.SetValue(this, null);
    }
}