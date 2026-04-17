using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Token;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class Blessing : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-BLESSING";
    public const string VarHeal = "HealAmount";

    private const decimal DefaultHeal = 6m;
    private const decimal UpgradeHeal = 12m;
    private const decimal RankScaling = 6m;

    public Blessing() : base(1, CardType.Skill, CardRarity.Basic, TargetType.None)
    {
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
    }

    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(VarHeal, DefaultHeal)
    ];

    // protected override IEnumerable<IHoverTip> ExtraHoverTips => [
    //     HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star)
    // ];

    // // 必須包含星技關鍵字
    // public override HashSet<CardKeyword> CanonicalKeywords => [JiangXiaoModKeywords.Star];

    public void UpdateStatsBasedOnRank()
    {
        int rank = JiangXiaoUtils.GetSkillRank(Owner);
        decimal baseHeal = IsUpgraded ? UpgradeHeal : DefaultHeal;
        DynamicVars[VarHeal].BaseValue = baseHeal + (rank - 1) * RankScaling;
    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        UpdateStatsBasedOnRank();
    }

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); 
        UpdateStatsBasedOnRank();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        UpdateStatsBasedOnRank();
        decimal currentHeal = DynamicVars[VarHeal].BaseValue;
        int rank = JiangXiaoUtils.GetSkillRank(Owner);

        // 1. 準備選項清單 (恢復為明確的 Token 生成)
        var choices = new List<CardModel>();
        
        choices.Add(CreateAndSetupToken<BlessingAllyToken>(currentHeal));
        choices.Add(CreateAndSetupToken<BlessingEnemyToken>(currentHeal));
        choices.Add(CreateAndSetupToken<BlessingSelfToken>(currentHeal));

        if (rank >= 5)
        {
            choices.Add(CreateAndSetupToken<BlessingAllAllyToken>(currentHeal));
            choices.Add(CreateAndSetupToken<BlessingAllEnemyToken>(currentHeal));
        }

        // 2. 彈出 3 選 1 或 5 選 1 畫面
        var selectedCard = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            Owner
        );

        // 3. 將選中的卡片加入手牌 (修正參數調用)
        if (selectedCard != null)
        {
            // [修正]：移除具名參數 "isFreeThisTurn:"，改回 STS2 標准的位置參數
            await CardPileCmd.AddGeneratedCardToCombat(
                selectedCard,
                PileType.Hand,
                false, // 這裡的 false 代表不耗費 0 費 (依據原卡費用)
                CardPilePosition.Top 
            );
        }
    }

    /// <summary>
    /// 輔助方法：恢復為泛型實作，解決 CombatState.CreateCard 的編譯問題
    /// </summary>
    private T CreateAndSetupToken<T>(decimal heal) where T : CustomCardModel
    {
        T token;

        if (CombatState != null)
        {
            // [修正]：使用泛型 T 以符合 CombatState.CreateCard<T> 的要求 
            token = CombatState.CreateCard<T>(Owner);
        }
        else
        {
            // [修正]：ModelDb 應調用 .Card<T>() 而非 .GetModel 
            token = (T)ModelDb.Card<T>().ToMutable();
        }

        if (token.DynamicVars.ContainsKey(VarHeal))
        {
            token.DynamicVars[VarHeal].BaseValue = heal;
        }

        return token;
    }
}