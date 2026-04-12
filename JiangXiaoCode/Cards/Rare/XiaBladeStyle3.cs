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

namespace JiangXiaoMod.Code.Cards.Rare;

/// <summary>
/// 夏家刀法第三式-斜刀奪愛
/// 3費 攻擊 稀有
/// 效果：獲得格擋並造成傷害，數值隨 BladeRank 提升（每級+3）。
/// </summary>

[Pool(typeof(JiangXiaoCardPool))]
public class XiaBladeStyle3 : CustomCardModel
{
    // [STS2_API] 調用基類構造函數：3費, 攻擊牌, 稀有(Rare), 目標為任一敵人
    public XiaBladeStyle3() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 設置關鍵字：使用刀法關鍵字 JiangXiaoModBLADE
    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModBLADE];

    /// <summary>
    /// 定義卡片變量：Damage (基礎 3) 與 Block (基礎 3)
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3m, ValueProp.Move), 
        new BlockVar(3m, ValueProp.Move)  
    ];

    /// <summary>
    /// 在戰鬥開始前或數值刷新時調用，確保與刀法等級同步
    /// </summary>
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 核心成長邏輯：根據「刀法等級 (BladeRank)」動態計算
    /// </summary>
    private void UpdateStatsBasedOnRank()
    {
        // 調用 JiangXiaoUtils 獲取當前刀法等級
        int rank = JiangXiaoUtils.GetBladeRank(Owner);

        // [邏輯] 傷害：基礎 3 + 每級 3；格擋：基礎 3 + 每級 3
        // 計算範例：Rank 0 = 3/3, Rank 1 = 6/6, Rank 2 = 9/9
        DynamicVars.Damage.BaseValue = 3m + (rank * 3m);
        DynamicVars.Block.BaseValue = 3m + (rank * 3m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 打出時即時刷新數值，確保反映最新的刀法等級
        UpdateStatsBasedOnRank();
        
        // 安全檢查
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (Owner?.Creature == null) return;

        // 2. 執行格擋動作 (目標為玩家自己)
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 3. 執行攻擊動作 (目標為選中的敵人)
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") // 使用刀法揮砍特效
            .Execute(choiceContext);
    }

    /// <summary>
    /// 升級效果處理
    /// </summary>
    protected override void OnUpgrade()
    {
        // 升級建議：提升基礎數值。考慮到稀有卡與 3 費成本，基礎值各提升 +4
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(3m);
        EnergyCost.UpgradeBy(-3);
    }
}