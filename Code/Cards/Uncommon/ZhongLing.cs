using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using JiangXiaoMod.Code.Relics;
using JiangXiaoMod.Code.Powers; 
using BaseLib.Abstracts;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures; // 確保引用了生物實體命名空間
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Cards.Common;
using JiangXiaoMod.Code.Cards.Uncommon;
using JiangXiaoMod.Code.Cards.Rare;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public class ZhongLing : CustomCardModel
{
    private const string VarHeal = "Heal";
    private const string VarHits = "Magic";

    public ZhongLing() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar(VarHeal, IsUpgraded ? 9m : 6m);
            yield return new DynamicVar(VarHits, IsUpgraded ? 5m : 4m);
        }
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromCard<ChengYin>()
    ];

    protected override void OnUpgrade()
    {
        DynamicVars[VarHits].UpgradeValueBy(3m);
        DynamicVars[VarHeal].UpgradeValueBy(1m);
        EnergyCost.UpgradeBy(-1);
        GetBattleHeal();
        GetBattleHits();
    }

    private decimal GetBattleHeal()
    {
        decimal val = IsUpgraded ? 9m : 6m;
        var player = RunManager.Instance?.DebugOnlyGetState()?.Players?.FirstOrDefault();
        if (player != null)
        {
            var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            val += ((relic?.SkillRank ?? 1) - 1) * 3m;
        }
        return val;
    }

    private int GetBattleHits()
    {
        decimal val = IsUpgraded ? 5m : 4m;
        var player = RunManager.Instance?.DebugOnlyGetState()?.Players?.FirstOrDefault();
        if (player != null)
        {
            var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            val += ((relic?.SkillRank ?? 1) - 1) * 1m;
        }
        return (int)val;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (combat == null || runState == null) return;

        decimal finalHeal = GetBattleHeal();
        int finalHits = GetBattleHits();

        // 1. 先篩選帶有《承印》能力的盟友
        var targets = combat.Allies
            .Where(creature => creature.Powers.Any(p => p is ChengYinPower))
            .ToList();

        // 2. 邏輯判斷：如果沒有承印目標，則將目標範圍擴大到全體（盟友 + 敵人）
        if (targets.Count == 0)
        {
            // 使用 Concat 合併列表以確保包含所有人，這是最保險的 STS2 寫法
            targets = combat.Allies.Concat<Creature>(combat.Enemies).ToList();
        }

        if (targets.Count == 0) return;

        var rng = runState.Rng.CombatTargets;

        for (int i = 0; i < finalHits; i++)
        {
            // 每次循環隨機挑選一個目標
            int randomIndex = rng.NextInt(targets.Count);
            var randomTarget = targets[randomIndex];

            // --- 修正重點 ---
            // 直接 await Task，不要調用 .Execute()
            await CreatureCmd.Heal(randomTarget, finalHeal, true);
            
            await Task.Delay(60);
        }
    }
}