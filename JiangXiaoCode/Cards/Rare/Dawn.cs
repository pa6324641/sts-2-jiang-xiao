using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using JiangXiaoMod.Code.Powers;
using JiangXiaoMod.Code.Relics;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character; // 確保引用了 JiangXiaoUtils 所在的命名空間

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
// [STS2_Optimization] 1. 使用 Primary Constructor 風格，並將 TargetType 改為 AllAllies
// 這樣 UI 會正確顯示「全體隊友」選取效果，不再是「多此一舉」的手動選取。
public sealed class Dawn() : CustomCardModel(0, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
{
    private const string VarM = "M";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8m, ValueProp.Move), // 這裡建議確認 ValueProp 是否需為 Move，一般格擋用 None
        new DynamicVar(VarM, 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<DawnPower>()
    ];

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    private void UpdateStatsBasedOnRank()
    {
        // [冗餘消除] 3. 直接使用你寫好的工具類，維護更方便
        int rank = JiangXiaoUtils.GetSkillRank(Owner);
        
        decimal calculatedM = rank switch
        {
            <= 2 => 1m,
            <= 4 => 2m,
            _ => 3m
        };

        if (IsUpgraded)
        {
            calculatedM += 1m;
        }

        DynamicVars[VarM].BaseValue = calculatedM;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null) return;

        // 確保打出時數值是最新的（特別是某些即時獲得的遺物影響）
        UpdateStatsBasedOnRank();

        // 這裡的 Allies 包含玩家自己與所有隊友/召喚物
        var targets = combat.Allies;
        decimal drawAmount = DynamicVars[VarM].BaseValue;

        // 4. 給予所有人格擋
        // 在 STS2 中，CreatureCmd.GainBlock 目前仍建議對每個實體分別調用
        foreach (var target in targets)
        {
            await CreatureCmd.GainBlock(target, DynamicVars.Block, cardPlay);
        }

        // 5. 施加曙光能力 (PowerCmd.Apply 本身支援傳入 IEnumerable<Creature>)
        // 這裡傳入的 drawAmount 會成為 DawnPower 的 Amount 屬性
        await PowerCmd.Apply<DawnPower>(targets, drawAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 只處理基礎數值升級，動態變量 M 的升級邏輯已統一在 UpdateStatsBasedOnRank 中
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}