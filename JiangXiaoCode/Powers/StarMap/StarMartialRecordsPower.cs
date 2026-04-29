using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JiangXiaoMod.Code.Powers.StarMaps;
// --- 第五星圖：星武紀 ---

public sealed class StarMartialRecordsPower : StarMapPowerModel
{
    // 效果：查看特性、提升/降低境界、星技轉換
    public StarMartialRecordsPower() : base() { }
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield break;
    }
    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        
    }
}