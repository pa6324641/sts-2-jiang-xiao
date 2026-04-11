using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players; 
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.Runs;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords; // 必須引用以識別 PileType


namespace JiangXiaoMod.Code.Cards.Common
{

[Pool(typeof(JiangXiaoCardPool))]
public class CleanTears : CustomCardModel
	{
		// 構造函數：確保參數與父類匹配
		public CleanTears() : base(2, CardType.Skill, CardRarity.Common, TargetType.None)
		{

		}

		protected override IEnumerable<DynamicVar> CanonicalVars => [
			new DynamicVar("M", 1m)
		];

		public void UpdateStatsBasedOnRank()
		{
			if (Owner == null) 
			{
				DynamicVars["M"].BaseValue = 1m;
				return;
			}
			// Owner 通常是 PlayerModel
			var player = this.Owner;
			// 透過之前定義的工具類獲取 Rank
			int rank = JiangXiaoUtils.GetSkillRank(player);
			
			decimal mValue = rank switch
			{
				>= 6 => 5m,
				>= 4 => 3m,
				_ => 1m
			};

			DynamicVars["M"].BaseValue = mValue;
		}
	public override Task BeforeCombatStart()
	{
		UpdateStatsBasedOnRank();
		return base.BeforeCombatStart();
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		var combat = CombatState;
		if (combat == null) return;

		UpdateStatsBasedOnRank();
		int mLimit = (int)DynamicVars["M"].BaseValue;

		foreach (var playerEntity in combat.Players)
		{
			var combatState = playerEntity.PlayerCombatState;
			if (combatState == null) continue;

			// 定義一個內部方法來處理單一牌堆的移除邏輯
			async Task PurgeFromPile(CardPile pile)
			{
				var status = pile.Cards.Where(c => c.Type == CardType.Status).Take(mLimit).ToList();
				var curses = pile.Cards.Where(c => c.Type == CardType.Curse).Take(mLimit).ToList();
				var toPurge = status.Concat(curses).ToList();

				if (toPurge.Count > 0)
				{
					// [核心修正] 使用 Add 移至消耗堆，這會觸發隱藏牌堆飛出的 VFX 特效
					// 且 Add 會自動調用 RemoveFromCurrentPile，數據最安全
					await CardPileCmd.Add(toPurge, PileType.Exhaust, CardPilePosition.Bottom, this, false);
				}
			}

			// 分別對三個牌堆執行移除 (這樣每個牌堆都會各自嘗試移除 M 張)
			await PurgeFromPile(combatState.Hand);
			await PurgeFromPile(combatState.DrawPile);
			await PurgeFromPile(combatState.DiscardPile);
		}
	}
		protected override void OnUpgrade()
		{
			// 此牌主要隨品質提升，若需升級效果可在此添加
			EnergyCost.UpgradeBy(-1);
		}
	}
}
