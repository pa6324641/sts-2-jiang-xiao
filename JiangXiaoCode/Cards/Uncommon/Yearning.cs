using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands;
using JiangXiaoMod.Code.Relics;
using JiangXiaoMod.Code.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.Rare;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))] 
public class Yearning : JiangXiaoCardModel
{
    private const string VarM = "M";

    public Yearning() : base(
        3, 
        type: CardType.Power, 
        rarity: CardRarity.Uncommon, 
        target: TargetType.Self
    )
    {
        JJCustomVar(VarM, 10m);
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJPowerTip<YearningHaloPower>();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Dawn>()
    ];

    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 公式：5 + (等級 * 5) -> 1:10, 2:15, 3:20
        DynamicVars[VarM].BaseValue = 5m + (skillRank * 5m);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-2);
        UpdateStatsBasedOnRank();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null || Owner?.Creature == null) return;

        UpdateStatsBasedOnRank();
        int currentM = (int)DynamicVars[VarM].BaseValue;

        // 尋找擁有「DawnPower」能力的所有盟友
        var alliesWithDawn = combat.Allies
            .Where(a => a.Powers.Any(p => p is DawnPower))
            .ToList();

        if (alliesWithDawn.Any())
        {
            // 若有目標符合，僅對他們施加
            await PowerCmd.Apply<YearningHaloPower>(alliesWithDawn, currentM, Owner.Creature, this);
        }
        else
        {
            // [修正] 解決 CS1061 錯誤：
            // 使用 LINQ 的 Concat 將 Allies 與 Enemies 合併，以獲取全場單位
            var allUnits = combat.Allies.Concat(combat.Enemies).ToList();
            await PowerCmd.Apply<YearningHaloPower>(allUnits, currentM, Owner.Creature, this);
        }
    }
}