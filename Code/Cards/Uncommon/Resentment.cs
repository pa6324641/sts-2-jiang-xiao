using BaseLib.Utils;
using JiangXiaoMod.Code.Cards.CardModels;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Powers;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars; // 確認此命名空間正確
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs; // 引入此項以支持圖鑑中的 Owner 獲取
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiangXiaoMod.Code.Cards.Rare;

[Pool(typeof(JiangXiaoCardPool))]
public class Resentment : JiangXiaoCardModel
{
    public const string CardId = "Resentment";
    private const string _mKey = "MVar";

    public Resentment() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        // 構造函數保持簡潔
    }

    // [STS2_Update] 變量定義應保持穩定，數值給予初始默認值
    protected override IEnumerable<DynamicVar> CanonicalVars => [ 
        new DynamicVar(_mKey, 30m) 
    ];

    // [STS2_BestPractice] 這裡是更新動態數值的主戰場
    public void UpdateStatsBasedOnRank()
    {
        // 獲取當前 Player (處理圖鑑中的 null 情況)
        var player = Owner ?? RunManager.Instance?.DebugOnlyGetState()?.Players?.FirstOrDefault();
        
        int currentRank = 0;
        if (player != null)
        {
            var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            currentRank = relic?.SkillRank ?? 0;
        }

        // 計算並更新 DynamicVars 中的數值
        decimal displayChance = 30 + (currentRank * 10);
        DynamicVars[_mKey].BaseValue = displayChance;
    }

    public override Task BeforeCombatStart()
    {
        UpdateStatsBasedOnRank();
        return base.BeforeCombatStart();
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 在打出前強制更新一次數值，確保萬無一失
        UpdateStatsBasedOnRank();

        // 施加能力
        if (Owner?.Creature != null)
        {
            await PowerCmd.Apply<ResentmentPower>(Owner.Creature, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 0 費
        EnergyCost.UpgradeBy(-1);
    }
}