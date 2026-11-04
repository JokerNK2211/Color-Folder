namespace ColorFolder.Core;

/// <summary>Thai and English UI text.</summary>
public static class T
{
    public static bool Thai => Settings.Language == "th";

    private static string L(string en, string th) => Thai ? th : en;

    public static string AppName => "Color Folder";
    public static string Colors => L("Colors", "สี");
    public static string Badges => L("Badges", "สัญลักษณ์");
    public static string NoBadge => L("No badge", "ไม่มีสัญลักษณ์");
    public static string ChooseIcon => L("Choose icon...", "เลือก icon...");
    public static string RestoreDefault => L("Restore default", "คืนค่าเดิม");
    public static string OpenApp => L("Open Color Folder...", "เปิด Color Folder...");
    public static string Folders => L("Folders", "โฟลเดอร์");
    public static string DropHint => L("Drag folders here, or click Add.", "ลากโฟลเดอร์มาวางที่นี่ หรือกด เพิ่ม");
    public static string Add => L("Add...", "เพิ่ม...");
    public static string Remove => L("Remove", "เอาออก");
    public static string Style => L("Icon style", "รูปแบบ icon");
    public static string StyleWindows => L("Windows", "Windows");
    public static string StyleMac => L("macOS", "macOS");
    public static string StyleTemplate => L("My template", "ต้นแบบของฉัน");
    public static string PickTemplate => L("Choose template...", "เลือกต้นแบบ...");
    public static string NoTemplate => L("No template chosen yet", "ยังไม่ได้เลือกต้นแบบ");
    public static string Apply => L("Apply", "ใช้กับโฟลเดอร์");
    public static string ImportIcon => L("Use icon file...", "ใช้ไฟล์ icon...");
    public static string ShowInMenu => L("Show in right-click menu", "แสดงในเมนูคลิกขวา");
    public static string Portable => L("Copy icon into the folder (works on other PCs)", "คัดลอก icon ไว้ในโฟลเดอร์ (ใช้ได้กับเครื่องอื่น)");
    public static string RefreshIcons => L("Refresh icons", "รีเฟรช icon");
    public static string Preview => L("Preview", "ตัวอย่าง");
    public static string Done(int n) => L($"Done: {n} folder(s) changed.", $"เสร็จแล้ว: เปลี่ยน {n} โฟลเดอร์");
    public static string Restored(int n) => L($"Restored {n} folder(s).", $"คืนค่าเดิม {n} โฟลเดอร์แล้ว");
    public static string NoFolders => L("Add at least one folder first.", "กรุณาเพิ่มโฟลเดอร์ก่อน");
    public static string Error => L("Could not change this folder", "เปลี่ยนโฟลเดอร์นี้ไม่ได้");
    public static string IconFilter => L("Icons and images", "ไฟล์ icon และรูปภาพ") + "|*.ico;*.png;*.icns;*.jpg;*.jpeg;*.bmp";
    public static string TemplateFilter => L("Folder icon", "icon โฟลเดอร์") + "|*.ico;*.png;*.icns";
    public static string ResetAllQuestion => L("Restore all folders changed by Color Folder to the normal icon?", "คืนค่า icon เดิมให้ทุกโฟลเดอร์ที่ Color Folder เปลี่ยนไว้ไหม?");
    public static string Language => Thai ? "English" : "ไทย";
}
