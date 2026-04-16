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
using MegaCrit.Sts2.Core.Runs; // 確保引用了 Runs 以存取 RunManager.Instance
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
        if (Owner == null) return;
        var relic = Owner.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        int rank = relic?.SkillRank ?? 1;

        decimal calculatedM = rank switch
        {
            <= 3 => 1m,
            <= 5 => 2m,
            _ => 3m
        };

        if (DynamicVars.TryGetValue("M", out var mVar))
        {
            mVar.BaseValue = calculatedM;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null) return;

        UpdateStatsBasedOnRank();
        decimal energyAmount = DynamicVars.TryGetValue("M", out var mVar) ? mVar.BaseValue : 1m;

        var tokenStrengthen = CombatState.CreateCard<StrengthenToken>(Owner);
        var tokenRest = CombatState.CreateCard<RestToken>(Owner);

        // 1. 執行選擇介面並等待結果
        var selectedToken = await CardSelectCmd.FromChooseACardScreen(
            choiceContext, 
            new List<CardModel> { tokenStrengthen, tokenRest }, 
            Owner
        );

        if (selectedToken != null)
        {
            // [關鍵修正：多人模式穩定性]
            // 使用 Task.Yield() 讓出控制權，讓 Godot 有時間清理上一幀的 "Completed" 訊號。
            // 這能直接解決 "Signal 'Completed' is already connected" 的報錯問題。
            await Task.Yield();

            // 2. 為自己添加 Token
            await CardPileCmd.Add(selectedToken, PileType.Hand);

            // 3. 為所有隊友同步添加 Token
            foreach (var ally in CombatState.Allies)
            {
                if (ally != null && ally.Player != null && ally != Owner.Creature)
                {
                    var allyToken = CombatState.CreateCard(selectedToken, ally.Player);
                    await CardPileCmd.Add(allyToken, PileType.Hand);
                }
            }

            // 4. 獲得能量
            await PlayerCmd.GainEnergy(energyAmount, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-3);
    }
}