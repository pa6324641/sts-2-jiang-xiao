using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Powers.StarMaps;

// --- 第七星圖：聖十字架 ---
public sealed class HolyCrossPower : StarMapPowerModel
{
    public override string CustomPackedIconPath => "Test.png".PowerImagePath();
    // 效果：免疫50%傷害
    public HolyCrossPower() : base() { }

    /// <summary>
    /// [STS2_API] 動態變數註冊
    /// </summary>
    protected override IEnumerable<DynamicVar> GetCustomVars()
    {
        // 將數值 50 註冊為動態變數 "M"
        // 這樣在 Localization_V3 的 JSON 文本中，只需寫上 "減少 {M}% 傷害" 即可動態讀取
        yield return new DynamicVar("M", 50);
    }

    /// <summary>
    /// [STS2_API] 星力等級更新邏輯
    /// </summary>
    protected override void ApplyRankLogic(Player? player, int powerLevel)
    {
        // 預留空間：若未來高等級星圖會影響減傷比例，可在此處理
    }

    /// <summary>
    /// [STS2_API] 受到傷害前觸發flash
    /// </summary>
    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 集中在此處處理視覺回饋，避免多重 Hook 導致圖標重複閃爍
        if (this.Owner != null && target == this.Owner)
        {
            this.Flash();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// [STS2_API] 以乘法修改受到的傷害
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 確保觸發減傷的是能力持有者本人
        if (this.Owner != null && target == this.Owner)
        {
            // 減傷 50%，因此將原始傷害乘以 0.5m 後回傳
            // 註：若未來需要根據星力等級改變減傷比例，這裡的 0.5m 也需改為動態讀取變數
            return 0.5m;
        }

        // 若不是目標本人，則回傳基礎邏輯（預設不變動）
        return base.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource);
    }
}