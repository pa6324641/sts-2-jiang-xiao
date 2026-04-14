using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Combat;
using JiangXiaoMod.Code.Powers;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Rare
{
    [Pool(typeof(JiangXiaoCardPool))]
    public sealed class ChengYin() : CustomCardModel(1, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
    {
        private const string _mKey = "M";
        
        protected override IEnumerable<DynamicVar> CanonicalVars => [ new(_mKey, 2m) ];
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips => [
            HoverTipFactory.FromPower<RegenPower>(),
            HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
            HoverTipFactory.FromPower<ChengYinPower>()
        ];

        public override Task BeforeCombatStart()
        {
            UpdateStatsBasedOnRank();
            return base.BeforeCombatStart();
        }

        public int GetQualityRank()
        {
            try {
                // [核心修正] 優先使用 Owner。若在圖鑑中(Owner為null)，則從 RunManager 獲取狀態。
                var player = Owner ?? RunManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault();
                var relic = player?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
                return relic?.SkillRank ?? 1;
            }
            catch { return 1; }
        }

        public void UpdateStatsBasedOnRank()
        {
            int rank = GetQualityRank();
            // 基礎 3 層，每級品質 +1
            decimal finalRegenValue = 3m + (rank - 1);
            DynamicVars[_mKey].BaseValue = finalRegenValue;
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 1. 強制更新當前數值
            UpdateStatsBasedOnRank();
            decimal regenAmount = DynamicVars[_mKey].BaseValue;

            // 2. [核心修正] 獲取戰鬥狀態中的所有盟友
            // 在 STS2 中，CombatState.Allies 包含了所有友方單位（包含玩家自己與寵物/召喚物）
            var combat = this.CombatState;
            if (combat != null)
            {
                // [STS2_API] 使用 PowerCmd.Apply 批量對所有盟友施加能力
                // 傳入 combat.Allies 作為目標集合
                await PowerCmd.Apply<ChengYinPower>(
                    combat.Allies, 
                    regenAmount, 
                    Owner.Creature, 
                    this
                );
            }
        }
    }
}