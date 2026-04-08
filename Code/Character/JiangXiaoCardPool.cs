using BaseLib.Abstracts;
using Godot;
using JiangXiaoMod.Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace JiangXiaoMod.Code.Character;


// 卡池定義
public sealed class JiangXiaoCardPool : CustomCardPoolModel
{
	public override string Title => JiangXiao.CharacterId;
	// 解決 CS0534：實作缺失的抽象成員
	// 如果這是角色的專屬職業卡池，請回傳 false
	public override bool IsColorless => false;

	// 設定為黑紅類型
	public override float H => 0.0f; 
	public override float S => 0.9f; 
	public override float V => 0.4f; 
	public override Color DeckEntryCardColor => JiangXiao.Color;

	
	public override string? BigEnergyIconPath => "res://JiangXiao/images/ui/combat/JiangXiao_energy_icon_1.png";	
	public override string? TextEnergyIconPath => "res://JiangXiao/images/ui/combat/text_JiangXiao_energy_icon.png";
}
