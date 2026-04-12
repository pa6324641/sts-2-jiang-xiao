using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps; 
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs; 
using JiangXiaoMod.Code.Extensions; 
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JiangXiaoMod.Code.Powers;

public sealed class RuinancePower : CustomPowerModel
{
    // [修正] 定義 ID 常數
    // public const string PowerId = "JIANGXIAOMOD-RUINANCE_POWER"; 
    
    // // [核心修正] 使用 override 屬性來提供 ID，解決 CS0200 錯誤
    // public new string Id => PowerId;

    private const string VarResist = "resist";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override string CustomPackedIconPath => "ruinance.png"; 

    // 構造函數保持簡潔，不再對 Id 進行賦值
    public RuinancePower() : base()
    {
    }
    
    public RuinancePower(int amount) : base()
    {
        this.Amount = amount;
    }

    public int GetResistValue()
    {
        // 安全獲取當前玩家實例
        var player = Owner?.Player ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();
        // 調用你工具類中的邏輯
        int rankLevel = JiangXiaoUtils.GetSkillRank(player);
        return 3 + (rankLevel - 1) * 3;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 傷害減免邏輯
        if (Owner != null && target == Owner && amount > 0)
        {
            return -(decimal)GetResistValue();
        }
        return 0m;
    }

    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner != null && target == Owner && amount > 0)
        {
            Flash(); 
        }
        return Task.CompletedTask;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 將計算出的化解值傳遞給本地化文本中的 {resist}
            yield return new DynamicVar(VarResist, (decimal)GetResistValue());
        }
    }
}