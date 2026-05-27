using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.ValueProps;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Powers.StarMaps
{
    // --- 第六星圖：油墨花 ---
    public sealed class InkFlowerPower : StarMapPowerModel
    {
        public override string CustomPackedIconPath => "Test.png".PowerImagePath();
        // 定義動態變數名，方便本地化文本調用
        public const string VarSlipperyAmount = "M";
        
        // 運行時變數：Slippery 層數 (預設 9)
        private int _slipperyAmount = 9;

        // 效果：獲得9層SlipperyPower，每回合開始後重置次數
        public InkFlowerPower() : base() 
        { 
        }

        /// <summary>
        /// [STS2_API] 動態變數註冊
        /// </summary>
        protected override IEnumerable<DynamicVar> GetCustomVars()
        {
            yield return new DynamicVar(VarSlipperyAmount, _slipperyAmount);
        }

        /// <summary>
        /// [STS2_API] 星力等級更新邏輯
        /// </summary>
        protected override void ApplyRankLogic(Player? player, int powerLevel)
        {
            // 預留空間：若未來高等級星圖會提高基礎層數，可在此調整 _slipperyAmount
        }

        /// <summary>
        /// [STS2_API] 能力首次賦予後觸發 (已套用最新規範鉤子)
        /// </summary>
        public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
        {
            await base.AfterApplied(applier, cardSource);
            decimal amount = DynamicVars[VarSlipperyAmount].BaseValue;
            
            // STS2 中，能力通常可以透過 this.Owner 取得當前持有者
            if (this.Owner != null)
            {
                this.Flash();
                await PowerCmd.Apply<SlipperyPower>(this.Owner, amount, this.Owner, cardSource);
            }
        }

        /// <summary>
        /// [STS2_API] 玩家每回合開始後觸發 (已套用最新規範鉤子)
        /// </summary>
        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            await base.AfterPlayerTurnStart(choiceContext, player);
            decimal amount = DynamicVars[VarSlipperyAmount].BaseValue;

            if (player?.Creature != null && player.Creature == base.Owner)
            {
                this.Flash();

                // [STS1_Legacy] 說明：此處邏輯參考自 STS1 查詢與給予/減少能力層數的做法。
                // 為了達到「重置為9層」的效果，我們計算當前層數的差額並進行補足或扣除。
                int currentSlippery = player.Creature.GetPowerAmount<SlipperyPower>(); 
                
                if (currentSlippery < _slipperyAmount)
                {
                    // 層數不足，補足層數
                    int amountToAdd = _slipperyAmount - currentSlippery;
                    await PowerCmd.Apply<SlipperyPower>(player.Creature, amountToAdd, player.Creature, null);
                }
                else if (currentSlippery > _slipperyAmount)
                {
                    // 層數過多，扣除多餘層數
                    int amountToRemove = currentSlippery - _slipperyAmount;
                    await PowerCmd.Apply<SlipperyPower>(player.Creature, -amountToRemove, player.Creature, null);
                }
            }
        }
        public override async Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            // 1. 安全檢查
            if (target != Owner || dealer == null || dealer == target)
            {
                return;
            }
            await CreatureCmd.Stun(dealer);
            await base.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource);
        }
    }
}