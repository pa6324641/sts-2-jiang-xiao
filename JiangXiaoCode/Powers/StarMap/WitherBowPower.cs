using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JiangXiaoMod.Code.Powers.StarMaps;
// --- 第三星圖：凋零弓 ---

public sealed class WitherBowPower : StarMapPowerModel
{
    // 效果：凋零
    public WitherBowPower() : base() { }
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }
    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        
    }
}