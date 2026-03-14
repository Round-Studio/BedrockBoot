<p align="center">
  <img src="assets/BedrockBoot.Icon.256x.png" alt="BedrockBoot Logo" width="80"/>
</p>

<h1 align="center">BedrockBoot v2</h1>

<p align="center">
  <b>Industrial Grade Minecraft Bedrock Edition Launcher for Windows</b>
  <br/>
  <b>一个基于 BedrockLauncher.Core 二次开发的基岩版启动器</b>
</p>
<div align="center">

[![Release](https://img.shields.io/github/v/release/Round-Studio/BedrockBoot?logo=github&style=flat-square&color=007ec6)](https://github.com/Round-Studio/BedrockBoot/releases)
[![Stars](https://img.shields.io/github/stars/Round-Studio/BedrockBoot?logo=github&style=flat-square&color=ffd700)](https://github.com/Round-Studio/BedrockBoot/stargazers)
[![Downloads](https://img.shields.io/github/downloads/Round-Studio/BedrockBoot/total?logo=github&style=flat-square&color=44cc12)](https://github.com/Round-Studio/BedrockBoot/releases)
[![License](https://img.shields.io/badge/License-GPL%203.0-ff7a35?style=flat-square)](LICENSE)

[![Afdian](https://img.shields.io/badge/Afdian-yjq666-946ce6?style=flat-square&logo=afdian)](https://afdian.com/a/yjq666)
[![Bilibili](https://img.shields.io/badge/Bilibili-Minecraft%E4%B8%80%E8%A7%92%E9%92%B1-00A4DB?style=flat-square&logo=bilibili)](https://space.bilibili.com/1527364468)
[![Group](https://img.shields.io/badge/Group-245839607-00A4DB?style=flat-square&logo=tencent-qq)](https://qm.qq.com/q/ax057FTyl)

</div>

---

## 简介 | Introduction

**BedrockBoot** 是一款专为 Windows 平台构建的 Minecraft 基岩版高性能启动器。项目旨在帮助用户高效启动 Minecraft Bedrock。  
启动器内集成 **游戏** 与 **CurseForge** 资源下载入口，为用户提供快捷体验  
甚至还有 **PaperConnect** 联机服务，抛弃传统的 Xbox 联机，让联机体验更加舒适  
多目录管理，多实例并存，整合包、资源包、模组统统不在话下  

*\*PaperConnect 联机是与 **BMCBL** 共同开发的一款 Xbox 联机替代方案*

### 相关链接

* **官网**：[BedrockBoot 官方网站](https://roundstudio.top/bedrockboot)
* **文档**：[BedrockBoot 帮助文档](https://docs.roundstudio.top/docs/product/bb)
* **隐私策略**：[BedrockBoot 隐私策略](https://docs.roundstudio.top/docs/product/bb/privacyPolicy)

---

## 下载 | Download

你可以从以下官方渠道获取 BedrockBoot 的编译产物：

| 渠道 | 链接 |
| :--- | :--- |
| **官方下载门户** | [Round Studio Download](https://roundstudio.top/bedrockboot) |
| **GitHub Releases** | [GitHub Release Assets](https://github.com/Round-Studio/BedrockBoot/releases) |

---

## 快速开始 | Quick Start

1. **环境准备**：确保宿主环境为 Windows 10 (19041+) 或 Windows 11。
2. **初始化部署**：下载发行包至非系统保护目录，运行 `BedrockBoot.exe`。
3. **版本调度**：进入“版本管理”模块，拉取或关联所需的基岩版版本。
4. **启动执行**：配置实例参数后，点击启动即可进入游戏。

---

## 参与贡献 | Contribution

BedrockBoot 是一个开源且社区驱动的项目，欢迎任何形式的贡献：

* **反馈**：通过 [GitHub Issues](https://github.com/Round-Studio/BedrockBoot/issues) 提交缺陷报告或功能建议。
* **贡献**：Fork 本仓库并提交 Pull Request。请在提交前确保代码通过基础单元测试。
* **技术栈**：C# / C++ / .NET / [Avalonia](https://github.com/avaloniaui/avalonia)。

---

## 构建项目 | Build From Source

如果您希望自行编译或调试项目：

1. 安装 [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [Rider](https://www.jetbrains.com/rider/) 并确保以安装 `.NET 桌面开发` 负载。
2. 确保已安装 **.NET 10.0 SDK**。
3. 克隆并编译：
   ```bash
   git clone --recursive https://github.com/Round-Studio/BedrockBoot.git
   cd ./BedrockBoot
   git checkout 2.0-develop
   dotnet build -c Release
   ```

---

## 团队与致谢 | Team & Credits

### 核心开发者
- **Lead Developers**: Dime, YoumiHa
- **UI/UX Design**: Dime, DrMing
- **Core Architecture**: YoumiHa

### 开源依赖
本项目对以下开源社区的工作表示由衷感谢：
- **Avalonia**: UI 渲染框架
- **OnePointUI.Avalonia**: 交互组件库
- **BedrockLauncher.Core**: 基岩版启动核心

---

## 开源协议 | License

该程序基于 **GPLv3** 开源协议发布，并包含以下附加条款：
1. **标识修改**：分发修改版本时，必须以合理方式修改程序名称或版本号。
2. **版权声明**：不得移除或遮盖程序内置的版权声明信息。

---
<p align="right"><i>Refined by Round Studio Architecture</i></p>