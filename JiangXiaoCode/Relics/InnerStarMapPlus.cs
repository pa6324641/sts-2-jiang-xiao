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
using MegaCrit.Sts2.Core.Nodes.Rooms; // 建議檢查命名空間是否為 Nodes.Rooms
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class InnerStarMapPlus : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    private int _skillPoints = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int JiangXiaoMod_SkillPoints 
    { 
        get => _skillPoints; 
        set 
        {
            if (_skillPoints == value) return; // 數值沒變就跳過，減少反射開銷
            _skillPoints = value;

            // [優化 1]：透過 setter 自動觸發連動刷新，確保數據一致性
            var player = Owner ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();
            if (player != null)
            {
                // 使用 OfType<T> 效能優於 Is / As 判斷
                player.Relics.OfType<StarSkillQuality>().FirstOrDefault()?.RefreshDisplay();
                player.Relics.OfType<StarPowerLevel>().FirstOrDefault()?.RefreshDisplay();
                // 如果有 BasicArts 也需要連動，可以在此加入
                player.Relics.OfType<BasicArts>().FirstOrDefault()?.RefreshDisplay();
            }
            RefreshDynamicText();
        }
    }

    private const string VarPoints = "points";
    protected override string IconBaseName => "inner_star_map";
    public override bool ShouldReceiveCombatHooks => true;

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    public void RefreshDynamicText()
    {
        DynamicVarsField?.SetValue(this, null);
    }

    /// <summary>
    /// 提供給外部（如 Utils）使用的靜態獲取方法
    /// </summary>
    public static int GetSkillPoints(IRunState? runState)
    {
        var player = runState?.Players.FirstOrDefault();
        var relic = player?.Relics.OfType<InnerStarMap>().FirstOrDefault();
        return relic?.JiangXiaoMod_SkillPoints ?? 0;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        int gain = 2500;
        if (room != null)
        {
            if (room.RoomType == RoomType.Boss) gain = 10000;
            else if (room.RoomType == RoomType.Elite) gain = 5000;
        }

        // [優化 2]：利用歷史記錄系統同步變動 (多人模式關鍵)
        if (Owner?.Creature?.CombatState != null)
        {
            CombatManager.Instance.History.StarsModified(Owner.Creature.CombatState, gain, Owner);
        }

        // [優化 3]： setter 內部已經包含了刷新連動遺物的邏輯
        // 此處只需單純修改數值即可，不需要再寫 redundant 的手動刷新代碼
        JiangXiaoMod_SkillPoints += gain;

        Flash(); 
        return Task.CompletedTask;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar(VarPoints, (decimal)JiangXiaoMod_SkillPoints);
        }
    }

    public override RelicModel? GetUpgradeReplacement()
    {
        var upgraded = ModelDb.Relic<InnerStarMapPlus>();
        
        if (upgraded is InnerStarMapPlus plusRelic)
        {
            plusRelic.JiangXiaoMod_SkillPoints = this.JiangXiaoMod_SkillPoints;
        }
        
        return upgraded;
    }
}