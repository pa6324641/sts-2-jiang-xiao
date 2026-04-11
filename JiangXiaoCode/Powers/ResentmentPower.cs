using BaseLib.Abstracts;
using Godot;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiangXiaoMod.Code.Powers;

public class ResentmentPower : CustomPowerModel
{
    public const string PowerId = "RESENTMENT_POWER";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    
    // [STS2_Fix] 統一使用同一個 Key
    private const string _mKey = "MVar"; 

    public ResentmentPower()
    {
    }

    // [STS2_BestPractice] 僅定義結構，不要在此執行計算
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 將計算好的值拋給 STS2 的 SmartFormat 系統渲染
            yield return new DynamicVar(_mKey, (decimal)UpdatePowerStats());
        }
    }

    // 當戰鬥加載或重新讀取時，更新數值
    public override Task BeforeCombatStart()
    {
        UpdatePowerStats();
        return base.BeforeCombatStart();
    }

    // [STS2_Update] 專門負責計算數值的函數
    public int UpdatePowerStats()
    {
        int rankLevel = 1;
        
        // 【STS2 獨特處理】
        // 當能力顯示在 UI (CanonicalVars) 時，Owner 可能尚未完全綁定到 Creature 身上。
        // 因此我們優先嘗試從 Owner 拿，如果拿不到，就從全域的 RunManager 獲取當前玩家狀態。
        var player = Owner?.Player ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();

        if (player?.Relics != null)
        {
            var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            if (relic != null)
            {
                // 注意：如果你遺物裡的屬性是 SkillRank，這裡可以改為 (int)relic.SkillRank
                rankLevel = (int)relic.GetRank(); 
            }
        }
        return 30 + (rankLevel *10);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 1. 安全檢查
        if (target != Owner || dealer == null || dealer == target || result.TotalDamage < 0)
        {
            return;
        }

        // 每次觸發前，重新同步一次機率（防止戰鬥中遺物等級變動）
        UpdatePowerStats();

        // 2. 獲取當前機率
        float chance = (float)DynamicVars[_mKey].BaseValue / 100f;

        // 3. 隨機判定
        if (GD.Randf() <= chance)
        {
            await ApplyRandomDebuff(choiceContext, dealer);
        }

        await base.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource);
    }

    private async Task ApplyRandomDebuff(PlayerChoiceContext context, Creature dealer)
    {
        // 計算負面效果層數 (0星=2, 1星=4, 2星=6...)
        var player = Owner?.Player ?? RunManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault();
        var relic = player?.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
        int currentRank = relic?.SkillRank ?? 0;
        int varAmount = (currentRank + 1) * 2;

        int roll = (int)GD.RandRange(0, 8);

        switch (roll)
        {
            case 0: await PowerCmd.Apply<WeakPower>(dealer, varAmount, Owner, null); break;
            case 1: await PowerCmd.Apply<VulnerablePower>(dealer, varAmount, Owner, null); break;
            case 2: await PowerCmd.Apply<FrailPower>(dealer, varAmount, Owner, null); break;
            case 3: await PowerCmd.Apply<TheGambitPower>(dealer, 1, Owner, null); break;
            case 4: await PowerCmd.Apply<PoisonPower>(dealer, varAmount * 4, Owner, null); break; // 毒稍微給多一點
            case 5: await PowerCmd.Apply<DoomPower>(dealer, varAmount * 4, Owner, null); break;
            case 6: await PowerCmd.Apply<StrengthPower>(dealer, -varAmount,Owner, null); break;
            // case 7: await CreatureCmd.Stun(dealer); break;
            case 7: await PowerCmd.Apply<DexterityPower>(dealer, -varAmount,Owner, null); break;
            case 8: await PowerCmd.Apply<ShrinkPower>(dealer, varAmount,Owner, null); break;
        }
    }
}