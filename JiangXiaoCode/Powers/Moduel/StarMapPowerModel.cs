using BaseLib.Abstracts;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace JiangXiaoMod.Code.Powers.StarMaps;

/// <summary>
/// 星圖能力基類：所有星圖效果都繼承自此
/// [STS2_Optimization] 透過繼承此類，可以輕鬆實作「移除舊星圖、切換新星圖」的邏輯
/// </summary>
public abstract class StarMapPowerModel : JiangXiaoPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    protected StarMapPowerModel() : base() { }
    protected abstract IEnumerable<DynamicVar> GetCustomVars();

    /// <summary>
    /// 安全地更新數值
    /// </summary>
    public void UpdateStatsBasedOnRank() 
    {
        // 1. 獲取當前運行狀態 (安全檢查的第一關)
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null) return; 

        // 2. 嘗試獲取玩家 (通常星圖都是給玩家的)
        var player = runState.Players.FirstOrDefault();
        
        // 3. 獲取星力等級 (使用先前定義的 Utils)
        // 注意：這裡確保了即使在沒有 Owner 的情況下，只要在運行中就能拿到等級
        int powerLevel = JiangXiaoUtils.GetPowerLevel(player);

        // 4. 執行子類的邏輯
        ApplyRankLogic(player, powerLevel);
    }

    protected abstract void ApplyRankLogic(Player? player, int powerLevel);

    // [STS2_API] 為了確保描述中的變量能隨等級更新，建議在這裡也掛鉤
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 在渲染描述前先觸發一次邏輯更新，確保變量是最新的
            UpdateStatsBasedOnRank();
            
            // 這裡可以回傳子類定義的變量
            return GetCustomVars();
        }
    }

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }
}