// ... (保留原有的 using)
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.GameInfo.Objects;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class Ruinance() : CustomCardModel(
    1, 
    CardType.Power, 
    CardRarity.Basic, 
    TargetType.Self 
)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(IsUpgraded ? 6m : 3m, ValueProp.Move),
        new DynamicVar("M", 1m) // M 在這裡作為觸發標記或倍率，如果不需要可移除
    ];

    // 新增：提取計算化解值的邏輯，僅在打出時呼叫一次
    private int CalculateResistAmount(Player? player)
    {
        int rankLevel = JiangXiaoUtils.GetSkillRank(player);
        return 3 + (rankLevel - 1) * 3;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 計算當前星技等級對應的數值
        int finalResist = CalculateResistAmount(Owner); 

        // 將數值作為第二個參數傳入，這會直接賦值給能力的 Amount
        await PowerCmd.Apply<RuinancePower>(Owner.Creature, finalResist, Owner.Creature, this);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Passive),
        HoverTipFactory.Static(StaticHoverTip.Block),
        // 直接使用泛型，讓系統調用 ModelDb 中的單例，不要 new
        HoverTipFactory.FromPower<RuinancePower>()
       
    ];
    public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        await base.BeforeHandDrawLate(player, choiceContext, combatState);

        // 1. 檢查玩家與戰鬥狀態安全性
        // [STS2_Optimization] 確保 PlayerCombatState 存在以避免空值引用
        if (player == null || player.PlayerCombatState == null) return;

        // 2. 判斷卡牌位置
        // [STS2_Logic] 使用 PlayerCombatState.DrawPile 檢查卡牌是否在抽牌堆中 
        if (player.PlayerCombatState.DrawPile.Cards.Contains(this))
        {
            // 3. 定義打出目標
            // 若卡牌目標類型為自身 (Self)，則指向玩家 Creature；否則由 AutoPlay 邏輯處理 
            Creature? target = (this.TargetType == TargetType.Self) ? player.Creature : null;

            // 4. [STS2_API] 執行自動打出
            // 使用傳入的 choiceContext 替代手動創建的 Context，這在多人模式或複雜排程下更穩定 
            // AutoPlay 會處理 CardPile 的移出手續，不需額外標記 HasBeenRemovedFromState
            await CardCmd.AutoPlay(choiceContext, this, target);
        }
    }

      public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}