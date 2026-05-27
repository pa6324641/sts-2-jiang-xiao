using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat; 
using Godot;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Powers.StarMaps
{
    // --- 第四星圖：噬海之魂 ---
    public sealed class SoulOfDevouringSeaPower : StarMapPowerModel
    {
        public override string CustomPackedIconPath => "Test.png".PowerImagePath();
        // 定義 STS2 原生支援的動態變數名
        public const string VarThreshold = "M";
        
        // 運行時變數：斬殺線百分比 (預設 20%)
        private int _executeThreshold = 20;

        public SoulOfDevouringSeaPower() : base() 
        { 
        }

        /// <summary>
        /// [STS2_API] 動態變數註冊
        /// </summary>
        protected override IEnumerable<DynamicVar> GetCustomVars()
        {
            yield return new DynamicVar(VarThreshold, _executeThreshold);
        }

        /// <summary>
        /// [STS2_API] 星力等級更新邏輯
        /// </summary>
        protected override void ApplyRankLogic(Player? player, int powerLevel)
        {
            // 預留星圖升級擴充空間
        }

        /// <summary>
        /// [STS2_API] 利用此函數在選定目標時實時反覆計算的特性，預估傷害後的血量並動態變更外觀顏色
        /// </summary>
        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            decimal finalAmount = base.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource);

            // 安全防禦檢查
            if (target != null && dealer == base.Owner && target != base.Owner && !target.IsDead && cardSource != null)
            {
                // 【核心修正】：模擬傷害扣除。計算當前血量減去「預估傷害值」後的預期剩餘血量
                // 這裡使用 finalAmount 作為預估造成的傷害量
                float predictedHp = (float)target.CurrentHp - (float)cardSource.DynamicVars.Damage.BaseValue;
                if (predictedHp < 0f) predictedHp = 0f; // 防護保底，避免負數

                // 計算預期剩餘血量佔最大血量的百分比
                float hpPercent = (predictedHp / target.MaxHp) * 100f;

                // 獲取當前戰鬥房間的 Godot 視覺表現節點
                NCombatRoom? combatRoom = NCombatRoom.Instance;
                if (combatRoom != null)
                {
                    NCreature? nCreature = combatRoom.GetCreatureNode(target);
                    if (nCreature != null)
                    {
                        // 如果模擬受傷後的血量低於或等於 20% 斬殺線
                        if (hpPercent <= _executeThreshold)
                        {
                            // 進入斬殺預警：將怪物的整體視覺色調調為紫色（或你喜歡的斬殺警告色）
                            nCreature.Modulate = new Color(0.7f, 0.1f, 1.0f, 1.0f);
                        }
                        else
                        {
                            // 未進入斬殺預警：恢復正常白色渲染
                            nCreature.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                        }
                    }
                }
            }

            return finalAmount;
        }

        /// <summary>
        /// [STS2_API] 監聽任何生物受到傷害結算後的事件並執行斬殺特效
        /// </summary>
        public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
        {
            // 呼叫基底邏輯以防遺漏底層事件
            await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);

            // 嚴格的安全防禦檢查，防止 CS8602 空引用
            if (target == null || dealer == null || base.Owner == null)
            {
                return;
            }

            // 條件判斷：玩家造成的傷害、非自殘、目標活著、且有實質不落地傷害
            if (dealer == base.Owner && target != base.Owner && !target.IsDead && result.UnblockedDamage > 0)
            {
                // 【核心修正】：此處的 target.CurrentHp 已經是受傷後的血量
                // 精確判斷受傷後的血量百分比是否達到斬殺標準
                float hpPercent = ((float)target.CurrentHp / target.MaxHp) * 100f;

                if (hpPercent <= _executeThreshold)
                {
                    // 1. 觸發能力圖示閃爍
                    this.Flash();

                    // 2. 獲取當前戰鬥房間實體，並安全檢查
                    NCombatRoom? combatRoom = NCombatRoom.Instance;
                    if (combatRoom != null)
                    {
                        // 3. 安全獲取怪物的 Godot 視覺表現節點 (NCreature)
                        NCreature? nCreature = combatRoom.GetCreatureNode(target);

                        if (nCreature != null && nCreature.Hitbox != null)
                        {
                            // 4. 呼叫官方標準的 UI 淡出/關閉動畫，並返回 Tween 物件
                            Tween? uiTween = nCreature.AnimDisableUi();

                            if (uiTween != null)
                            {
                                // 5. 當 UI 動畫完成時，安全地釋放該怪物的 UI 節點
                                uiTween.TweenCallback(Callable.From(nCreature.QueueFreeSafely));
                            }

                            // 6. 實例化官方的毀滅/厄運特殊斬殺特效
                            NDoomVfx? doomVfx = NDoomVfx.Create(nCreature.Visuals, nCreature.Hitbox.GlobalPosition, nCreature.Hitbox.Size, true);

                            if (doomVfx != null)
                            {
                                // 7. 將特效節點添加至當前的戰鬥房間中播放
                                combatRoom.AddChild(doomVfx);
                            }
                        }
                    }

                    // 8. 最後調用核心指令系統，同步其底層邏輯狀態為死亡（直接擊殺）
                    await CreatureCmd.Kill(target);
                }
            }
        }
    }
}