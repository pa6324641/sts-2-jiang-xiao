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
using MegaCrit.Sts2.Core.GameActions.Multiplayer; // 必須引用，為了使用 RunManager

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

        public int GetQualityRank()
        {
            try {
                // [核心修正] 
                // 1. 在 CardModel 中，'Owner' 屬性就是發動這張卡的 Player。
                // 2. 如果 Owner 為 null (例如在圖鑑中)，則使用你提供的 DebugOnlyGetState() 獲取。
                var player = Owner ?? RunManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault();
                var relic = player?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
                return relic?.SkillRank ?? 1;
            }
            catch { return 1; }
        }

        public void UpdateStatsBasedOnRank()
        {
            int rank = GetQualityRank();
            decimal finalRegenValue = 3m + (rank - 1);
            DynamicVars[_mKey].BaseValue = finalRegenValue;
        }
        public override Task BeforeCombatStart()
        {
            UpdateStatsBasedOnRank();
            return base.BeforeCombatStart();
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            UpdateStatsBasedOnRank();
            int regenAmount = (int)DynamicVars[_mKey].BaseValue;

            // 這裡傳入 regenAmount，它會被保存在 ChengYinPower 的 Amount 屬性中
            await PowerCmd.Apply<ChengYinPower>(
                new[] { Owner.Creature }, 
                regenAmount, 
                Owner.Creature, 
                this
            );
        }
    }
}