using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; 
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
using JiangXiaoMod.Code.Cards.Common;   
using JiangXiaoMod.Code.Cards.Uncommon; 
using JiangXiaoMod.Code.Cards.Rare;
using MegaCrit.Sts2.Core.HoverTips;

namespace JiangXiaoMod.Code.Cards.Rare;

/// <summary>
/// 真夏家刀法-清理門戶
/// 4費 攻擊 稀有
/// 修正說明：解決了特效路徑不存在導致的 NullReferenceException。
/// </summary>

[Pool(typeof(JiangXiaoCardPool))]
public class XiaBladeStyleTrue : CustomCardModel
{
    public XiaBladeStyleTrue() : base(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.JiangXiaoModBLADE];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(4m, ValueProp.Move), 
        new BlockVar(4m, ValueProp.Move)  
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<XiaBladeStyle1>(),
        HoverTipFactory.FromCard<XiaBladeStyle2>(),
        HoverTipFactory.FromCard<XiaBladeStyle3>()
    ];

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    private void UpdateStatsBasedOnRank()
    {
        // 增加 Owner 安全檢查
        if (Owner == null) return;

        int rank = JiangXiaoUtils.GetBladeRank(Owner);
        decimal baseCalc = 4m + (rank * 4m);

        // 組合技判定：檢查玩家的永久牌組
        bool hasStyle1 = Owner.Deck.Cards.Any(c => c is XiaBladeStyle1);
        bool hasStyle2 = Owner.Deck.Cards.Any(c => c is XiaBladeStyle2);
        bool hasStyle3 = Owner.Deck.Cards.Any(c => c is XiaBladeStyle3);

        if (hasStyle1 && hasStyle2 && hasStyle3)
        {
            baseCalc *= 4m;
        }

        DynamicVars.Damage.BaseValue = baseCalc;
        DynamicVars.Block.BaseValue = baseCalc;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        UpdateStatsBasedOnRank();
        
        // [修正] 更加嚴謹的目標與實體檢查
        if (cardPlay.Target == null || Owner?.Creature == null) return;

        // 執行格擋
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // [修正] 使用標準路徑 "vfx/vfx_attack_slash" 以避免 NullRef
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") 
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}