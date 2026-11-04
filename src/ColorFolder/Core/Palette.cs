using System.Windows.Media;

namespace ColorFolder.Core;

public record FolderColor(string Id, string Hex, string En, string Th)
{
    public Color Color => (Color)ColorConverter.ConvertFromString(Hex);
}

public record Badge(string Id, string En, string Th);

/// <summary>Colors and badges. Must match tools/make_icons.py.</summary>
public static class Palette
{
    public static readonly FolderColor[] Colors =
    {
        new("red", "#E5484D", "Red", "แดง"),
        new("orange", "#F76B15", "Orange", "ส้ม"),
        new("amber", "#FFB224", "Amber", "เหลืองอำพัน"),
        new("yellow", "#F5D90A", "Yellow", "เหลือง"),
        new("lime", "#8DC63F", "Lime", "เขียวมะนาว"),
        new("green", "#30A46C", "Green", "เขียว"),
        new("teal", "#12A594", "Teal", "เขียวน้ำทะเล"),
        new("sky", "#0EA5E9", "Sky", "ฟ้า"),
        new("blue", "#3E63DD", "Blue", "น้ำเงิน"),
        new("indigo", "#5B5BD6", "Indigo", "คราม"),
        new("purple", "#8E4EC6", "Purple", "ม่วง"),
        new("pink", "#D6409F", "Pink", "ชมพู"),
        new("brown", "#A0704C", "Brown", "น้ำตาล"),
        new("gray", "#8B8D98", "Gray", "เทา"),
        new("black", "#2E2E33", "Black", "ดำ"),
        new("white", "#E8E8EC", "White", "ขาว"),
    };

    public static readonly Badge[] Badges =
    {
        new("star", "Star", "ดาว"),
        new("heart", "Heart", "หัวใจ"),
        new("check", "Check", "เครื่องหมายถูก"),
        new("cross", "Cross", "กากบาท"),
        new("warning", "Warning", "คำเตือน"),
        new("lock", "Lock", "กุญแจ"),
        new("cloud", "Cloud", "คลาวด์"),
        new("code", "Code", "โค้ด"),
        new("music", "Music", "เพลง"),
        new("image", "Image", "รูปภาพ"),
        new("play", "Video", "วิดีโอ"),
        new("download", "Download", "ดาวน์โหลด"),
        new("work", "Work", "งาน"),
        new("home", "Home", "บ้าน"),
        new("clock", "Clock", "นาฬิกา"),
        new("flag", "Flag", "ธง"),
    };

    public const string DefaultColor = "sky";

    public static FolderColor? FindColor(string? id) => Colors.FirstOrDefault(c => c.Id == id);
    public static Badge? FindBadge(string? id) => Badges.FirstOrDefault(b => b.Id == id);
}
