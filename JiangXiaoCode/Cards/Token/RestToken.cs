using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class RestToken : JiangXiaoCardModel
{
    public const string CardId = "RestToken";

    public RestToken() : base(2, CardType.Skill, CardRarity.Token, TargetType.None)
    {
    }

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 確保 Owner 及其戰鬥實體存在
        if (Owner?.Creature == null) return;

        // 1. 計算治療量 (最大生命值的 20%)
        int healAmount = (int)Math.Ceiling(Owner.Creature.MaxHp * 0.2f);

        // 2. 執行治療
        // [修正 CS1503] 
        // 參數 1: 目標 Creature
        // 參數 2: 治療數值 (int 或 decimal)
        // 參數 3: 是否顯示特效 (bool) -> 根據報錯訊息，這裡應傳入 bool
        await CreatureCmd.Heal(Owner.Creature, healAmount, true);
    }
}