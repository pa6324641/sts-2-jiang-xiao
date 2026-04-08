using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands; 
using JiangXiaoMod.Code.Powers;
using JiangXiaoMod.Code.Relics; // 確保引用遗物所在的命名空間
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // 用於 FirstOrDefault
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))] 
public class Dawn : CustomCardModel
{
    public const string CardId = "Dawn";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8m, ValueProp.Move),
        new DynamicVar("M", 1m) // 這裡的 1m 會被 UpdateStatsBasedOnRank 覆蓋
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<DawnPower>()
    ];

    public Dawn() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    /// <summary>
    /// STS2 核心邏輯：即時計算數值。這會影響卡牌描述的顯示。
    /// </summary>

    private int GetQualityRank()
    {
        if (Owner == null) return 1;
        var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        return relic?.SkillRank ?? 1;
    }

    public void UpdateStatsBasedOnRank()
    {
        int rank = GetQualityRank();
        // 2. 根據等級計算基礎抽牌數 (rank1-2: 1, rank3-4: 2, rank5-7: 3)
        decimal calculatedM = rank switch
        {
            <= 2 => 1m,
            <= 4 => 2m,
            _ => 3m
        };

        // 3. 如果卡牌已升級，額外 +1 (保留你原本 OnUpgrade 的邏輯)
        if (IsUpgraded)
        {
            calculatedM += 1m;
        }

        // 4. 更新動態變量 M 的基礎值
        DynamicVars["M"].BaseValue = calculatedM;
    }

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        if (combat == null || Owner?.Creature == null) return;

        // 💡 建議：在這裡調用一次 UpdateStatsBasedOnRank 確保打出時數值是最新的
        UpdateStatsBasedOnRank();

        // 1. 獲取施加目標（所有隊友）
        var targets = combat.Allies;

        // 2. 獲得格擋 
        // 💡 修正：直接傳入 DynamicVars.Block，不要用 .BaseValue
        foreach (var target in targets)
        {
            // 這裡傳入 DynamicVars.Block (它是 BlockVar 類型)，符合函數要求
            await CreatureCmd.GainBlock(target, DynamicVars.Block, cardPlay);
        }

        // 3. 獲取計算後的 M 值 (建議用 .Get("M") 增加穩定性)
        decimal drawAmount = DynamicVars["M"].BaseValue;
        
        // 4. 施加曙光能力
        await PowerCmd.Apply<DawnPower>(targets, drawAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 這裡只需要處理格擋的固定升級
        // M 的升級邏輯已移至 RecalculateValues 中統一處理
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}