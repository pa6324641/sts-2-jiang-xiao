using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class InnerStarMap : AbstractInnerStarMap
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override string CardRewardAlternativeKey => "CONVERT_SKILL_POINTS_BASE";
    protected override int SkillPointsGain => 1000;

    // 配置基礎版的各項點數分配
    protected override int CombatVictoryBaseGain => 625;
    protected override int CombatVictoryEliteGain => 1250;
    protected override int CombatVictoryBossGain => 2500;
    protected override int RoomEnteredGain => 625;

    public override RelicModel? GetUpgradeReplacement()
    {
        // 升級時，將當前點數暫存至升級版的靜態緩存中
        InnerStarMapPlus._transferPointsBuffer = this.JiangXiaoMod_SkillPoints;
        return ModelDb.Relic<InnerStarMapPlus>();
    }
    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        
        // [STS2_BugFix] 說明：由於 RunManager.FinalizeStartingRelics() 在初始遺物時會使用 foreach 遍歷遺物列表，
        // 若在此處直接同步調用 RelicCmd.Obtain() 會向同一個列表添加新遺物（修改了正在枚舉的集合），導致集合修改異常。
        // 透過 Godot 的排程延遲調用 (CallDeferred)，將獲得附屬遺物的操作推遲到當前主執行緒初始化遺物遍歷完全結束後再安全執行。
        Godot.Callable.From(async () =>
        {
            await RelicCmd.Obtain(ModelDb.Relic<StarPowerLevel>().ToMutable(), base.Owner);
            await RelicCmd.Obtain(ModelDb.Relic<StarSkillQuality>().ToMutable(), base.Owner);
            await RelicCmd.Obtain(ModelDb.Relic<BasicArts>().ToMutable(), base.Owner);
        }).CallDeferred();
    }

}