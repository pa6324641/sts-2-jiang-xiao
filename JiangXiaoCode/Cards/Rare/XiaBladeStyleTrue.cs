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
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace JiangXiaoMod.Code.Cards.Rare;

/// <summary>
/// 真夏家刀法-清理門戶
/// 優化：採用 ModifyBlockMultiplicative 與 ModifyDamageMultiplicative 處理 4 倍邏輯。
/// 確保符合使用者提供的最新 API 簽名格式。
/// </summary>
[Pool(typeof(JiangXiaoCardPool))]
public class XiaBladeStyleTrue : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-XIA_BLADE_STYLE_TRUE";

    // 緩存變量：紀錄是否滿足組合技條件，避免在 Modify 勾子中頻繁掃描牌組
    private bool _isSetComplete = false;

    public XiaBladeStyleTrue() : base(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        JJBlock(4m, ValueProp.Move);
        JJDamage(4m, ValueProp.Move);
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModBLADE);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<XiaBladeStyle1>(),
        HoverTipFactory.FromCard<XiaBladeStyle2>(),
        HoverTipFactory.FromCard<XiaBladeStyle3>()
    ];

    /// <summary>
    /// 核心邏輯：計算基礎技藝加成，並掃描組合技狀態。
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        if (player == null) return;

        // 1. 計算基於「刀法等級」的基礎值
        int bladeRank = JiangXiaoUtils.GetBladeRank(player);
        decimal baseVal = IsUpgraded ? 6m : 4m;
        decimal baseCalc = baseVal + (bladeRank * 4m);

        // 2. 組合技掃描（掃描永久牌組 Master Deck）
        var deckCards = player.Deck.Cards;
        bool hasStyle1 = false;
        bool hasStyle2 = false;
        bool hasStyle3 = false;

        foreach (var card in deckCards)
        {
            if (card is XiaBladeStyle1) hasStyle1 = true;
            else if (card is XiaBladeStyle2) hasStyle2 = true;
            else if (card is XiaBladeStyle3) hasStyle3 = true;
            if (hasStyle1 && hasStyle2 && hasStyle3) break;
        }

        // 更新緩存狀態
        _isSetComplete = (hasStyle1 && hasStyle2 && hasStyle3);

        // 3. 更新 DynamicVars 的 BaseValue
        if (DynamicVars.TryGetValue("Damage", out var dmgVar)) dmgVar.BaseValue = baseCalc;
        if (DynamicVars.TryGetValue("Block", out var blkVar)) blkVar.BaseValue = baseCalc;
    }

    /// <summary>
    /// [STS2_API] 傷害乘法修正勾子
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        decimal finalAmount = base.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource);

        // [安全檢查]：只有當「傷害來源」是這張卡牌本身，且達成組合技時，才翻倍
        if (cardSource == this && _isSetComplete)
        {
            finalAmount *= 4m;
        }

        return finalAmount;
    }

    /// <summary>
    /// [STS2_API] 格擋乘法修正勾子
    /// </summary>
    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        decimal finalBlock = base.ModifyBlockMultiplicative(target, block, props, cardSource, cardPlay);

        // [安全檢查]：只有當「格擋來源」是這張卡牌本身，且達成組合技時，才翻倍
        if (cardSource == this && _isSetComplete)
        {
            finalBlock *= 4m;
        }

        return finalBlock;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 確保數值在出牌時已根據最新狀態計算
        UpdateStatsBasedOnRank();
        
        if (cardPlay.Target == null || Owner?.Creature == null) return;

        // 1. 執行格擋：傳入 BlockVar 對象，引擎會自動觸發 ModifyBlockMultiplicative
        if (DynamicVars.TryGetValue("Block", out var blkVar) && blkVar is BlockVar block)
        {
            await CreatureCmd.GainBlock(Owner.Creature, block, cardPlay);
        }

        // 2. 執行攻擊：傳入基礎數值，FromCard(this) 會自動觸發 ModifyDamageMultiplicative
        if (DynamicVars.TryGetValue("Damage", out var dmgVar) && dmgVar is DamageVar damage)
        {
            await DamageCmd.Attack(damage.BaseValue) 
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash") 
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        // 費用 4 -> 3
        EnergyCost.UpgradeBy(-1);
        // 刷新數值
        UpdateStatsBasedOnRank();
    }
}