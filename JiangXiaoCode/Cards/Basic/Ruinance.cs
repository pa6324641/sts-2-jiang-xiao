using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands; 
using MegaCrit.Sts2.Core.ValueProps;
using JiangXiaoMod.Code.Powers;
using JiangXiaoMod.Code.Character;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Relics;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
// 使用與 DefendWatcher 相同的簡潔建構子格式
public sealed class Ruinance() : CustomCardModel(
    1,                    // 費用
    CardType.Power,       // 改為 Power，因為沒有攻擊動作
    CardRarity.Basic,     // 稀有度
    TargetType.Self       // 目標改為 Self，因為只對自己加格擋和能力
)
{

    // 這裡定義卡牌上顯示的數值
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(IsUpgraded ? 6m : 3m, ValueProp.Move),
        new DynamicVar("M", IsUpgraded ? 1m : 1m) ,

    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Passive),
        HoverTipFactory.FromPower<RuinancePower>(),
        HoverTipFactory.Static(StaticHoverTip.Block)
        
    ];

    // 卡牌圖片路徑 (參考 Watcher 範例)
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 獲得格擋 (直接傳入 DynamicVars.Block 物件)
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 賦予忍耐能力 (傳入變量 M 的數值)
        // 注意：確保 RuinancePower.cs 裡的建構子 public RuinancePower(int amount) 存在
        await PowerCmd.Apply<RuinancePower>(
            Owner.Creature, 
            (int)DynamicVars["M"].IntValue, 
            Owner.Creature, 
            this
        );
    }

    protected override void OnUpgrade()
    {
        // 升級時數值加強
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    public override async Task BeforeCombatStartLate()
    {
        await base.BeforeCombatStartLate();

        var player = this.Owner;
        var combat = this.CombatState;

        if (player == null || combat == null || player.PlayerCombatState == null) return;

        var drawPile = player.PlayerCombatState.DrawPile;

        // 檢查卡牌是否在抽牌堆
        if (drawPile.Cards.Contains(this))
        {
            // 能力牌通常不需要目標，所以根據 TargetType 判斷是否需要傳入敵人
            Creature? target = (this.TargetType == TargetType.None) 
                            ? null 
                            : combat.Enemies.FirstOrDefault(e => e.IsAlive);

            // [修正 CS8625] 使用 null! 繞過編譯器檢查
            PlayerChoiceContext context = new GameActionPlayerChoiceContext(null!);

            // STS2 方式：從抽牌堆自動丟出並打出
            await CardCmd.AutoPlay(context, this, target);

            // 核心邏輯：若是能力牌，確保它打出後不會被移出戰鬥狀態 (防止圖標消失)
            if (this.Type == CardType.Power)
            {
                this.HasBeenRemovedFromState = false;
            }
            
            /* STS1 註解：
            在 STS1 中我們會用 AbstractDungeon.actionManager.addToBottom(new NewQueueCardAction(...));
            在 STS2 中，這行 await CardCmd.AutoPlay 會處理所有事情，包含從抽牌堆移除的動畫。
            */
        }
    }


}