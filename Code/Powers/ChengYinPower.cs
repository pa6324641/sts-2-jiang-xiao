using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs; // 為了使用 RunManager 抓取保底數值
using JiangXiaoMod.Code.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Powers
{
    public class ChengYinPower : CustomPowerModel
    {
        public const string PowerId = "ChengYinPower";

        public ChengYinPower() : base() { }

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.None;

        // 【修復顯示 0 的核心邏輯】
        public int GetCurrentRegenAmount()
        {
            // 1. 如果 Amount 已經有值(大於0)，代表卡片已經成功傳入數值，直接使用它。
            if (Amount > 0) return (int)Amount;

            // 2. [保底方案] 如果 Amount 還是 0 (通常發生在 UI 剛加載時)，
            // 則透過 RunManager 直接計算一次目前玩家應有的數值。
            var player = RunManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault();
            var relic = player?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            return 3 + ((relic?.SkillRank ?? 1) - 1);
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                // 使用函數獲取數值，確保渲染時不會因為 Amount 暫時為 0 而顯示錯誤
                yield return new DynamicVar("RegenAmt", (decimal)GetCurrentRegenAmount());
            }
        }

        public override async Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            await base.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource);

            // 當持有者受到肉體傷害時觸發
            if (target == Owner && result.UnblockedDamage > 0)
            {
                // 獲取目前的再生量
                int finalAmount = GetCurrentRegenAmount();

                await PowerCmd.Apply<RegenPower>(
                    target: Owner, 
                    amount: (decimal)finalAmount, 
                    applier: Owner, 
                    cardSource: null
                );
            }
        }
    }
}