using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps; 
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Runs;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Reflection;

namespace JiangXiaoMod.Code.Powers;

public sealed class RuinancePower : JiangXiaoPowerModel
{
	// 確保這裡的字串與 .json 中的 {Amount} 完全一致
	private const string VarAmount = "M";

	public override PowerType Type => PowerType.None;
	public override PowerStackType StackType => PowerStackType.Counter; 

	// 無參數構造函數（系統註冊用）
	public RuinancePower() : base() { }
	
	// 傷害減免邏輯
	public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		// 只有當擁有者是被攻擊者，且傷害大於 0 時才生效
		if (Owner != null && target == Owner && amount > 0)
		{
			// [注意] STS2 中此處 return 的數值會直接加到傷害總值上，所以負數代表減傷
			// 如果 this.Amount 是正數（層數），則減去該層數
			return -(decimal)this.Amount;
		}
		return 0m;
	}

	public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner != null && target == Owner && amount > 0)
		{
			Flash(); 
		}
		return Task.CompletedTask;
	}

	// [STS2 重要] 處理 UI 動態數值顯示
	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			decimal displayValue;

			// 1. 如果在戰鬥中，且對象已存在（實例化階段）
			if (Owner != null)
			{
				displayValue = (decimal)this.Amount;
			}
			// 2. 如果是預覽狀態（例如商店、卡牌說明、圖表單例）
			else
			{
				displayValue = CalculatePreviewResist();
			}

			// 返回動態變量，確保 key 名稱與 JSON 中的 {Amount} 匹配
			yield return new DynamicVar(VarAmount, displayValue);
		}
	}

	private decimal CalculatePreviewResist()
	{
		// [修正] 根據你之前的腳本範例，使用 DebugOnlyGetState() 來獲取狀態
		// 這是因為某些版本的 sts2.dll 將 State 設為 internal 或有不同的命名
		var runState = RunManager.Instance?.DebugOnlyGetState();
		var player = runState?.Players?.FirstOrDefault();
		
		if (player != null)
		{
			// 調用你的工具類獲取星技等級
			int rank = JiangXiaoUtils.GetSkillRank(player);
			
			// 根據等級計算預覽值 (目前設定為固定 3，你可以改為公式)
			// 例如：return 3 + (rank - 1) * 2;
			return 3m; 
		}

		// 萬一抓不到玩家（例如在主選單預覽時），回傳基礎值
		return 3m; 
	}
}
