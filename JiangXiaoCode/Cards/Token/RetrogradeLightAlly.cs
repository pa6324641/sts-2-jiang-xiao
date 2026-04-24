using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Players;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class RetrogradeLightAlly : JiangXiaoCardModel
{
    public const string CardId = "RetrogradeLightAlly";
    public override string PortraitPath => $"retrograde_light.png".CardImagePath();

    // [Fix CS1061] TargetType 不含 Ally。STS2 中單體友方通常使用 TargetType.Self 
    // 若要指定隊友，建議設為 Self 並在 OnPlay 內邏輯處理，或確認是否有 SingleAlly 
    public RetrogradeLightAlly() : base(0, CardType.Skill, CardRarity.Basic, TargetType.AnyAlly, false)
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
            // [Fix CS1061] 根據報錯，HitPoints 缺失。在 STS2 中當前血量通常為 Health 或 CurrentHealth
            // 這裡修正為 Health。若編譯仍失敗，請嘗試 CurrentHealth
            int averageHp = (player.CurrentHp + target.CurrentHp) / 2;

            player.SetCurrentHpInternal(averageHp);
            target.SetCurrentHpInternal(averageHp);
        }
        await Task.CompletedTask;
    }
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        
    }
}