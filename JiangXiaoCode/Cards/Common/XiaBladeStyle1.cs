using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Extensions; 
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Common;

/// <summary>
/// 夏家刀法第一式-橫刀奪愛
/// 1費 攻擊 普通 
/// 效果：獲得格擋並造成傷害，數值隨 BLADERank 提升。
/// </summary>

[Pool(typeof(JiangXiaoCardPool))]
public class XiaBladeStyle1 : CustomCardModel
{
    // 調用基類構造函數：1費, 攻擊牌, 普通稀有度, 目標為任一敵人
    public XiaBladeStyle1() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 設置關鍵字：使用刀法關鍵字 JiangXiaoModBLADE
    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModBLADE];

    /// <summary>
    /// 定義卡片變量：Damage (傷害) 與 Block (格擋)
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(1m, ValueProp.Move), // 基礎傷害 1
        new BlockVar(1m, ValueProp.Move)  // 基礎格擋 1
    ];

    /// <summary>
    /// 在戰鬥開始前或數值刷新時調用，確保數值與刀法等級同步
    /// </summary>
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 核心邏輯：根據「刀法等級 (BladeRank)」動態計算傷害與格擋
    /// </summary>
    private void UpdateStatsBasedOnRank()
    {
        // 從工具類獲取當前的刀法等級 (需確保 JiangXiaoUtils 內有 GetBladeRank 方法)
        // [STS2_Optimization] 若手冊未定義 GetBladeRank，此處假設與 GetUnarmedRank 邏輯一致
        int rank = JiangXiaoUtils.GetBladeRank(Owner);

        // [邏輯] 基礎 1 + 每級 1
        // 計算結果：Rank 0 = 1, Rank 1 = 2, Rank 2 = 3...
        decimal finalValue = 1m + (rank * 1m);

        DynamicVars.Damage.BaseValue = finalValue;
        DynamicVars.Block.BaseValue = finalValue;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 打出前強制刷新數值，避免等級變動未即時更新
        UpdateStatsBasedOnRank();
        
        // 安全檢查
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (Owner?.Creature == null) return;

        // 2. 執行格擋動作 (目標為使用者自己)
        // 使用 CreatureCmd.GainBlock 確保受到力量或能力加成
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 3. 執行攻擊動作 (目標為選中的敵人)
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") // 橫刀奪愛使用揮砍特效
            .Execute(choiceContext);
    }

    /// <summary>
    /// 升級效果處理
    /// </summary>
    protected override void OnUpgrade()
    {
        // 升級建議：基礎值提升 (例如 2->5)，使成長基數更高
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(1m);
        EnergyCost.UpgradeBy(-1);
    }
}