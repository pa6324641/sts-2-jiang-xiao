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
using MegaCrit.Sts2.Core.Commands; 
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using HarmonyLib;
using System.Reflection; // 必須引用以進行快取清理

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class InnerStarMap : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // --- 核心修正：使用帶有 Backing Field 的屬性 ---
    private int _skillPoints = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int JiangXiaoMod_SkillPoints 
    { 
        get => _skillPoints; 
        set 
        {
            _skillPoints = value;
            // 當數值被設定時（包含讀檔注入），自動清空 UI 快取
			// --- 新增：強制連動刷新夥伴 ---
        // 確保當點數變動時，主動去推動品質遺物與等級遺物
        var player = Owner ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();
        if (player != null)
        {
            player.Relics.OfType<StarSkillQuality>().FirstOrDefault()?.RefreshDisplay();
            player.Relics.OfType<StarPowerLevel>().FirstOrDefault()?.RefreshDisplay();
        }
            RefreshDynamicText();
        }
    }

    private const string VarPoints = "points";
    protected override string IconBaseName => "inner_star_map";
    public override bool ShouldReceiveCombatHooks => true;

    // 取得快取清理的欄位資訊 (Static 以增進效能)
    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// 強制清空 STS2 的動態文字快取，使下次顯示時重新讀取 CanonicalVars
    /// </summary>
    public void RefreshDynamicText()
    {
        DynamicVarsField?.SetValue(this, null);
    }

    public static int GetSkillPoints(IRunState runState)
    {
        var player = runState?.Players.FirstOrDefault();
        if (player == null) return 0;

        var relic = player.Relics.FirstOrDefault(r => r is InnerStarMap) as InnerStarMap;
        return relic?.JiangXiaoMod_SkillPoints ?? 0;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        int gain = 1250;
        if (room != null)
        {
            if (room.RoomType == RoomType.Boss) gain = 5000;
            else if (room.RoomType == RoomType.Elite) gain = 2500;
        }

        // 這裡會觸發 setter 裡面的 RefreshDynamicText()
        JiangXiaoMod_SkillPoints += gain;

        // 刷新其他連動遺物
        if (Owner != null)
        {
            var qualityRelic = Owner.Relics.OfType<StarSkillQuality>().FirstOrDefault();
            qualityRelic?.RefreshDisplay(); // 假設你的其他遺物也有自定義刷新函數

            var levelRelic = Owner.Relics.OfType<StarPowerLevel>().FirstOrDefault();
            levelRelic?.RefreshDisplay();
        }

        Flash(); 
        return Task.CompletedTask;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // STS2 會在清空快取後重新來這裡拿最新的 SkillPoints
            yield return new DynamicVar(VarPoints, (decimal)JiangXiaoMod_SkillPoints);
        }
    }
}