using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;


namespace JiangXiaoMod.Code.Powers.StarMaps;
// --- 第四星圖：噬海之魂 ---

public sealed class SoulOfDevouringSeaPower : StarMapPowerModel
{
    // 效果：奪舍、操作他人身體
    public SoulOfDevouringSeaPower() : base() { }
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }
    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        
    }
}