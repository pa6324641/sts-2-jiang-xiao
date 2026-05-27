using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Commands;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Powers.StarMaps
{
    // --- 第三星圖：凋零弓 ---
    public sealed class WitherBowPower : StarMapPowerModel
    {
        public override string CustomPackedIconPath => "Test.png".PowerImagePath();
        // 變數名稱修正：改為 STS 系統原生支援的安全變數名 X, M, Y
        private const string VarDamageBonus = "X";
        private const string VarPoison = "M";
        private const string VarDoom = "Y";

        public WitherBowPower() : base() { }

        /// <summary>
        /// [STS2_API] 動態變數註冊
        /// </summary>
        protected override IEnumerable<DynamicVar> GetCustomVars()
        {
            // 修正顯示邏輯：直接註冊整數 20，用於文本正常顯示 20%
            yield return new DynamicVar(VarDamageBonus, 20m); 
            yield return new DynamicVar(VarPoison, 9m);
            yield return new DynamicVar(VarDoom, 9m);
        }

        protected override void ApplyRankLogic(Player? player, int powerLevel)
        {
            // 當星圖能力被賦予或等級改變時的初始化邏輯
            // 若依等級成長，可在此修改 DynamicVars[VarDamageBonus].BaseValue 等
            
            InvokeDisplayAmountChanged();
        }

        /// <summary>
        /// [STS2_API] 傷害乘法修正勾子
        /// 全局檢測：若打出的卡牌具備「弓箭精通」標籤，則進行傷害乘算修正
        /// </summary>
        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            decimal finalAmount = base.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource);

            // [安全檢查]：當傷害來源卡牌存在，且該卡牌擁有「弓箭精通」擴充屬性時
            if (cardSource != null && cardSource.IsJiangXiaoModBOW() && cardSource.Owner.Creature == base.Owner)
            {
                // 將 20 轉換回 1.20 的乘法倍率 (1 + 20 / 100)
                decimal bonusPercent = DynamicVars[VarDamageBonus].BaseValue;
                decimal multiplier = 1m + (bonusPercent / 100m);
                
                finalAmount *= multiplier;
            }

            return finalAmount;
        }

        /// <summary>
        /// [STS2_API] 卡牌打出後的非同步後置勾子
        /// 用於處理卡牌結算後，賦予目標毒與災厄的行為
        /// </summary>
        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            // 檢查卡牌擁有者是否為此 Power 的持有者，且卡牌屬於「弓箭精通」
            if (cardPlay.Card.Owner?.Creature == base.Owner && cardPlay.Card.IsJiangXiaoModBOW())
            {
                var target = cardPlay.Target; 

                if (target != null && !target.IsDead)
                {
                    int poisonLayers = (int)DynamicVars[VarPoison].BaseValue;
                    int doomLayers = (int)DynamicVars[VarDoom].BaseValue;

                    // 使用 STS2 的非同步 Action 佇列
                    await PowerCmd.Apply<PoisonPower>(target, poisonLayers, base.Owner, cardPlay.Card);
                    await PowerCmd.Apply<DoomPower>(target, doomLayers, base.Owner, cardPlay.Card);
                }
            }

            // 呼叫基底實作確保其餘事件正常傳遞
            await base.AfterCardPlayed(context, cardPlay);
        }
    }
}