using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Relics;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class Blessing : CustomCardModel
{
    public const string CardId = "JIANGXIAOMOD-BLESSING";
    private const string VarHeal = "HealAmount";

    public Blessing() : base(2, CardType.Skill, CardRarity.Basic, TargetType.AnyPlayer)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(VarHeal, 6m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star)
    ];

    // 穩健獲取星級品質
    private int GetQualityRank()
    {
        if (IsCanonical || Owner == null) return 1;
        try 
        {
            var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            // 呼叫遺物中的 SkillRank
            return relic != null ? relic.SkillRank : 1;
        }
        catch { return 1; }
    }

    // 更新 BaseValue 的核心邏輯
    public void UpdateStatsBasedOnRank()
    {
        int rank = GetQualityRank();
        decimal baseHeal = IsUpgraded ? 12m : 6m; 

        // 基礎值直接加上加成
        DynamicVars[VarHeal].BaseValue = baseHeal + (rank - 1) * 6m;
    }

    // 戰鬥開始前刷新
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    protected override void OnUpgrade()
    {
        DynamicVars[VarHeal].UpgradeValueBy(3m);
        EnergyCost.UpgradeBy(-1);
        UpdateStatsBasedOnRank();
    }

    // 🌟 移除原本會報錯的 GetDescriptionForPile 覆寫

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 確保打出時數值絕對正確
        UpdateStatsBasedOnRank();

        // 直接取用修改後的基礎值
        decimal finalHeal = DynamicVars[VarHeal].BaseValue;

        var target = cardPlay.Target ?? Owner?.Creature;

        if (target != null)
        {
            // 執行治癒指令
            await CreatureCmd.Heal(target, finalHeal, true);
        }
    }
}