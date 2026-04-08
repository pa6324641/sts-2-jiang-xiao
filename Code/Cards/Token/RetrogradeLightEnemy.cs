using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class RetrogradeLightEnemy : JiangXiaoCardModel
{
    public const string CardId = "RetrogradeLightEnemy";

    public RetrogradeLightEnemy() : base(0, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy, false)
    {
    }

    public override HashSet<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner?.Creature;
        var target = cardPlay.Target;

        if (player != null && target != null)
        {
            // 計算均值並設定血量
            // [STS2_Compatibility] 屬性名為 HitPoints (大寫 P)
            int averageHp = (player.CurrentHp + target.CurrentHp) / 2;

            player.SetCurrentHpInternal(averageHp);
            target.SetCurrentHpInternal(averageHp);
        }
        await Task.CompletedTask;
    }
}