using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;


namespace JiangXiaoMod.Code.Powers.StarMaps;
// --- 第七星圖：聖十字架 ---

public sealed class HolyCrossPower : StarMapPowerModel
{
    // 效果：免疫星技攻擊
    public HolyCrossPower() : base() { }
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }
    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        
    }
}