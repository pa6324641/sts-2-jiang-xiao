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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs; // 必須引用以獲取全局狀態

namespace JiangXiaoMod.Code.Powers;

public sealed class RuinancePower : CustomPowerModel
{
    private const string VarResist = "resist";

    public override PowerType Type => PowerType.Buff;
    // 多人模式建議保持 StackType.None 或按需求調整，但 constructor 必須受控
    public override PowerStackType StackType => PowerStackType.Counter; 
    public override string CustomPackedIconPath => "ruinance.png"; 

    // 必須保留無參數構造函數供系統註冊用
    public RuinancePower() : base() { }
    
    // 用於戰鬥中實例化的構造函數
    public RuinancePower(int amount) : base()
    {
        this.Amount = amount;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner != null && target == Owner && amount > 0)
        {
            return -(decimal)this.Amount;
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

    // [STS2 重要修正]：CanonicalVars 用於 UI 顯示
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            decimal displayValue = (decimal)this.Amount;

            // 如果 Amount 為 0 (代表這是 ModelDb 裡的預覽單例)，嘗試即時計算預覽值
            if (displayValue == 0)
            {
                displayValue = CalculatePreviewResist();
            }

            yield return new DynamicVar(VarResist, displayValue);
        }
    }

    // 專門為 UI 預覽設計的數值計算
    private decimal CalculatePreviewResist()
    {
        // 嘗試從全域 RunManager 抓取玩家當前的遺物狀態進行預覽
        var player = RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();
        if (player == null) return 6m; // 預設基礎值

        // 這裡套用你的遺物邏輯，例如：
        // var relic = player.Relics.OfType<StarSkillQuality>().FirstOrDefault();
        // int rank = relic?.GetRank() ?? 1;
        // return 6 + (rank - 1) * 6;
        
        return 6m; 
    }
}