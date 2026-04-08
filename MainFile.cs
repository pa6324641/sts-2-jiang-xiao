using System.Reflection;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace JiangXiaoMod; // 建議改回你的 Mod 命名空間

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
	// 這裡的 ModId 非常重要，必須與你的 PCK 根目錄資料夾名稱一致
	public const string ModId = "JiangXiao"; 

	public static Logger Logger { get; } = new(ModId, LogType.Generic);

	public static void Initialize()
	{
		Logger.Info("江曉模組正在初始化...");
		
		Harmony harmony = new(ModId);
		// 獲取當前 DLL
		var assembly = Assembly.GetExecutingAssembly();
		
		// 只保留這一行，且確保它只執行一次
		//ScriptManagerBridge.LookupScriptsInAssembly(assembly);
		
		harmony.PatchAll();
		Logger.Info("江曉模組初始化完成！");
	}
}
