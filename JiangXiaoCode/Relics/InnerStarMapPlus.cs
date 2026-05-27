using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;


namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class InnerStarMapPlus : AbstractInnerStarMap
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    // 用於升級時跨實例傳遞數據的靜態緩存
    public static int _transferPointsBuffer = -1;

    protected override string CardRewardAlternativeKey => "CONVERT_SKILL_POINTS_UPGRADED";
    protected override int SkillPointsGain => 2000;

    // 配置升級版的雙倍點數分配
    protected override int CombatVictoryBaseGain => 1250;
    protected override int CombatVictoryEliteGain => 2500;
    protected override int CombatVictoryBossGain => 5000;
    protected override int RoomEnteredGain => 1250;

    public override RelicModel? GetUpgradeReplacement() => null;

    /// <summary>
    /// 獨有邏輯：當獲得升級版遺物時，從緩存中安全恢復數據
    /// </summary>
    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (_transferPointsBuffer != -1)
        {
            this.JiangXiaoMod_SkillPoints = _transferPointsBuffer;
            _transferPointsBuffer = -1; // 恢復後立即清空緩存，保障存檔安全
        }
    }
}