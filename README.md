# Color Folder

A Windows program to change folder colors and icons from the right-click menu. Thai and English.

## Features

- Right-click a folder → **Color Folder** → pick a color, a badge, your own icon file, or restore the normal icon. Works with many folders selected.
- 16 colors × 16 badges, in two styles: **macOS** (default) and **Windows**.
- **My template**: pick any folder icon on your PC (.ico/.png/.icns) and the app recolors it into every color, keeping its exact look. These icons stay on your PC.
- Use any .ico, .png or .icns file as a folder icon.
- Optional "portable" mode copies the icon into the folder, so the color shows on other PCs (USB drives, shared folders).
- Per-user install, no admin rights. Uninstall offers to restore every folder it changed.

On Windows 11 the menu is under **Show more options** (or Shift + right-click).

## Download

Each push builds an installer on GitHub Actions: open the latest **Build** run and download the **ColorFolder-Setup** artifact.

## How it works

The app sets the folder icon with the Windows API `SHGetSetFolderCustomSettings` (the same thing Properties → Customize → Change Icon does), which writes the folder's hidden `desktop.ini`. Icons are copied to `%LOCALAPPDATA%\ColorFolder\icons` so folders keep their color after updates. The right-click menu is registered under `HKCU\Software\Classes\Directory\shell\ColorFolder`.

## Project layout

- `src/ColorFolder/` – the app (C#, .NET 8, WPF)
- `installer/` – Inno Setup script
- `icons/` – icon set, see [icons/README.md](icons/README.md)
- `tools/` – scripts that generate the icons

## Build

```
dotnet publish src/ColorFolder/ColorFolder.csproj -c Release -o publish
iscc installer\ColorFolder.iss
```
