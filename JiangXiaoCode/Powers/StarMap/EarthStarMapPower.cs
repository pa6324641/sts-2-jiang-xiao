using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JiangXiaoMod.Code.Powers.StarMaps;
// --- 第九星圖：大地星圖 ---

public sealed class EarthStarMapPower : StarMapPowerModel
{
    // 效果：連接大地、分擔攻擊、我即大地
    public EarthStarMapPower() : base() { }
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }

    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        
    }
}