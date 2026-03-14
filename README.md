<p align="center">
  <img src="assets/BedrockBoot.Icon.256x.png" alt="BedrockBoot Logo" width="80"/>
</p>

<h1 align="center">BedrockBoot v2</h1>

<p align="center">
  <b>Industrial Grade Minecraft Bedrock Edition Launcher for Windows</b>
  <br/>
  <b>A Bedrock Edition launcher based on BedrockLauncher.Core</b>
</p>

<div align="center">

[简体中文](/docs/README_zh.md) |
**English**

[![Release](https://img.shields.io/github/v/release/Round-Studio/BedrockBoot?logo=github&style=flat-square&color=007ec6)](https://github.com/Round-Studio/BedrockBoot/releases)
[![Stars](https://img.shields.io/github/stars/Round-Studio/BedrockBoot?logo=github&style=flat-square&color=ffd700)](https://github.com/Round-Studio/BedrockBoot/stargazers)
[![Downloads](https://img.shields.io/github/downloads/Round-Studio/BedrockBoot/total?logo=github&style=flat-square&color=44cc12)](https://github.com/Round-Studio/BedrockBoot/releases)
[![License](https://img.shields.io/badge/License-GPL%203.0-ff7a35?style=flat-square)](LICENSE)

[![Afdian](https://img.shields.io/badge/Afdian-yjq666-946ce6?style=flat-square&logo=afdian)](https://afdian.com/a/yjq666)
[![Bilibili](https://img.shields.io/badge/Bilibili-Minecraft%E4%B8%80%E8%A7%92%E9%92%B1-00A4DB?style=flat-square&logo=bilibili)](https://space.bilibili.com/1527364468)
[![Group](https://img.shields.io/badge/Group-245839607-00A4DB?style=flat-square&logo=tencent-qq)](https://qm.qq.com/q/ax057FTyl)

</div>

---

## Introduction

**BedrockBoot** is a high-performance Minecraft Bedrock Edition launcher built specifically for the Windows platform. The project aims to help users launch Minecraft Bedrock efficiently.

* **Integrated Resources**: Built-in access to **Game** and **CurseForge** downloads for a seamless experience.
* **PaperConnect**: Features the **PaperConnect** multiplayer service—an alternative to traditional Xbox Live networking for a smoother connection.
* **Advanced Management**: Supports multi-directory management, multiple instances, modpacks, resource packs, and behavior mods.

*\*PaperConnect is an Xbox Live alternative co-developed with **BMCBL**.*

### Related Links

* **Official Website**: [BedrockBoot Official Site](https://roundstudio.top/bedrockboot)
* **Documentation**: [BedrockBoot Docs](https://docs.roundstudio.top/docs/product/bb)
* **Privacy Policy**: [BedrockBoot Privacy Policy](https://docs.roundstudio.top/docs/product/bb/privacyPolicy)

---

## Download

You can obtain BedrockBoot binaries through the following official channels:

| Channel | Link |
| :--- | :--- |
| **Official Download Portal** | [Round Studio Download](https://roundstudio.top/bedrockboot) |
| **GitHub Releases** | [GitHub Release Assets](https://github.com/Round-Studio/BedrockBoot/releases) |

---

## Quick Start

1.  **Environment**: Ensure your host environment is Windows 10 (19041+) or Windows 11.
2.  **Deployment**: Download the release package to a non-system protected directory and run `BedrockBoot.exe`.
3.  **Version Management**: Navigate to the "Version Management" module to pull or link the required Bedrock Edition versions.
4.  **Launch**: Configure instance parameters and click "Launch" to enter the game.

---

## Contribution

BedrockBoot is an open-source, community-driven project. We welcome contributions in any form:

* **Feedback**: Submit bug reports or feature suggestions via [GitHub Issues](https://github.com/Round-Studio/BedrockBoot/issues).
* **Pull Requests**: Fork this repository and submit a PR. Please ensure your code passes basic unit tests before submission.
* **Tech Stack**: C# / .NET / [Avalonia](https://github.com/avaloniaui/avalonia).

---

## Build From Source

If you wish to compile or debug the project yourself:

1.  Install [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Rider](https://www.jetbrains.com/rider/) and ensure the `.NET Desktop Development` workload is installed.
2.  Ensure the **.NET 10.0 SDK** is installed.
3.  Clone and compile:
    ```bash
    git clone --recursive [https://github.com/Round-Studio/BedrockBoot.git](https://github.com/Round-Studio/BedrockBoot.git)
    cd ./BedrockBoot
    git checkout 2.0-develop
    dotnet build -c Release
    ```

---

## Team & Credits

### Core Developers
* **Lead Developers**: Dime, YoumiHa
* **UI/UX Design**: Dime, DrMing
* **Core Architecture**: YoumiHa

### Open Source Dependencies
We express our sincere gratitude to the following open-source communities:
* **Avalonia**: UI Framework
* **OnePointUI.Avalonia**: Interactive Component Library
* **BedrockLauncher.Core**: Bedrock Edition Launch Core

---

## License

This program is released under the **GPLv3** license with the following additional terms:
1.  **Modification Labeling**: When distributing modified versions, the program name or version number must be reasonably modified.
2.  **Copyright Notice**: Built-in copyright statements within the program must not be removed or obscured.

---
<p align="right"><i>Refined by Round Studio Architecture</i></p>