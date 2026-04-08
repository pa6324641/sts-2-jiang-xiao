using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps; 
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
// 新增 STS2 的 RunManager 引用，確保 UI 能抓到資料
using MegaCrit.Sts2.Core.Runs; 

namespace JiangXiaoMod.Code.Powers;

[Pool(typeof(JiangXiaoPotionPool))] // 請確認這是設計給藥水池還是卡牌池，若是給能力使用，通常可以省略或綁定對應池
public sealed class RuinancePower : CustomPowerModel
{
    public const string PowerId = "RuinancePower";
    private const string VarResist = "resist";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomPackedIconPath => "ruinance.png"; 

    public RuinancePower() : base()
    {
        
    }
    
    public RuinancePower(int amount) : base()
    {
        this.Amount = amount;
    }

    public int GetResistValue()
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
        return 6 + (rankLevel - 1) * 6;
    }

    // 根據 CreatureCmd，BeforeDamageReceived 用於觸發視覺效果
    // 若要「修改傷害數值」，STS2 規定使用 ModifyDamage Hook。
    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 注意：這裡拿到的 amount 是已經被上面的 ModifyDamageAdditive 修改過的
        if (target == Owner && amount > 0)
        {
            Flash(); 
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 確保目標是擁有者，有傷害，且該傷害受能力影響 (非 Unpowered)
        //if (target == Owner && amount > 0 && !props.HasFlag(ValueProp.Unpowered))
        if (target == Owner && amount > 0)
        {
            // 減去化解值 (STS2 中 Additive 減少傷害要回傳負數)
            return -(decimal)GetResistValue();
        }
        return 0m;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 將計算好的值拋給 STS2 的 SmartFormat 系統渲染
            yield return new DynamicVar(VarResist, (decimal)GetResistValue());
        }
    }
}