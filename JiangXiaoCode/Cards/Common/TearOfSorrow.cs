using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Players;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SmartFormat.Extensions.PersistentVariables;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public class TearOfSorrow : JiangXiaoCardModel
{
    // 建議對齊 Localization 的 ID 命名規範
    public const string CardId = "JIANGXIAOMOD-TEAR_OF_SORROW";
    // public const string Var = "M";

    public TearOfSorrow() : base(4, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJPowerTip<TearOfSorrowPower>();
        JJCustomVar("M", 1m);
    }

    /// <summary>
    /// 根據星技品質動態調整 M 值 (層數)
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 使用 switch 表達式讓邏輯更清晰
        decimal mAmount = skillRank switch
        {
            <= 3 => 1m,
            <= 5 => 2m,
            _    => 3m
        };

        // 處理升級加成：屬性應為 Upgraded
        if (IsUpgraded)
        {
            mAmount += 1m;
        }

        DynamicVars["M"].BaseValue = mAmount;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature == null) return;

        // 獲取等級以決定目標範圍
        int rank = JiangXiaoUtils.GetSkillRank(Owner);
        decimal mAmount = DynamicVars["M"].BaseValue;

        // 決定受影響目標
        // Rank 1-3: 影響全場 (盟友 + 敵人)；Rank 4+: 僅影響敵人
        IEnumerable<Creature> targets = (rank <= 3) 
            ? [.. CombatState.Allies, .. CombatState.Enemies] 
            : CombatState.Enemies;

        foreach (var target in targets)
        {
            // 施加能力，傳入層數與來源
            await PowerCmd.Apply<TearOfSorrowPower>(target, mAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 建議：高費卡牌升級通常會減費，增加實用性
        EnergyCost.UpgradeBy(-1); 
        
        // 手動觸發一次數值更新，讓 UI 立即刷新預覽
        UpdateStatsBasedOnRank();
    }
}