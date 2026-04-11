using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands; 
using MegaCrit.Sts2.Core.Combat;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using JiangXiaoMod.Code.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JiangXiaoMod.Code.Powers;

// 繼承 CustomPowerModel 會自動處理 ID 註冊
public class DawnPower : CustomPowerModel
{
    // 這裡保留 PowerId 方便程式內引用，但 BaseLib 會自動生成 "JiangXiaoMod-DawnPower"
    public const string PowerId = "DawnPower";

    private const string VarM = "M";


    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public DawnPower() : base()
    {
        // ❌ 絕對不要在這裡寫 this.Amount = 1;
        // 這會導致遊戲啟動時崩潰
    }
    public int GetLifestealValue()
    {
        int rankLevel = 1;

        // 【STS2 獨特處理】：優先從 Owner 拿，拿不到則從全域獲取
        // 這裡套用您提供的範本邏輯
        var player = Owner?.Player ?? RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();

        if (player?.Relics != null)
        {
            var relic = player.Relics.FirstOrDefault(r => r is StarSkillQuality) as StarSkillQuality;
            if (relic != null)
            {
                // 呼叫遺物的等級獲取函數
                rankLevel = (int)relic.GetRank(); 
            }
        }

        // 實作分支邏輯：1-3抽1, 4-5抽2, 6-7抽3
        return rankLevel switch
        {
            <= 3 => 1,
            <= 5 => 2,
            <= 7 => 3,
            _ => 3 // 超過 7 級預設保持 3 張
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 將計算好的 M 值拋給 STS2 的 SmartFormat 系統渲染
            yield return new DynamicVar(VarM, (decimal)GetLifestealValue());
        }
    }

    // 玩家回合開始後的晚期執行抽牌
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        // 判定能力擁有者是否為當前玩家
        if (Owner != null && Owner.IsAlive)
        {
            // Amount 會由 PowerCmd.Apply 傳入的數值決定
            if (Amount > 0)
            {
                await CardPileCmd.Draw(choiceContext, (uint)Amount, player);
            }
        }
    }
}