using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiangXiaoMod.Code.Powers.StarMaps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Powers.StarMaps;

/// <summary>
/// 第一星圖：北斗九星
/// 效果：累計打出 9 張牌後，得到 M 能量，抽 N 張牌。
/// 公式：(PowerLevel - 1) + BaseValue
/// </summary>
public sealed class BeiDouNineStarsPower : StarMapPowerModel
{
    public override PowerStackType StackType => PowerStackType.Counter;

    // --- 基礎數值定義 ---
    private const int BaseValueGain = 1; // 根據最初需求設定為 9，也可改為 1
    private const int BaseValueDraw = 1;
    
    // 變數名稱（對應 JSON 中的 {M} 和 {N}）
    public const string VarM = "M";
    public const string VarN = "N";

    // --- 運行時數值 ---
    private int _progress = 0;
    private int _currentGain;
    private int _currentDraw;

    public override int DisplayAmount => _progress;
    private const int TriggerThreshold = 9;

    public BeiDouNineStarsPower() : base()
    {
        _progress = 0;
        // 初始計算（預防萬一）
        _currentGain = BaseValueGain;
        _currentDraw = BaseValueDraw;
    }

    /// <summary>
    /// [STS2_API] 將計算好的動態數值傳遞給在地化文本 (JSON)
    /// </summary>
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        yield return new DynamicVar(VarM, (decimal)_currentGain);
        yield return new DynamicVar(VarN, (decimal)_currentDraw);
    }

    /// <summary>
    /// [STS2_API] 根據角色的「星力等級 (PowerLevel)」動態更新數值
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        // 公式實作：(等級 - 1) + 基礎值
        _currentGain = (int)Math.Floor((decimal)(powerLevel - 1) / 3) + BaseValueGain;
        _currentDraw = (int)Math.Floor((decimal)(powerLevel - 1) / 3) + BaseValueDraw;
        
        // 數值改變時通知 UI 更新
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature == base.Owner) 
        {
            await CounterCardPlayed(context);
        }
    }

    public async Task CounterCardPlayed(PlayerChoiceContext choiceContext)
    {
        _progress++;
        
        if (_progress >= TriggerThreshold)
        {
            if (base.Owner.Player != null)
            {
                _progress = 0;
                this.Flash();
                
                // 使用動態計算後的數值 _currentGain
                await PlayerCmd.GainEnergy(_currentGain, base.Owner.Player);

                // 使用動態計算後的數值 _currentDraw
                await CardPileCmd.Draw(choiceContext, _currentDraw, base.Owner.Player);
            }
        }
        InvokeDisplayAmountChanged();
    }
}