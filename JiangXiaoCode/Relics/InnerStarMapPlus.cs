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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class InnerStarMapPlus : CustomRelicModel, IInnerStarMap
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    // 用於升級時傳遞數據的靜態緩存
    public static int _transferPointsBuffer = -1;

    private int _skillPoints = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int JiangXiaoMod_SkillPoints 
    { 
        get => _skillPoints; 
        set 
        {
            if (_skillPoints == value) return;
            _skillPoints = value;

            // [重要]：利用反編譯代碼中確認的 IsMutable (或 IsCanonical) 進行檢查
            // 只有當遺物是「可變實例」且已有「擁有者」時才刷新 UI
            if (base.IsMutable && Owner != null) 
            {
                var player = Owner;
                player.Relics.OfType<StarSkillQuality>().FirstOrDefault()?.RefreshDisplay();
                player.Relics.OfType<StarPowerLevel>().FirstOrDefault()?.RefreshDisplay();
                player.Relics.OfType<BasicArts>().FirstOrDefault()?.RefreshDisplay();
            }
            RefreshDynamicText();
        }
    }

    private const string VarPoints = "points";
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    public override bool ShouldReceiveCombatHooks => true;

    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    public void RefreshDynamicText()
    {
        DynamicVarsField?.SetValue(this, null);
    }

    // [修正]：根據反編譯源碼第 431 行，正確的掛鉤是 AfterObtained
    public override Task AfterObtained()
    {
        // 檢查是否有從升級前傳遞過來的數據
        if (_transferPointsBuffer != -1)
        {
            this.JiangXiaoMod_SkillPoints = _transferPointsBuffer;
            _transferPointsBuffer = -1; // 使用後清除緩存
        }
        
        return base.AfterObtained(); // 返回基類的 Task (通常是 Task.CompletedTask)
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        int gain = 2500;
        if (room != null)
        {
            if (room.RoomType == RoomType.Boss) gain = 10000;
            else if (room.RoomType == RoomType.Elite) gain = 5000;
        }

        if (Owner?.Creature?.CombatState != null)
        {
            CombatManager.Instance.History.StarsModified(Owner.Creature.CombatState, gain, Owner);
        }

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

    public override RelicModel? GetUpgradeReplacement() => null;
}