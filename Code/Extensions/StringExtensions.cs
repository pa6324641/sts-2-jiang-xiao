namespace JiangXiaoMod.Code.Extensions;

public static class StringExtensions
{
    // 指向江曉遺物資料夾
    public static string RelicImagePath(this string name) => $"res://JiangXiao/images/relics/{name}";

    // 指向江曉卡牌資料夾
    public static string CardImagePath(this string name) => $"res://JiangXiao/images/cards/{name}";

    // 指向江曉能力圖示資料夾 (通常是 32x32 或 64x64 的 png)
    public static string PowerImagePath(this string name) => $"res://JiangXiao/images/powers/{name}";
}