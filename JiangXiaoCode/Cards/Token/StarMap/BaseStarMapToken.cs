using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public abstract class BaseStarMapToken : JiangXiaoCardModel
{
    // [修正 1]：將 CardRarity 更改為範本中正確的 Token
    protected BaseStarMapToken() : base(1, CardType.Power, CardRarity.Token, TargetType.None)
    {
        // [修正 2 & 3]：先移除錯誤的屬性設定以通過編譯。
        // 請參考 Blessing，使用你的擴展方法來加上「消耗」與「虛無」，例如：
        JJKeywordAndTip(CardKeyword.Exhaust);
        // JJKeywordAndTip(JiangXiaoModKeywords.Ethereal);
    }

    protected override void ApplyRankLogic(Player? player, int skillRank) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner?.Creature;
        if (player == null) return;

        var powersToRemove = player.Powers.Where(p => p is StarMapPowerModel).ToList();
        
        foreach (var power in powersToRemove)
        {
            // [修正 4]：根據範本，Remove 只需傳入 power 本身，不再需要傳入 player！
            await PowerCmd.Remove(power);
        }

        // 施加新的星圖
        await ApplyNewStarMap(choiceContext, player);
    }

    protected abstract Task ApplyNewStarMap(PlayerChoiceContext context, Creature player);
}