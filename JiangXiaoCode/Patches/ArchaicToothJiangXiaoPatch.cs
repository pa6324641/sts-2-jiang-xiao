using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JiangXiaoMod.Code.Cards.Ancient;
using JiangXiaoMod.Code.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace JiangXiaoMod.Code.Patches;

/// <summary>
/// 處理古代牙齒在江曉角色下的預覽邏輯
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer))]
public static class ArchaicToothJiangXiaoPatch
{
    // 江曉初始打擊的 ID
    private const string JiangXiaoStrikeId = "JIANGXIAOMOD-STRIKE_JIANG_XIAO";

    [HarmonyPrefix]
    public static bool SetupForPlayerPrefix(ArchaicTooth __instance, Player player, ref bool __result)
    {
        // 1. 安全檢查：確保 RunState 可用
        if (player.RunState == null) return true;

        // 2. 尋找牌組中是否有江曉的專屬初始卡
        // 優先匹配 ID，這是最準確的做法
        var starter = player.Deck.Cards.FirstOrDefault(c => c.Id.Entry == JiangXiaoStrikeId);

        // 如果找不到江曉的初始卡，則回傳 true 執行原版邏輯（相容其他角色或特殊情況）
        if (starter == null)
        {
            return true;
        }

        // 3. 準備預覽目標：神:技藝打擊
        // [STS2 API] 透過 RunState 創建卡牌實體
        var ancient = player.RunState.CreateCard<GodSkillStrike>(player);

        // 4. 處理升級繼承
        // 如果玩家目前的初始卡已經升級，預覽顯示的古代卡也應該是升級後的版本
        if (starter.IsUpgraded)
        {
            CardCmd.Upgrade(ancient);
        }

        // 5. 設定遺物預覽數據
        // SetupForTests 是 STS2 用來綁定「轉換前」與「轉換後」卡牌數據的核心方法
        // ToSerializable() 會將 Model 轉換為存檔/顯示用的序列化格式
        __instance.SetupForTests(starter.ToSerializable(), ancient.ToSerializable());

        // 6. 成功攔截
        // 將結果設為 true 表示此遺物「已就緒」，並回傳 false 阻止原版隨機尋找打擊/防禦的邏輯
        __result = true;
        
        // 加入 Log 方便調試
        // MainFile.Logger.Info($"[ArchaicTooth] 已成功為江曉設定預覽：{starter.Id.Entry} -> {ancient.Id.Entry}");
        
        return false;
    }
}