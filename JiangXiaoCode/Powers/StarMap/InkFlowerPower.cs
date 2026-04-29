using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;


namespace JiangXiaoMod.Code.Powers.StarMaps;
// --- 第六星圖：油墨花 ---

public sealed class InkFlowerPower : StarMapPowerModel
{
    // 效果：召喚油墨花打擊或控制
    public InkFlowerPower() : base() { }
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }
    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        
    }
}