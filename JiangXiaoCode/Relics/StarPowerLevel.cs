using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using BaseLib.Extensions;
using JiangXiaoMod.Code.Extensions;

namespace JiangXiaoMod.Code.Relics;

[Pool(typeof(JiangXiaoRelicPool))]
public sealed class StarPowerLevel : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    private const string VarLevel = "level";
    private const string VarEnergy = "energy";

    // protected override string IconBaseName => "star_power_level";
    private static readonly FieldInfo? DynamicVarsField = typeof(RelicModel).GetField("_dynamicVars", BindingFlags.NonPublic | BindingFlags.Instance);

    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();

    /// <summary>
    /// [STS2_API] 正確的能量修正勾子
    /// </summary>
    /// <param name="player">目前正在計算/結算能量上限的玩家對象</param>
    /// <param name="amount">修正鏈中目前的能量上限值</param>
    /// <returns>修正後的能量上限</returns>
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        // [STS2_Multiplayer_Safety] 核心修正：
        // 檢查正在結算的 player 是否就是本遺物的持有者 (this.Owner)。
        // 如果不是，則直接回傳原始數值，避免幫隊友（或其他江曉）額外增加能量。
        if (player != this.Owner)
        {
            return amount;
        }

        // 僅對持有者生效
        int bonus = GetLevel(player);
        return amount + (decimal)bonus;
    }

    /// <summary>
    /// 根據玩家的星點數計算當前星力等級
    /// </summary>
    /// <param name="specificPlayer">指定的玩家，若為 null 則自動尋找持有者或環境對象</param>
    public int GetLevel(Player? specificPlayer = null)
    {
        // 優先順序：傳入參數 > 遺物持有者 > 戰局首位玩家(預覽用)
        Player? player = specificPlayer;
        
        if (player == null && IsMutable)
        {
            // 在戰鬥或非圖鑑狀態下，嘗試獲取 Owner
            try { player = Owner; } catch { }
        }

        // 圖鑑/預覽邏輯：若仍無對象，則取當前運作狀態中的第一個玩家
        player ??= RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault();
        
        if (player == null) return 1;

        // 從目標玩家的遺物中尋找核心遺物 InnerStarMap
        // 原本需要寫 if (null) 的邏輯現在簡化為：
        var mainRelic = JiangXiaoUtils.GetStarMap(player);

        if (mainRelic != null)
        {
            // 直接訪問介面定義的屬性，不管是基礎版還是升級版都通用
            int points = mainRelic.JiangXiaoMod_SkillPoints;
            // 根據點數判定等級 (1-6)
            if (points < 5000) return 1;
            if (points < 20000) return 2;
            if (points < 30000) return 3;
            if (points < 40000) return 4;
            if (points < 45000) return 5;
        }
        return 6;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 此處呼叫時 targetPlayer 會走預覽/持有者邏輯
            int currentLevel = GetLevel();
            yield return new DynamicVar(VarLevel, (decimal)currentLevel);
            yield return new DynamicVar(VarEnergy, (decimal)currentLevel); 
        }
    }

    public override Task BeforeCombatStart()
    {
        RefreshDisplay();
        return Task.CompletedTask;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        // 重置 DynamicVar 緩存以刷新文本顯示
        DynamicVarsField?.SetValue(this, null);
    }

    public override bool ShouldReceiveCombatHooks => true;
}