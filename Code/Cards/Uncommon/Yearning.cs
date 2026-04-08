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
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Cards.Common;
using JiangXiaoMod.Code.Cards.Rare;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))] 
public class Yearning : CustomCardModel
{
    // 定義動態變量名稱
    private const string VarM = "M";

    public Yearning() : base(
        baseCost: 3, 
        type: CardType.Power, 
        rarity: CardRarity.Uncommon, 
        target: TargetType.Self
    )
    {
    }

    // [動態文本核心 1]: 定義 CanonicalVars 讓 JSON 能讀取 {M}
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(VarM, 10m) // 預設值 10
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromCard<Dawn>(),
        HoverTipFactory.FromPower<YearningHaloPower>()
    ];

    // [動態文本核心 2]: 在戰鬥開始或需要刷新時更新數值
    public void UpdateStatsBasedOnRank()
    {
        int rank = GetQualityRank();
        // 計算公式：5 + (等級 * 5)
        // 等級 1 -> 10, 等級 2 -> 15, 等級 3 -> 20
        DynamicVars[VarM].BaseValue = 5m + (rank * 5m);
    }

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }
        protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-2);
        UpdateStatsBasedOnRank();
    }

    // 獲取星技品質等級
    private int GetQualityRank()
    {
        // 如果是圖鑑模式或 Owner 為空，返回 1 級以供正確顯示
        if (IsCanonical || Owner?.Creature == null) return 1;
        
        var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        return relic != null ? (int)relic.GetRank() : 1;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null || Owner?.Creature == null) return;

        // 確保打出時數值是最新的
        UpdateStatsBasedOnRank();
        int currentRank = GetQualityRank();

        var alliesWithDawn = combat.Allies.Where(a => a.Powers.Any(p => p is DawnPower)).ToList();

        if (alliesWithDawn.Any())
        {
            // 將等級 currentRank 作為 Amount 傳入 Power
            await PowerCmd.Apply<YearningHaloPower>(alliesWithDawn, currentRank, Owner.Creature, this);
        }
        else
        {
            var allUnits = combat.Allies.Concat(combat.Enemies).ToList();
            await PowerCmd.Apply<YearningHaloPower>(allUnits, currentRank, Owner.Creature, this);
        }
    }
}