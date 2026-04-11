using BaseLib.Abstracts;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Powers;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public class TearOfSorrow : JiangXiaoCardModel
{
    public const string CardId = "TearOfSorrow";

    public TearOfSorrow() : base(4, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        // 這裡建構子應傳入 CardId，父類會處理資源加載
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("M", 1m) 
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromPower<TearOfSorrowPower>()
    ];

    /// <summary>
    /// 根據星技品質 rank 計算 M 值 (層數)
    /// rank 1-3: 1層
    /// rank 4-5: 2層
    /// rank 6-7: 3層
    /// </summary>
    public void UpdateStatsBasedOnRank()
    {
        var player = Owner ?? RunManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault();
        int rank = JiangXiaoUtils.GetSkillRank(player);

        decimal mAmount;
        if (rank <= 3)
            mAmount = 1m;
        else if (rank <= 5)
            mAmount = 2m;
        else
            mAmount = 3m;

        // 若卡片已升級，額外增加 1 層 (可根據需求調整升級邏輯)
        if (IsUpgraded)
        {
            mAmount += 1m;
        }

        DynamicVars["M"].BaseValue = mAmount;
    }

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState;
        // 驗證戰鬥狀態與持有者
        if (combat == null || Owner?.Creature == null) return;

        var player = this.Owner;
        
        // 1. 播放前更新數值，確保抓取的 M 值準確
        UpdateStatsBasedOnRank();
        int rank = JiangXiaoUtils.GetSkillRank(player);
        decimal mAmount = DynamicVars["M"].BaseValue;

        // 2. 確定目標群體
        IEnumerable<Creature> targets;
        if (rank <= 3)
        {
            // Rank 1-3: 影響全場單位 (盟友 + 敵人)
            targets = combat.Allies.Concat(combat.Enemies).ToList();
        }
        else
        {
            // Rank 4 以上: 僅影響敵人
            targets = combat.Enemies;
        }

        // 3. 執行效果
        foreach (var target in targets)
        {
            // 施加傷之淚能力 (TearOfSorrowPower)
            // 傳入目標實體、計算後的層數、來源實體、來源卡片
            await PowerCmd.Apply<TearOfSorrowPower>(target, mAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升級時即時更新一次數值預覽
        UpdateStatsBasedOnRank();
    }
}