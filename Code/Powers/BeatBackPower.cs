using System.Threading.Tasks;
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JiangXiaoMod.Code.Powers;

/// <summary>
/// 擊退能力：在回合結束時，恢復對應數量的力量。
/// </summary>
public sealed class BeatBackPower : CustomPowerModel
{
    // 定義變數名稱，用於在地化文本顯示
    public const string VarAmount = "Amount";

    public override PowerType Type => PowerType.Debuff;

    // STS2 中，若要顯示層數通常使用 Normal。None 有時會隱藏數字。
    public override PowerStackType StackType => PowerStackType.Counter;

    public BeatBackPower()
    {
    }

    /// <summary>
    /// 定義要在在地化文本中顯示的動態變數
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 將 Amount 映射到在地化標籤中
            yield return new DynamicVar(VarAmount, Amount);
        }
    }

    /// <summary>
    /// 當回合結束時觸發。
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 只有在能力持有者的回合結束時才觸發 (例如怪物回合結束)
        if (side == Owner.Side)
        {
            if (Amount != 0)
            {
                // 恢復力量：STS2 建議直接 await PowerCmd.Apply
                await PowerCmd.Apply<StrengthPower>([Owner], Amount, Owner, null);
            }

            // 移除此能力
            await PowerCmd.Remove(this);
        }
    }
}