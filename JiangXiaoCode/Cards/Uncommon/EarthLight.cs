using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Token;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Cards.Uncommon;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class EarthLight : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-EARTH_LIGHT";
    
    public EarthLight() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        // [防呆] 暫時移除消耗的設定以確保編譯通過！
        // 成功編譯後，請參考你其他卡牌如何寫 "消耗"，例如：this.Exhaust = true;
    }

    public override string PortraitPath => "Temporarily.png".CardImagePath();

    protected override void ApplyRankLogic(Player? player, int skillRank) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        // 【重點】請確保你 List 裡面呼叫的 Token，都已經像上面的 BeiDouNineStarsToken 一樣建立好腳本了
        // 如果第三到第九星圖的腳本還沒建，請先將它們註解掉，否則一樣會報 CS0246 錯誤喔！
        var choices = new List<CardModel>
        {
            CombatState.CreateCard<BeiDouNineStarsToken>(Owner),
            CombatState.CreateCard<FlowerBladeToken>(Owner),
            CombatState.CreateCard<WitherBowToken>(Owner),
            CombatState.CreateCard<SoulOfDevouringSeaToken>(Owner),
            CombatState.CreateCard<StarMartialRecordsToken>(Owner),
            CombatState.CreateCard<InkFlowerToken>(Owner),
            CombatState.CreateCard<HolyCrossToken>(Owner),
            CombatState.CreateCard<PeakOfLifeToken>(Owner),
            CombatState.CreateCard<EarthStarMapToken>(Owner)
        };

        var toHandPrompt = new LocString("card_selection", "TO_HAND");
        var prefs = new CardSelectorPrefs(toHandPrompt, 1, 1);

        var selectedResults = await CardSelectCmd.FromSimpleGrid(
            choiceContext, 
            choices, 
            Owner, 
            prefs
        );
        
        var selectedCard = selectedResults?.FirstOrDefault();

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
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}