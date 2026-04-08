using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models; // 引入 STS2 模型基類
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Relics; // 確保引用了遺物所在的命名空間
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class DefendJiangXiao : CustomCardModel
{
    public DefendJiangXiao() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    // 初始化基礎格擋數值為 5
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star)
    ];

    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    /// 獲取當前星技品質等級。
    /// 這裡透過 Owner (Player) 的遺物欄位尋找 StarSkillQuality。
    /// </summary>
    private int GetQualityRank()
    {
        if (Owner == null) return 1;
        var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        return relic?.SkillRank ?? 1;
    }

    /// <summary>
    /// 更新格擋數值的核心邏輯：基礎值 + (等級 - 1) * 3
    /// </summary>
    public void UpdateStatsBasedOnRank()
    {
        int rank = GetQualityRank();
        // STS2 升級邏輯：基礎 5，升級後為 8 (5+3)
        decimal baseBlock = IsUpgraded ? 8m : 5m; 

        // 動態更新 Block 的 BaseValue，這會同步觸發 UI 上的文字變動
        // STS2 的 LocString 會捕捉 BaseValue 的變化並渲染
        DynamicVars.Block.BaseValue = baseBlock + (rank - 1) * 3m;
    }

    // 在戰鬥開始前確保數值已根據等級刷新
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出時再次檢查數值，確保在戰鬥中若等級發生變動（如某些機制）能即時反映
        UpdateStatsBasedOnRank();

        // 使用 CreatureCmd 執行格擋指令，直接傳入更新後的 DynamicVars.Block
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        // 這是 STS2 的標準升級方式，增加 3 點基礎格擋
        DynamicVars.Block.UpgradeValueBy(3m);
        EnergyCost.UpgradeBy(-1);
        // 升級後立即重新計算品質加成
        UpdateStatsBasedOnRank();
    }
}