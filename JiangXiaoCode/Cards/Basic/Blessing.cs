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
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class Blessing : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-BLESSING";
    public const string VarHeal = "HealAmount";

    private const decimal DefaultHeal = 6m;
    private const decimal BonusHealOnUpgrade = 3m; // 改名以避免邏輯混淆
    private const decimal RankScaling = 3m;

    public Blessing() : base(1, CardType.Skill, CardRarity.Basic, TargetType.None)
    {
        // 使用基類輔助方法添加關鍵字與提示
        JJKeywordAndTip(JiangXiaoModKeywords.Star);
        JJCustomVar(VarHeal, 6m);
    }

    public override string PortraitPath => "blessing.png".CardImagePath();

    // protected override IEnumerable<DynamicVar> CanonicalVars => [
    //     new DynamicVar(VarHeal, DefaultHeal)
    // ];

    /// <summary>
    /// STS2 核心邏輯：計算星技品質對數值的加成
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        // [修正邏輯]：基礎值應為 (初始值 + 升級加成) + (等級成長)
        decimal baseHeal = DefaultHeal + (IsUpgraded ? BonusHealOnUpgrade : 0m);
        DynamicVars[VarHeal].BaseValue = baseHeal + (skillRank - 1) * RankScaling;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 升級變為 0 費
        UpdateStatsBasedOnRank();
    }

    // 確保進入戰鬥、獲取卡牌時數值即時更新
    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }

    // protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    // {
    //     if (CombatState == null || Owner == null) return;

    //     // 1. 確保最新數值並獲取當前治療量
    //     UpdateStatsBasedOnRank();
    //     decimal currentHeal = DynamicVars[VarHeal].BaseValue;
    //     int rank = JiangXiaoUtils.GetSkillRank(Owner);

    //     // 2. 準備選項清單
    //     var choices = new List<CardModel>
    //     {
    //         CreateAndSetupToken<BlessingAllyToken>(currentHeal),
    //         CreateAndSetupToken<BlessingEnemyToken>(currentHeal),
    //         CreateAndSetupToken<BlessingSelfToken>(currentHeal)
    //     };

    //     // 星技品質 5 級 (星空期) 解鎖群體治療
    //     if (rank >= 5)
    //     {
    //         choices.Add(CreateAndSetupToken<BlessingAllAllyToken>(currentHeal));
    //         choices.Add(CreateAndSetupToken<BlessingAllEnemyToken>(currentHeal));
    //     }

    //     // 3. 彈出二選一/多選一畫面
    //     var selectedCard = await CardSelectCmd.FromChooseACardScreen(
    //         choiceContext,
    //         choices,
    //         Owner
    //     );

    //     // 4. 將選中的卡片加入手牌
    //     if (selectedCard != null)
    //     {
    //         await CardPileCmd.AddGeneratedCardToCombat(
    //             selectedCard,
    //             PileType.Hand,
    //             false, // 是否本回合 0 費 (Token 本身通常設定為 0 費，故傳入 false)
    //             CardPilePosition.Top 
    //         );
    //     }
    // }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        // 1. 更新數值
        UpdateStatsBasedOnRank();
        decimal currentHeal = DynamicVars[VarHeal].BaseValue;
        int rank = JiangXiaoUtils.GetSkillRank(Owner);

        // 2. 準備選項
        var choices = new List<CardModel>
        {
            CreateAndSetupToken<BlessingAllyToken>(currentHeal),
            CreateAndSetupToken<BlessingEnemyToken>(currentHeal),
            CreateAndSetupToken<BlessingSelfToken>(currentHeal)
        };

        if (rank >= 5)
        {
            choices.Add(CreateAndSetupToken<BlessingAllAllyToken>(currentHeal));
            choices.Add(CreateAndSetupToken<BlessingAllEnemyToken>(currentHeal));
        }

        // 3. 處理選擇畫面
        CardModel? selectedCard = null;

        // 這裡我們直接手動 new 一個 LocString
        // [STS2_Correction] 手動建立提示字串，對應本地化檔案中的 "card_selection" -> "TO_HAND"
        var toHandPrompt = new LocString("card_selection", "TO_HAND");
        var prefs = new CardSelectorPrefs(toHandPrompt, 1, 1);

        // 判斷是否需要切換 UI 模式
        if (choices.Count <= 3)
        {
            // 3張以下使用精美的發現畫面
            selectedCard = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                choices,
                Owner
            );
        }
        else
        {
            // 5張時使用你找到的 FromSimpleGrid 網格畫面
            var selectedResults = await CardSelectCmd.FromSimpleGrid(
                choiceContext, 
                choices, 
                Owner, 
                prefs
            );
            selectedCard = selectedResults?.FirstOrDefault();
        }

        // 4. 加入手牌
        if (selectedCard != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(
                selectedCard,
                PileType.Hand,
                false,
                CardPilePosition.Top 
            );
        }
    }

    /// <summary>
    /// 泛型 Token 生成工廠
    /// </summary>
    private T CreateAndSetupToken<T>(decimal heal) where T : CustomCardModel
    {
        T token = (CombatState != null) 
            ? CombatState.CreateCard<T>(Owner) 
            : (T)ModelDb.Card<T>().ToMutable();

        // [優化]：統一同步 Token 的治療數值
        if (token.DynamicVars.ContainsKey(VarHeal))
        {
            token.DynamicVars[VarHeal].BaseValue = heal;
        }

        return token;
    }
}