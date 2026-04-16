using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Extensions; 
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.Token;
using BaseLib.Extensions;

namespace JiangXiaoMod.Code.Cards.Basic;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class Blessing : CustomCardModel
{
    public const string CardId = "JIANGXIAOMOD-BLESSING";
    public const string VarHeal = "HealAmount";

    public Blessing() : base(1, CardType.Skill, CardRarity.Basic, TargetType.None)
    {
    }
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(VarHeal, 6m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star)
    ];

    public void UpdateStatsBasedOnRank()
    {
        int rank = JiangXiaoUtils.GetSkillRank(Owner);
        decimal baseHeal = IsUpgraded ? 12m : 6m; 
        DynamicVars[VarHeal].BaseValue = baseHeal + (rank - 1) * 6m;
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

        // 1. 準備選項清單
        var choices = new List<CardModel>();

        if (rank < 5)
        {
            // 預設 3 張選項
            choices.Add(CreateAndSetupToken<BlessingAllyToken>(currentHeal));
            choices.Add(CreateAndSetupToken<BlessingEnemyToken>(currentHeal));
            choices.Add(CreateAndSetupToken<BlessingSelfToken>(currentHeal));
        }
        else
        {
            // 鑽石等級 5 張選項
            choices.Add(CreateAndSetupToken<BlessingAllyToken>(currentHeal));
            choices.Add(CreateAndSetupToken<BlessingEnemyToken>(currentHeal));
            choices.Add(CreateAndSetupToken<BlessingSelfToken>(currentHeal));
            choices.Add(CreateAndSetupToken<BlessingAllAllyToken>(currentHeal));
            choices.Add(CreateAndSetupToken<BlessingAllEnemyToken>(currentHeal));
        }

        // 2. 彈出3選一或5選一畫面
        var selectedCard = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            Owner
        );

        // 3. 將選中的卡片以動畫方式加入手牌
        if (selectedCard != null)
        {
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(
                    selectedCard,
                    PileType.Hand,
                    false, 
                    CardPilePosition.Top 
                )
            );
        }
    }
    // 輔助方法：生成 Token 並同步當前的治癒數值
    private T CreateAndSetupToken<T>(decimal heal) where T : CustomCardModel
    {
        T token;

        // 檢查是否在戰鬥狀態中
        if (CombatState != null)
        {
            // 戰鬥中：直接透過 CombatState 創建，會自動處理所有戰鬥邏輯掛鉤
            token = CombatState.CreateCard<T>(Owner);
            
            if (token.DynamicVars.ContainsKey(VarHeal))
            {
                token.DynamicVars[VarHeal].BaseValue = heal;
            }
        }
        else
        {
            // 非戰鬥狀態 (例如圖鑑預覽)：
            // 從數據庫獲取該卡片的模板，並調用 ToMutable() 生成一個可以修改數值的實體
            // 注意：這裡必須進行強制轉型 (T)
            token = (T)MegaCrit.Sts2.Core.Models.ModelDb.Card<T>().ToMutable();

            if (token.DynamicVars.ContainsKey(VarHeal))
            {
                // 當不在戰鬥中時，回傳您要求的預設 6 點
                token.DynamicVars[VarHeal].BaseValue = 6m; 
            }
        }

        return token;
    }
}