using System.Threading.Tasks;
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Powers;

/// <summary>
/// 擊退能力：在回合結束時，恢復對應數量的力量。
/// 邏輯參考：類似於 STS1 的「束縛 (Shackled)」效果。
/// </summary>
public sealed class BeatBackPower : JiangXiaoPowerModel
{
    // 定義常量以便在地化文本中通過 [!Amount!] 引用
    public const string VarAmount = "var";

    public override PowerType Type => PowerType.Debuff;

    // 優化：改為 Normal 以符合 STS2 標準能力數值顯示規範
    public override PowerStackType StackType => PowerStackType.Counter;

    public BeatBackPower()
    {
        // 構造函數邏輯
    }

    /// <summary>
    /// 定義動態變數映射，將內部的 Amount 傳遞給在地化系統
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // [STS2_Logic] 確保 Amount 的數值能即時更新到 UI 描述中
            yield return new DynamicVar(VarAmount, Amount);
        }
    }

    /// <summary>
    /// 當回合結束時觸發邏輯。
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 只有在能力持有者的回合結束時才執行
        if (side == Owner.Side && Owner.IsAlive)
        {
            if (Amount != 0)
            {
                // [STS2_API] 使用 PowerCmd.Apply 進行力量恢復
                // 這裡假設 Amount 為正數，代表恢復的力量值
                await PowerCmd.Apply<StrengthPower>([Owner], Amount, Owner, null);
            }

            // 效果執行完畢後移除自身
            await PowerCmd.Remove(this);
        }
    }
}