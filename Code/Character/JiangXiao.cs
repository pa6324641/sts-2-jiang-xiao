using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using JiangXiaoMod.Code.Cards.Basic; 
using JiangXiaoMod.Code.Relics;
using System.Linq;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes;
using BaseLib.Utils;
using JiangXiaoMod.Code.Character;
using JiangXiaoMod.Code.Cards.Rare;
using JiangXiaoMod.Code.Cards.Common;
using JiangXiaoMod.Code.Cards.Uncommon;

namespace JiangXiaoMod.Code.Character;

[Pool(typeof(JiangXiaoCardPool))]
public class JiangXiao : CustomCharacterModel
{
	public const string CharacterId = "JiangXiao";
	
	public JiangXiao() : base()
	{
		// 這是 BaseLib 註冊模型的標準方式
		//BaseLib.Patches.Content.CustomContentDictionary.AddModel(GetType());
	}

	// 角色代表顏色：極深紅
	public static readonly Color Color = new(0.2f, 0.0f, 0.0f);
	public override Color MapDrawingColor => Color;
	public override Color NameColor => Color;

	// 資源路徑配置
	//上方UI頭像
	public override string CustomIconTexturePath => "res://JiangXiao/images/JiangXiao/character_icon_JiangXiao2.png";
	//選擇人物
	public override string CustomCharacterSelectIconPath => "res://JiangXiao/images/JiangXiao/char_select_JiangXiao.png";
	//選擇人未解鎖
	public override string CustomCharacterSelectLockedIconPath => "res://JiangXiao/images/JiangXiao/char_select_JiangXiao_locked.png";
	//人物
	public override string CustomVisualPath => "res://JiangXiao/scenes/JiangXiao/jiangxiao.tscn";
	//選擇大背景
	public override string CustomCharacterSelectBg => "res://JiangXiao/scenes/JiangXiao/char_select_bg_jiang_xiao.tscn";
	//轉場
	public override string CustomCharacterSelectTransitionPath => "res://JiangXiao/images/JiangXiao/transitions/jiangxiao_transition_mat.tres";
	//能量動畫
	public override string CustomEnergyCounterPath => "res://JiangXiao/scenes/JiangXiao/jiangxiao_energy_counter_empty.tscn";
	public override CustomEnergyCounter? CustomEnergyCounter =>
    new CustomEnergyCounter(
        (i) => "res://JiangXiao/images/ui/combat/JiangXiao_energy_icon_0.png", // 先固定路徑測試
        new Color(0.1f, 0, 0),
        new Color(0.8f, 0, 0)
    );
	public override string CustomTrailPath => "res://JiangXiao/scenes/JiangXiao/card_trail_jiangxiao.tscn";
	//頭像
	public override string CustomIconPath => "res://JiangXiao/scenes/JiangXiao/jiang_xiao_icon.tscn";
	//先用觀者
	public override string CustomRestSiteAnimPath => "res://JiangXiao/scenes/JiangXiao/jiangxiao_rest_site.tscn";
	//先用觀者
	public override string CustomMerchantAnimPath => "res://JiangXiao/scenes/JiangXiao/jiangxiao_merchant.tscn";
	public override string CustomMapMarkerPath => "res://JiangXiao/images/JiangXiao/map_marker_JiangXiao.png";

	public override CharacterGender Gender => CharacterGender.Masculine;
	public override int StartingHp => 75; 
	public override int StartingGold => 99;

	// 初始卡組
	public override IEnumerable<CardModel> StartingDeck => [
		ModelDb.Card<StrikeJiangXiao>(), 
		ModelDb.Card<StrikeJiangXiao>(), 
		ModelDb.Card<StrikeJiangXiao>(),
		ModelDb.Card<StrikeJiangXiao>(),
		ModelDb.Card<DefendJiangXiao>(),
		ModelDb.Card<DefendJiangXiao>(),
		ModelDb.Card<DefendJiangXiao>(),
		ModelDb.Card<Blessing>(),
		ModelDb.Card<SecondaryFace>(),
		ModelDb.Card<Ruinance>()
	];

	// 初始遺物
	public override IReadOnlyList<RelicModel> StartingRelics => [
		ModelDb.Relic<InnerStarMap>() ,
		ModelDb.Relic<StarPowerLevel>() ,
		ModelDb.Relic<StarSkillQuality>() 
	];

	private string JiangXiaoEnergyPaths(int i)
	{
		int iconIndex = 0;

		// STS2 提醒：在選單畫面時，RunManager 可能尚未初始化
		var runState = RunManager.Instance?.DebugOnlyGetState();
		
		if (runState != null)
		{
			var player = runState.Players.FirstOrDefault();
			if (player != null)
			{
				// 這裡假設你有一個 StarPowerLevel 遺物來控制圖標
				var levelRelic = player.Relics.FirstOrDefault(r => r is StarPowerLevel) as StarPowerLevel;
				int level = levelRelic?.GetLevel() ?? 1;
				if (level >= 4 && level <= 5) iconIndex = 1;
				else if (level >= 6) iconIndex = 2;
			}
		}

		return $"res://JiangXiao/images/ui/combat/JiangXiao_energy_icon_{iconIndex}.png";
	}

	// 正確的池指定方式：確保這些池類 (JiangXiaoCardPool 等) 已經正確定義
	public override CardPoolModel CardPool => ModelDb.CardPool<JiangXiaoCardPool>();
	public override RelicPoolModel RelicPool => ModelDb.RelicPool<JiangXiaoRelicPool>();
	public override PotionPoolModel PotionPool => ModelDb.PotionPool<JiangXiaoPotionPool>();

	public override List<string> GetArchitectAttackVfx()
	{
		return [
			"vfx/vfx_attack_blunt",
			"vfx/vfx_heavy_blunt",
			"vfx/vfx_attack_slash",
			"vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
		];
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		var animState = new AnimState("Idle", true);
		var animator = new CreatureAnimator(animState, controller);
		animator.AddAnyState("Idle", animState);
		animator.AddAnyState("Hit", new AnimState("Hit") { NextState = animState });
		animator.AddAnyState("Attack", new AnimState("Attack") { NextState = animState });
		return animator;
	}
}
