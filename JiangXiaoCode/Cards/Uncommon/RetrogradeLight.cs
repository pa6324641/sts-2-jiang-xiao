using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Token;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class RetrogradeLight : JiangXiaoCardModel
{
    public const string CardId = "RetrogradeLight";

    public RetrogradeLight() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        // 1. 依照您的範例，先預生成兩張實體卡片作為選項
        var enemyToken = CombatState.CreateCard<RetrogradeLightEnemy>(Owner);
        var allyToken = CombatState.CreateCard<RetrogradeLightAlly>(Owner);

        var choices = new List<CardModel> { enemyToken, allyToken };

        // 2. 彈出二選一畫面
        var selectedCard = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            Owner
        );

        // 3. 依照您的範例，將選中的卡片以動畫方式加入手牌
        if (selectedCard != null)
        {
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(
                    selectedCard,
                    PileType.Hand,
                    false, // 不洗牌
                    CardPilePosition.Top // 置於手牌頂端（通常手牌無所謂位置）
                )
            );
        }
    }
        protected override void OnUpgrade()
    {
        // -費
        EnergyCost.UpgradeBy(-1);
    }
}