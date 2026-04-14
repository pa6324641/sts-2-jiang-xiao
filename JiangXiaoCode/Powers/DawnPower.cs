using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands; 
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace JiangXiaoMod.Code.Powers;

public class DawnPower : CustomPowerModel
{
    public const string PowerId = "DawnPower";
    private const string VarM = "M";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public DawnPower() : base()
    {
    }

    // 定義動態變量 M
    protected override IEnumerable<DynamicVar> CanonicalVars => [ new DynamicVar(VarM, 1m) ];

    // [STS2_Logic] 當能力層數變動時，更新描述中的 M 值
    private void UpdateDescriptionValue()
    {
        // 直接將 M 設定為當前的 Amount (層數)
        DynamicVars[VarM].BaseValue = Amount;
    }

    // 鉤子：當此能力的層數發生變化時（例如重複打出卡牌疊加時）
    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this) // 確保是自己的層數變動
        {
            UpdateDescriptionValue();
        }
        return base.AfterPowerAmountChanged(power, amount, applier, cardSource);
    }

    // 鉤子：初始獲得能力時更新一次數值
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        UpdateDescriptionValue();
        return base.AfterApplied(applier, cardSource);
    }

    // 核心抽牌邏輯
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        // 1. 權限檢查：只有當「當前開始回合的玩家」是「能力的持有者」時才執行
        if (Owner != null && Owner.Player == player && Owner.IsAlive)
        {
            // 2. 獲取自己身上的層數
            int selfAmount = (int)Amount;

            if (selfAmount > 0)
            {
                // 3. 執行抽牌，數量等於自己身上的層數
                await CardPileCmd.Draw(choiceContext, (uint)selfAmount, player);
            }
        }
    }
}