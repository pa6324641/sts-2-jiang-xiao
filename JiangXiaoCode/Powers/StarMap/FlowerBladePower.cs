using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;


namespace JiangXiaoMod.Code.Powers.StarMaps;
// --- 第二星圖：花里胡哨之刃 (花刃) ---

public sealed class FlowerBladePower : StarMapPowerModel
{
    // 效果：鋒利、撕裂
    public FlowerBladePower() : base() { }
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }

    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        
    }
}