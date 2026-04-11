using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.CardModels;

public interface IWishable
{
    Task OnWish(PlayerChoiceContext choiceContext, CardPlay cardPlay);
}