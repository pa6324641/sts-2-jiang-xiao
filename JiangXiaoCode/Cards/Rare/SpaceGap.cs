// ... 保持前面的 using 不變 ...

using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class SpaceGap : JiangXiaoCardModel
{
    public const string CardId = "SpaceGap";

    public SpaceGap() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("M", 1m) // 初始值設定為 1
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromPower<IntangiblePower>()
    ];

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        // [修正] 如果你希望升級後是 2 層，這裡應該加 1 (1 + 1 = 2)
        // 如果你希望升級後是 3 層，才用 UpgradeValueBy(2)
        DynamicVars["M"].UpgradeValueBy(1); 
    }

    /// <summary>
    /// 根據星級品質遺物調整卡牌數值
    /// </summary>
    public void UpdateStatsBasedOnRank()
    {
        var player = Owner ?? RunManager.Instance?.DebugOnlyGetState()?.Players?.FirstOrDefault();
        var relic = player?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        int currentRank = relic?.SkillRank ?? 1;

        // --- 修正邏輯開始 ---
        
        // 1. 先確定「升級基礎值」：未升級為 1，已升級為 2
        decimal baseAmount = IsUpgraded ? 2m : 1m;

        // 2. 根據星級計算「額外加成」
        // 假設你的設計是：5星以上時，在基礎上額外 +1 層
        if (currentRank >= 5)
        {
            DynamicVars["M"].BaseValue = baseAmount + 1m; // 升級後會變成 3 層，未升級變成 2 層
        }
        else
        {
            DynamicVars["M"].BaseValue = baseAmount; // 尊重升級狀態，1-4星時升級卡保持 2 層
        }
        
        // --- 修正邏輯結束 ---
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        var player = Owner;
        if (combat == null || player?.Creature == null) return;

        // 確保數值最新
        UpdateStatsBasedOnRank();

        // 獲取最終計算後的數值
        decimal intangibleAmount = DynamicVars["M"].BaseValue;
        
        // 施加無實體
        await PowerCmd.Apply<IntangiblePower>(player.Creature, intangibleAmount, player.Creature, this);

        // 星級 5-7 特殊效果：隊友獲得 1 層
        var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        if (relic?.SkillRank >= 5)
        {
            var allies = combat.Allies.Where(a => a != player.Creature);
            foreach (var ally in allies)
            {
                await PowerCmd.Apply<IntangiblePower>(ally, 1m, player.Creature, this);
            }
        }
    }

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }
}