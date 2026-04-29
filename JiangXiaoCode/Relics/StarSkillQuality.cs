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

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarSkillQuality : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public enum QualityRank { Bronze = 1, Silver = 2, Gold = 3, Platinum = 4, Diamond = 5, CandleMoon = 6, ScorchingSun = 7 }

    private const string VarRank = "rank"; 

    // 用於強制刷新 UI 文字的反射字段
    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    public int SkillRank => (int)GetRank();

    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();

    /// <summary>
    /// 獲取技能點數
    /// 修正：在 STS2 中，直接使用 Owner 屬性是區分多人模式不同玩家的最穩定方式。
    /// </summary>
    public int GetPoints()
    {
        // 1. 檢查遺物是否有持有者 (Owner)
        // 在戰鬥或地圖移動時，持有此遺物的玩家會被正確賦值給 Owner。
        // 這能確保在多人模式下，每個玩家的遺物都只檢查自己的點數。
        if (Owner != null)
        {
            // 使用你的工具類獲取該玩家身上的星圖遺物
            var mainRelic = JiangXiaoUtils.GetStarMap(Owner);
            return mainRelic?.JiangXiaoMod_SkillPoints ?? 0;
        }

        // 2. 如果 Owner 為空，通常發生在圖鑑 (Compendium) 或商店預覽。
        // 此時回傳 0 以避免空引用崩潰。
        return 0;
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

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // STS2 會調用此處來獲取數值並渲染到描述文本中
            yield return new DynamicVar(VarRank, (decimal)SkillRank);
        }
    }

    public override Task BeforeCombatStart()
    {
        // 戰鬥開始前刷新一次顯示，確保等級是最新的
        RefreshDisplay();
        return Task.CompletedTask;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        // 獲得遺物時刷新顯示
        RefreshDisplay();
    }

    public override bool ShouldReceiveCombatHooks => true;

    /// <summary>
    /// 強制清除動態變量緩存，促使遊戲重新讀取 CanonicalVars 並更新 UI 文本。
    /// </summary>
    public void RefreshDisplay()
    {
        try 
        {
            // 清空緩存後，下一次 UI 渲染就會重新觸發 CanonicalVars 的 get 訪問器
            DynamicVarsField?.SetValue(this, null);
        }
        catch (Exception)
        {
            // 靜默處理反射異常
        }
    }
}