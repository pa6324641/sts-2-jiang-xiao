using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Creatures;
// [核心發現] 引用此命名空間以使用官方的中斷/拋棄式上下文
using MegaCrit.Sts2.Core.GameActions.Multiplayer; 

namespace JiangXiaoMod.Code.Powers;

public class TearOfSorrowPower : CustomPowerModel
{
    public const string PowerId = "JIANGXIAOMOD-TEAR_OF_SORROW_POWER";
    public new string Id => PowerId;

    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => PowerType.Debuff;

    public TearOfSorrowPower() : base() { }
    public TearOfSorrowPower(int amount) : base()
    {
        this.Amount = amount;
    }

protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 1. 安全檢查：如果 Owner 為空（例如在圖鑑預覽或初始化時）
            if (Owner == null)
            {
                // 提供一個預設顯示值（例如 1），避免 UI 顯示 0 
                // 或者回傳當前層數 Amount 作為佔位符
                yield return new DynamicVar("M", Amount > 0 ? Amount : 1m);
                yield break;
            }

            // 2. 戰鬥中真實計算
            // 取得 Owner 的最大生命值並計算 5% (無條件捨去，最小 1 點)
            decimal damagePerStack = Math.Max(Math.Floor(Owner.MaxHp * 0.05m), 1m);
            decimal totalDamage = damagePerStack * Amount;

            // 3. 回傳變數 "M"
            // 注意：請確保 Localization 中的描述是使用 {M} 而不是 {MVar}
            yield return new DynamicVar("M", totalDamage);
        }
    }

    /// <summary>
    /// 參照原版 PoisonPower 的實作邏輯
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        // 1. 陣營檢查：毒的邏輯是 side != base.Owner.Side 則 return
        if (side != base.Owner.Side) return;

        if (Owner == null || Amount <= 0) return;

        // 2. 計算傷害 (5% MaxHP * 層數)
        decimal damagePerStack = Math.Max(Math.Floor(Owner.MaxHp * 0.05m), 1m);
        decimal totalDamage = damagePerStack * Amount;

        // 3. 執行傷害
        // 參考 PoisonPower：使用 new ThrowingPlayerChoiceContext() 
        // 屬性使用 Unblockable (無視格擋) 與 Unpowered (不因力量/易傷等加成，符合流失生命定義)
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(), 
            base.Owner, 
            totalDamage, 
            ValueProp.Unblockable | ValueProp.Unpowered, 
            null, // 傷害來源：毒是傳 null，這裡也跟隨官方標準
            null
        );

        // 註：如果此能力需要像毒一樣每回合減少層數，請在此添加：
        // await PowerCmd.Decrement(this);
    }
}