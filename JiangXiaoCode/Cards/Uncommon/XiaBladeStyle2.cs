using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions; 
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using JiangXiaoMod.Code.Keywords;

namespace JiangXiaoMod.Code.Cards.Uncommon;

/// <summary>
/// JIANGXIAOMOD-XIA_BLADE_STYLE2
/// 夏家刀法第二式-豎刀奪愛
/// 2費 攻擊 罕見
/// 效果：獲得格擋並造成傷害，數值隨夏家刀法等級 (BladeRank) 提升。
/// </summary>
[Pool(typeof(JiangXiaoCardPool))]
public sealed class XiaBladeStyle2 : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-XIA_BLADE_STYLE2";

    public XiaBladeStyle2() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // [STS2_Optimization] 使用基類輔助方法初始化變量，無需重寫 CanonicalVars
        JJDamage(2m, ValueProp.Move);
        JJBlock(2m, ValueProp.Move);

        // [STS2_Optimization] 使用基類輔助方法添加關鍵字，確保提示視窗 (HoverTip) 自動生成
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModBLADE);
    }

    /// <summary>
    /// 核心成長邏輯：根據「夏家刀法等級」動態計算
    /// 此方法會在卡牌生成、戰鬥開始、每回合在手牌中時自動觸發
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // 獲取當前刀法等級 (調用 JiangXiaoUtils)
        int bladeRank = JiangXiaoUtils.GetBladeRank(player);

        // 定義基礎數值與成長成長係數
        // 根據您的描述：每級 +2 (Rank 1 為 4/4)
        decimal step = 2m; 
        decimal baseVal = IsUpgraded ? 4m : 2m; // 若升級則基礎值提升

        // 更新動態數值
        // 計算公式：基礎值 + (等級 * 成長係數)
        DynamicVars.Damage.BaseValue = baseVal + (bladeRank * step);
        DynamicVars.Block.BaseValue = baseVal + (bladeRank * step);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 安全檢查
        if (Owner?.Creature == null || cardPlay.Target == null) return;

        // 1. 執行格擋動作 (目標為玩家自己)
        // 直接傳入 DynamicVars.Block 物件，系統會自動處理當前的 BaseValue
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 執行攻擊動作 (目標為選中的敵人)
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") 
            .Execute(choiceContext);
    }

    /// <summary>
    /// 升級效果處理
    /// </summary>
    protected override void OnUpgrade()
    {
        // 費用 2 -> 0 (根據您的代碼 UpgradeBy(-2))
        EnergyCost.UpgradeBy(-2);

        // 刷新數值：這會觸發 ApplyRankLogic，並根據 IsUpgraded 切換基礎值
        UpdateStatsBasedOnRank();
    }
}