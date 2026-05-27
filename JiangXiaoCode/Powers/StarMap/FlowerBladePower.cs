using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using JiangXiaoMod.Code.Extensions; 
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards; 
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Cards.Uncommon;
using JiangXiaoMod.Code.Cards.Basic;
using JiangXiaoMod.Code.Cards.Common;

namespace JiangXiaoMod.Code.Powers.StarMaps
{
    /// <summary>
    /// 第二星圖：花里胡哨之刃 (花刃)
    /// 效果：指定類型卡牌增加 9 點傷害。每打出該類型卡牌 +1 層，9 層後獲得特定大招並歸零。
    /// </summary>
    public sealed class FlowerBladePower : StarMapPowerModel
    {
        public override string CustomPackedIconPath => "Test.png".PowerImagePath();
        public override PowerStackType StackType => PowerStackType.Counter;

        // --- 基礎數值定義 ---
        private const decimal BaseDamageBonus = 12m;
        private const int BaseTriggerThreshold = 9;
        
        // [STS2_API] 變數名稱修正：改為 STS 系統原生支援的安全變數名 M 與 Y
        public const string VarDmg = "M";
        public const string VarThreshold = "Y";

        // --- 運行時數值 ---
        private int _progress = 0;
        private decimal _currentDamageBonus; // [修正] 用私有變數來儲存當前傷害加成
        private int _currentThreshold;       // [修正] 用私有變數來儲存當前閾值

        // 將顯示數值綁定到進度層數
        public override int DisplayAmount => _progress;

        public FlowerBladePower() : base()
        {
            _progress = 0;
            _currentDamageBonus = BaseDamageBonus;
            _currentThreshold = BaseTriggerThreshold;
        }

        /// <summary>
        /// [STS2_API] 動態變數註冊
        /// 註冊後可於 JSON 文本中使用 {M} 和 {Y} 進行本地化顯示
        /// </summary>
        protected override IEnumerable<DynamicVar> GetCustomVars()
        {
            // [修正] 永遠回傳當前的私有變數，避免直接操作 DynamicVars 字典
            yield return new DynamicVar(VarDmg, _currentDamageBonus);
            yield return new DynamicVar(VarThreshold, _currentThreshold);
        }

        /// <summary>
        /// [STS2_API] 星力等級更新邏輯
        /// </summary>
        protected override void ApplyRankLogic(Player? player, int powerLevel)
        {
            // [修正] 透過修改私有變數來更新數值，避開字典操作閃退
            _currentDamageBonus = BaseDamageBonus + (powerLevel - 1);
            
            InvokeDisplayAmountChanged();
        }

        /// <summary>
        /// 輔助函數：判斷是否為「花刃」目標卡牌
        /// </summary>
        private bool IsTargetCard(CardModel card)
        {
            if (card == null) return false;

            // [STS2_API] 判定機制
            bool hasKeyword = card.IsJiangXiaoModBLADE() || 
                              (card.Keywords != null && card.Keywords.Contains(JiangXiaoModKeywords.JiangXiaoModBLADE));
            
            bool isJiangXiaoStrike = card is StrikeJiangXiao;
            bool isQingMang = card is QingMang;

            return hasKeyword || isJiangXiaoStrike || isQingMang;
        }

        /// <summary>
        /// [STS2_API] 傷害加法修正勾子
        /// </summary>
        public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            decimal finalAmount = base.ModifyDamageAdditive(target, amount, props, dealer, cardSource);

            // 只有當傷害來源是一張卡，且為目標卡牌時才增加傷害
            if (cardSource != null && IsTargetCard(cardSource) && cardSource.Owner.Creature == base.Owner)
            {
                // [修正] 改為讀取私有變數
                finalAmount += _currentDamageBonus;
            }

            return finalAmount;
        }

        /// <summary>
        /// [STS2_API] 監聽卡牌打出事件
        /// </summary>
        public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await base.AfterCardPlayedLate(choiceContext, cardPlay);

            if (cardPlay.Card.Owner?.Creature == base.Owner && IsTargetCard(cardPlay.Card)) 
            {
                await CounterCardPlayed(choiceContext);
            }
        }

        /// <summary>
        /// 處理層數增加與觸發大招
        /// </summary>
        private async Task CounterCardPlayed(PlayerChoiceContext choiceContext)
        {
            _progress++;
            
            if (_progress >= _currentThreshold)
            {
                if (base.Owner.Player != null)
                {
                    _progress = 0; // 層數歸零
                    
                    // 註：若此處 this.Flash() 依然引起閃退，可能需要改為 STS2 的視覺特效發送器。
                    // 建議先保留測試，若再閃退可將此行註解。
                    this.Flash();  
                    
                    StrikeJiangXiao rewardCard = (CombatState != null) 
                        ? CombatState.CreateCard<StrikeJiangXiao>(Owner.Player) 
                        : (StrikeJiangXiao)ModelDb.Card<StrikeJiangXiao>().ToMutable();

                    if (rewardCard != null)
                    {
                        await CardPileCmd.AddGeneratedCardToCombat(
                            rewardCard,
                            PileType.Hand,
                            false,
                            CardPilePosition.Top 
                        );
                    }
                }
            }
            
            InvokeDisplayAmountChanged();
        }
    }
}