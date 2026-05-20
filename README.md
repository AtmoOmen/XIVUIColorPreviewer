# XIV UI Color Previewer

一款用于预览《最终幻想 14》游戏内 UIColor 套用至文字效果的 Windows 桌面工具

![截图](https://raw.githubusercontent.com/AtmoOmen/XIVUIColorPreviewer/master/Resources/Preview-1.png)

## 功能

- 实时预览游戏 UI 配色效果
- 基于游戏内 UIColor 数据表
- 支持自定义颜色调整

## 环境要求

- Windows 10 1709 (Build 17763) 或更高版本
- .NET 10
- Windows App SDK 2.0

## 自构建

```powershell
dotnet publish XIVUIColorPreviewer\XIVUIColorPreviewer.csproj -c Release -p:Platform=x64 -p:PublishProfile=win-x64 -p:WindowsPackageType=None
```

构建产物位于 `XIVUIColorPreviewer\bin\x64\Release\net10.0-windows10.0.19041.0\win-x64\publish\` 目录下。