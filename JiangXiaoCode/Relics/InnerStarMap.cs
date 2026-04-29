using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Powers.StarMaps;
using Godot;
using MegaCrit.Sts2.Core.Commands;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class InnerStarMap : CustomRelicModel, IInnerStarMap
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    private int _skillPoints = 0;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int JiangXiaoMod_SkillPoints 
    { 
        get => _skillPoints; 
        set 
        {
            if (_skillPoints == value) return;
            _skillPoints = value;

            // [安全修正]：根據反編譯源碼，增加 IsMutable 檢查
            // 避免在遊戲初始化藍圖時因觸發 Owner 邏輯而崩潰
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

    public static int GetSkillPoints(IRunState? runState)
    {
        var player = runState?.Players.FirstOrDefault();
        // [修正]：這裡應能獲取基礎版或升級版。利用 OfType<RelicModel> 並檢查 ID 較安全
        var relic = player?.Relics.FirstOrDefault(r => r.Id.Entry.Contains("INNER_STAR_MAP"));
        
        // 利用反射或轉型獲取點數，此處簡化處理
        if (relic is InnerStarMap normal) return normal.JiangXiaoMod_SkillPoints;
        if (relic is InnerStarMapPlus plus) return plus.JiangXiaoMod_SkillPoints;
        return 0;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        int gain = 625;
        if (room != null)
        {
            if (room.RoomType == RoomType.Boss) gain = 2500;
            else if (room.RoomType == RoomType.Elite) gain = 1250;
        }

        if (Owner?.Creature?.CombatState != null)
        {
            CombatManager.Instance.History.StarsModified(Owner.Creature.CombatState, gain, Owner);
        }

        JiangXiaoMod_SkillPoints += gain;
        Flash(); 
        return Task.CompletedTask;
    }
    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        int gain = 625;
        if (room.RoomType == RoomType.Event || room.RoomType == RoomType.Shop || room.RoomType == RoomType.RestSite || room.RoomType == RoomType.Treasure)
        {
            JiangXiaoMod_SkillPoints += gain;
            Flash(); 
        }
        return Task.CompletedTask;
    }


    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar(VarPoints, (decimal)JiangXiaoMod_SkillPoints);
        }
    }

    // [核心修正]：升級邏輯
    public override RelicModel? GetUpgradeReplacement()
    {
        // 1. 將當前實例的點數存入 InnerStarMapPlus 的靜態緩存
        InnerStarMapPlus._transferPointsBuffer = this.JiangXiaoMod_SkillPoints;
        
        // 2. 僅傳回升級版的藍圖範本
        // 系統會自動移除此遺物，並根據此藍圖建立新的 InnerStarMapPlus 實例
        return ModelDb.Relic<InnerStarMapPlus>();
    }

    // [新增]：偵測北斗九星能力的施加
    public override async Task BeforeCombatStart()
    {
        // 1. 檢查持有者是否已經擁有該能力（避免重複施加）
        // STS2 BaseLib 中，使用 HasPower<T>() 擴展方法
        if (!Owner.HasPower<BeiDouNineStarsPower>())
        {
            // 符合條件，在控制台輸出調試資訊
            GD.Print("江曉成功啟動了北斗九星！");
            
            // 2. 施加能力：目標為 Owner 的 Creature，層數為 1
            // [STS2_API] 使用 PowerCmd.Apply 異步執行
            await PowerCmd.Apply<BeiDouNineStarsPower>(Owner.Creature, 1, null, null);
            
            // 3. 觸發遺物閃爍視覺效果
            Flash();
        }

        await base.BeforeCombatStart();
    }
}