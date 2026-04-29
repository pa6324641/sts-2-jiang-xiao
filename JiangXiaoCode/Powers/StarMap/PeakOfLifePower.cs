using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JiangXiaoMod.Code.Powers.StarMaps;
// --- 第八星圖：生命巔峰 ---

public sealed class PeakOfLifePower : StarMapPowerModel
{
    // 效果：形態與身體素質改變至巔峰
    public PeakOfLifePower() : base() { }
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }
    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        
    }
}