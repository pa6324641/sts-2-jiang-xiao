using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using JiangXiaoMod.Code.Relics;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Keywords;

namespace JiangXiaoMod.Code.Cards.Common;

[Pool(typeof(JiangXiaoCardPool))]
public class SecondaryFace : JiangXiaoCardModel
{
    public const string CardId = "SecondaryFace";

    public SecondaryFace() : base(2, CardType.Skill, CardRarity.Common, TargetType.None)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("M", 1m)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(JiangXiaoModKeywords.Star),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];


    public void UpdateStatsBasedOnRank()
    {
        var player = Owner;
        if (player == null) return;

        var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        int currentRank = relic?.SkillRank ?? 1;

        // 設定 M 值：rank 1-3 -> 1, rank 4-5 -> 2, rank 6-7 -> 7
        decimal mValue = currentRank switch
        {
            >= 6 => 7m,
            >= 4 => 2m,
            _ => 1m
        };

        if (DynamicVars.ContainsKey("M"))
        {
            DynamicVars["M"].BaseValue = mValue;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var combat = player?.Creature?.CombatState;
        if (player == null || combat == null) return;

        UpdateStatsBasedOnRank();
        int copyCount = (int)DynamicVars["M"].BaseValue;

        // 💡 修正選牌體驗：顯式禁用手動確認
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = false, // 強制點擊即生效
            Cancelable = false                 // 禁用取消以簡化流程
        };

        // 1. 從手牌選擇卡片
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext, 
            player, 
            prefs, 
            (card) => card.Id != this.Id, 
            this
        );

        CardModel? targetCard = selectedCards?.FirstOrDefault();
        if (targetCard == null) return;

        // 2. 從全域卡庫獲取卡片模板
        CardModel? template = ModelDb.AllCards.FirstOrDefault(c => c.Id == targetCard.Id);
        if (template == null) return;

        for (int i = 0; i < copyCount; i++)
        {
            CardModel copy = combat.CreateCard(template, player);
            
            // 3. 狀態同步
            if (targetCard.IsUpgraded)
            {
                CardCmd.Upgrade(copy);
            }

            // 4. 屬性修改
            copy.EnergyCost.SetCustomBaseCost(0);
            copy.AddKeyword(CardKeyword.Exhaust);

            // 5. 加入戰鬥手牌 (true 會觸發卡片飛入手牌的動畫)
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, true);
        }
    }

    // 💡 解決 Missing Sprite 報錯 (若無專屬圖，可先指定為打擊的圖)
    // 這樣遊戲就不會去 atlas 找不存在的 SecondaryFace 圖了
    // protected override string CustomImagePath => "cards/strike_jiang_xiao.png".CardImagePath();
}