using BaseLib.Abstracts;
using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Cards.Token;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class ShadowOfVoid : JiangXiaoCardModel
{
    public const string CardId = "ShadowOfVoid";

    public ShadowOfVoid() : base(5, CardType.Skill, CardRarity.Rare, TargetType.None)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("M", 1m) 
    ];

    public void UpdateStatsBasedOnRank()
    {
        var relic = Owner?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        int rank = relic?.SkillRank ?? 1;

        decimal calculatedM = rank switch
        {
            <= 3 => 1m,
            <= 5 => 2m,
            _ => 3m
        };

        // [修正 CS8602] 使用 TryGetValue 確保安全存取
        if (DynamicVars.TryGetValue("M", out var mVar))
        {
            mVar.BaseValue = calculatedM;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null) return;

        UpdateStatsBasedOnRank();
        
        // [修正 CS8602] 獲取能量數值
        decimal energyAmount = DynamicVars.TryGetValue("M", out var mVar) ? mVar.BaseValue : 1m;

        var tokenStrengthen = CombatState.CreateCard<StrengthenToken>(Owner);
        var tokenRest = CombatState.CreateCard<RestToken>(Owner);

        // 觸發選擇界面
        var selectedToken = await CardSelectCmd.FromChooseACardScreen(
            choiceContext, 
            new List<CardModel> { tokenStrengthen, tokenRest }, 
            Owner
        );

        if (selectedToken != null)
        {
            // [修正 CS1503] CardPileCmd.Add 第三個參數是 CardPilePosition 而非 Player。
            // 既然卡片在 Create 時已指定 Owner，直接 Add 即可。
            await CardPileCmd.Add(selectedToken, PileType.Hand);

            // 同步給隊友
            foreach (var ally in CombatState.Allies)
            {
                // ally 是 Creature, Owner 是 Player。需要比較實體：ally != Owner.Creature
                if (ally != null && ally != Owner.Creature && ally.Player != null)
                {
                    // [修正 CS1503] CreateCard 的第一個參數應傳入 CardModel (作為模板)，而非 ModelId
                    var allyToken = CombatState.CreateCard(selectedToken, ally.Player);
                    await CardPileCmd.Add(allyToken, PileType.Hand);
                }
            }
        }

        // [修正 CS1503] GainEnergy 參數順序為 (decimal amount, Player player)
        await PlayerCmd.GainEnergy(energyAmount, Owner);
    }

    protected override void OnUpgrade()
    {
        // 若需升級效果可在此添加
        EnergyCost.UpgradeBy(-1);
    }
}