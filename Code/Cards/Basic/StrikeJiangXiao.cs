using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Relics; // 確保引用品質遺物所在的命名空間
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class StrikeJiangXiao : CustomCardModel
{
    public StrikeJiangXiao() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 初始化基礎傷害數值為 6
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star)
    ];

    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    /// <summary>
    /// 從玩家遺物中獲取星技品質等級。
    /// </summary>
    private int GetQualityRank()
    {
        if (Owner == null) return 1;
        // 尋找名為 StarSkillQuality 的遺物並讀取其 SkillRank 屬性
        var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        return relic?.SkillRank ?? 1;
    }

    /// <summary>
    /// 更新攻擊數值的核心邏輯：基礎值 + (等級 - 1) * 3
    /// </summary>
    public void UpdateStatsBasedOnRank()
    {
        int rank = GetQualityRank();
        // STS2 基礎傷害 6，升級後為 9 (6+3)
        decimal baseDamage = IsUpgraded ? 9m : 6m;

        // 修改 Damage 的 BaseValue 會直接影響到 UI 顯示與最終傷害計算
        DynamicVars.Damage.BaseValue = baseDamage + (rank - 1) * 3m;
    }

    // 戰鬥開始前刷新數值，確保手牌顯示正確
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 確保打出時的數值是根據當前等級計算的最精確值
        UpdateStatsBasedOnRank();

        // 使用 STS2 的 DamageCmd 進行攻擊
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 執行標準升級：傷害增加 3
        DynamicVars.Damage.UpgradeValueBy(3m);
        EnergyCost.UpgradeBy(-1);
        // 升級後立即重新計算品質等級的加成
        UpdateStatsBasedOnRank();
    }
}