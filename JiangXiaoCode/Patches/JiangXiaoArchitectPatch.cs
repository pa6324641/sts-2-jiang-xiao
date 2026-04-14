using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using JiangXiaoMod.Code.Character;

[HarmonyPatch(typeof(TheArchitect), "DefineDialogues")]
public static class JiangXiaoArchitectPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref AncientDialogueSet __result)
    {
        // 1. 獲取角色 ID (加入空檢查，防止 ModelDb 未加載)
        var charModel = ModelDb.Character<JiangXiao>();
        if (charModel == null) return;
        string charId = charModel.Id.Entry;

        // 2. 處理 CS8604: 檢查 CharacterDialogues 是否為 null
        // 如果原版字典是空的，我們初始化一個新的字典
        IDictionary<string, IReadOnlyList<AncientDialogue>> currentDialogues = __result.CharacterDialogues;
        
        if (currentDialogues != null && currentDialogues.ContainsKey(charId))
        {
            return;
        }

        // 3. 創建對話數據
        var myDialogue = new AncientDialogue("") 
        {
            VisitIndex = 0,
            EndAttackers = ArchitectAttackers.Both 
        };

        // 4. 安全地複製字典 (解決 CS8604)
        // 如果 currentDialogues 為 null，則建立一個空字典開始
        var newDict = currentDialogues != null 
            ? new Dictionary<string, IReadOnlyList<AncientDialogue>>(currentDialogues) 
            : new Dictionary<string, IReadOnlyList<AncientDialogue>>();
            
        newDict[charId] = new List<AncientDialogue> { myDialogue }.AsReadOnly();

        // 5. 處理 CS8600: 反射字段可能為 null 的警告
        // 使用 FieldInfo? (可空類型) 並進行 null 檢查
        FieldInfo? backingField = typeof(AncientDialogueSet).GetField("<CharacterDialogues>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        
        if (backingField != null)
        {
            backingField.SetValue(__result, newDict);
        }
        else
        {
            // 備用方案：嘗試直接設定屬性
            PropertyInfo? prop = typeof(AncientDialogueSet).GetProperty(nameof(AncientDialogueSet.CharacterDialogues));
            prop?.SetValue(__result, newDict);
        }
    }
}