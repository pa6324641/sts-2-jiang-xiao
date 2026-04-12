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

namespace JiangXiaoMod.Code.Cards.Uncommon;

/// <summary>
/// 夏家刀法第二式-豎刀奪愛
/// 2費 攻擊 罕見
/// 效果：獲得格擋並造成傷害，數值隨 BladeRank 提升（每級+1）。
/// </summary>


[Pool(typeof(JiangXiaoCardPool))]
public class XiaBladeStyle2 : CustomCardModel
{
    // [STS2_API] 調用基類構造函數：2費, 攻擊牌, 罕見(Uncommon), 目標為任一敵人
    public XiaBladeStyle2() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 設置關鍵字：使用刀法關鍵字 JiangXiaoModBLADE [cite: 36, 43]
    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModBLADE];

    /// <summary>
    /// 定義卡片變量：Damage (基礎 2) 與 Block (基礎 2) 
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(2m, ValueProp.Move), 
        new BlockVar(2m, ValueProp.Move)  
    ];

    /// <summary>
    /// 在戰鬥開始前或數值刷新時調用，確保與刀法等級同步 [cite: 39]
    /// </summary>
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    /// <summary>
    /// 核心成長邏輯：根據「刀法等級 (BladeRank)」動態計算 [cite: 7, 8, 42]
    /// </summary>
    private void UpdateStatsBasedOnRank()
    {
        // 調用 JiangXiaoUtils 獲取當前刀法等級 [cite: 7, 8]
        int rank = JiangXiaoUtils.GetBladeRank(Owner);

        // [邏輯] 傷害：基礎 2 + 每級 2；格擋：基礎 2 + 每級 2
        // 計算範例：Rank 1 為 2/2，Rank 2 為 4/4 [cite: 42]
        DynamicVars.Damage.BaseValue = 2m + (rank * 2m);
        DynamicVars.Block.BaseValue = 2m + (rank * 2m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 打出時即時刷新數值，確保符合當前等級 [cite: 42]
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
        // 升級建議：提升基礎傷害與格擋 (各+2) 
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
        EnergyCost.UpgradeBy(-2);
    }
}