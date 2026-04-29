using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using JiangXiaoMod.Code.Extensions;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Keywords;
using JiangXiaoMod.Code.Cards.CardModels;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public sealed class CunjinEcho : JiangXiaoCardModel
{
    private const string VarEcho = "M";

    public CunjinEcho() : base(
        cost: 3, 
        type: CardType.Skill, 
        rarity: CardRarity.Rare, 
        target: TargetType.None)
    {
        // 1. 添加徒手戰鬥關鍵字提示
        JJKeywordAndTip(JiangXiaoModKeywords.JiangXiaoModUNARMED);
        
        // 2. [STS2_原生支援] 添加原生的「重放 (Replay)」懸浮提示
        // 註：這能確保玩家把滑鼠移到卡牌上時，能看到系統內建的重放解釋
        ExtraHoverTips.Append(HoverTipFactory.Static(StaticHoverTip.ReplayStatic));

        // 3. 初始化動態變數 M
        JJCustomVar(VarEcho, 1m);
    }

    /// <summary>
    /// 核心數值邏輯：統一公式計算
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        int unarmedRank = JiangXiaoUtils.GetUnarmedRank(player);
        decimal echoAmount = unarmedRank switch
        {
            <= 3 => 1m,
            <= 6 => 2m,
            _ => 2m
        };

        // int echoAmount = 1 + (int)Math.Floor((unarmedRank - 1) / 3m);

        // 更新描述中的動態變數 {M}
        DynamicVars[VarEcho].BaseValue = echoAmount;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.PlayerCombatState == null) return;

        // 確保打出前數值是最新的
        UpdateStatsBasedOnRank();

        // 1. 篩選：手牌中除了自己以外的攻擊牌
        var attackCardsInHand = player.PlayerCombatState.Hand.Cards
            .Where(c => c != this && c.Type == CardType.Attack)
            .ToList();

        // 如果沒有攻擊牌，直接結束
        if (attackCardsInHand.Count == 0) return;

        // 2. 彈出選擇器
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)
        {
            RequireManualConfirmation = false, // 點擊即生效，增加流暢度
            Cancelable = false                 // 稀有牌效果通常強制執行
        };

        // 執行選擇命令
        var selectionResult = await CardSelectCmd.FromHand(
            choiceContext, 
            player, 
            prefs, 
            (card) => card != this && card.Type == CardType.Attack, 
            this
        );

        CardModel? targetCard = selectionResult?.FirstOrDefault();
        
        if (targetCard != null)
        {
            // 3. 獲取當前卡面顯示的 M 值作為實際增加的重放次數
            // 這樣可以保證「所見即所得」，避免公式不一致
            int actualEcho = (int)DynamicVars[VarEcho].BaseValue;

            // [STS2_API] 增加重放次數
            targetCard.BaseReplayCount += actualEcho;
        }
    }

    protected override void OnUpgrade()
    {
        // 升級降低能耗至 2
        EnergyCost.UpgradeBy(-1);
    }
}