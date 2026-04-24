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
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Powers;

/// <summary>
/// 悲傷之淚：每回合開始時造成目標最大生命值 5% 的傷害。
/// 已優化預覽模式下的數值顯示，並強化戰鬥中的安全判定。
/// </summary>
public class TearOfSorrowPower : JiangXiaoPowerModel
{
	// 確保此 ID 與 powers.json 中的鍵值對應
	public const string PowerId = "JIANGXIAOMOD-TEAR_OF_SORROW_POWER";

	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerType Type => PowerType.Debuff;

	public TearOfSorrowPower() : base() { }

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			// [STS2_Optimization] 處理 UI 渲染邏輯
			// 當能力在怪物實體上時，Owner 不為 null
			if (Owner != null)
			{
				// 計算當前精確的傷害值
				decimal dmg = Math.Max(Math.Floor(Owner.MaxHp * 0.05m), 1m);
				yield return new DynamicVar("M", dmg);
			}
			else
			{
				// 【重要】：在圖鑑或卡牌預覽中，Owner 會是 null。
				// 這裡回傳 0 是安全做法。
				// 如果你的描述是「造成 {M} 點傷害」，預覽會顯示 0。
				// 建議：在 JSON 描述寫成「造成 5% 最大生命值的傷害 (當前：{M})」。
				yield return new DynamicVar("M", 0m); 
			}
		}
	}

	public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		// 1. 安全檢查：必須確保擁有者存在，且當前開始的是「擁有者陣營」的回合
		// 既然是 Debuff，Owner 通常是怪物，所以當怪物陣營回合開始時觸發
		if (Owner == null || side != Owner.Side) 
		{
			await base.AfterSideTurnStart(side, combatState);
			return;
		}

		// 2. 層數檢查
		if (Amount <= 0) return;

		// 3. 重新計算傷害 (考量到最大 HP 可能在戰鬥中變動，所以不直接從 CanonicalVars 抓值)
		decimal damagePerStack = Math.Max(Math.Floor(Owner.MaxHp * 0.05m), 1m);
		decimal totalDamage = damagePerStack * Amount;

		// 4. 執行效果
		Flash(); // 視覺回饋：能力圖示閃爍

		// [STS2_API] 使用 CreatureCmd.Damage 執行忽視格擋的生命值削減
		await CreatureCmd.Damage(
			new ThrowingPlayerChoiceContext(), // STS2 非玩家直接操作的行為上下文
			Owner, 
			totalDamage, 
			ValueProp.Unblockable | ValueProp.Unpowered, // 無視格擋、無視各種增傷/減傷修飾
			null, // 無直接傷害來源（如力量）
			null  // 無特定卡牌來源
		);

		await base.AfterSideTurnStart(side, combatState);
	}
}
