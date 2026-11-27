# 更新源调整变更说明（2026-11-28）

> **分支**：`2.0-develop-preload.net`（基线：上游 `2.0-develop`）
> **范围**：本条分支上围绕「PreLoad.NET 强制更新」与「启动器自更新」的 5 个提交
> **提交**：`4c7e5409`、`8056e746`、`44ce1af9`、`04d49d4c`、`6b036644`
> **涉及文件**：15 个（9 个代码文件，其中 2 个新建；6 个 i18n 资源）

---

## 一、为什么改

1. 上游要求**更新启动器前必须先把 PreLoad.NET 补到最新**，且不允许用户配置绕过。
2. `api.github.com` 会按 IP 限流，国内还经常连不上；组件更新一旦卡在 API 上，用户既更新不了组件也更新不了启动器。
3. PreLoad.NET 已经同步发布到 CNB（`RoundStudio_cnb/preload.net-build`），国内可直连，但 CNB 没有可匿名访问的 Releases API，抓页面既慢又不稳。
4. 启动器自身的版本号、更新日志此前只有 API 一条路，API 一挂就只剩「网络错误」弹窗。

---

## 二、PreLoad.NET：版本号与下载解耦（`4c7e5409`）

把「查版本」和「下文件」拆成两条互不阻塞的链路：

| 环节 | 顺序 | 说明 |
| --- | --- | --- |
| 版本号 | GitHub API（8 秒限时）→ `PreLoad.NET-Release` main 分支 `version` | API 顺带给出资产对象；raw 文件是普通网页请求，不吃 API 配额 |
| 下载 | CNB（按 Tag）→ GitHub 资产 / latest 直链 | 已拿到版本号时按 Tag 取，保证文件与记录一致 |
| 版本号完全拿不到 | CNB latest → GitHub latest | 两条都不依赖版本号 |

- **删除 CNB 发布页抓取**：不再 GET `https://cnb.cool/RoundStudio_cnb/preload.net-build/-/releases` 并正则抓第一条 tag。每次检查少拉约 177 KB HTML，也去掉了对页面结构的依赖。
- 内部枚举 `PreLoadDownloadChannel{Cnb, GitHubRelease, GitHubLatest}` 改为 `PreLoadVersionSource{GitHubRelease, GitHubRaw}`，`GetRemoteVersionAsync()` 去掉 `includeCnb` 参数；`CheckBeforeLauncherUpdateAsync` 里原来的 `foreach (includeCnb in [true, false])` 两轮循环随之简化。
- 三个入口（启动后台检查、启动游戏守卫、更新启动器前检查）行为统一：
  - **启动后台检查**：失败只写 Console，不影响启动；
  - **启动游戏守卫**：组件缺失时阻塞下载，全失败拦截启动；
  - **更新启动器前检查**：强制，失败即中止启动器更新（无开关、不读配置）。

涉及文件：`Models/Helper/PreLoadReleaseSource.cs`、`Models/Helper/PreLoadDependencyHelper.cs`、`Views/Pages/SettingSubPage/.../UniversalSoftwareUpdate.axaml.cs`。

---

## 三、启动器自更新：API 失败改读 version 分支（`8056e746`）

上游 CI（`d556604`、`c902b0b`）在发布时把构建信息写进仓库 `version` 分支：

| 路径 | 内容 | 示例 |
| --- | --- | --- |
| `info/release` | 正式版版本号（不带 v） | `2.1.11.103` |
| `info/preview` | 预览版版本号（不带 v） | `2026.11.27.1845` |
| `info/commit` | CI 提交版版本号 | `2026.11.27.1834` |
| `changelog/<版本号>.md` | 该版本更新日志（Markdown） | `changelog/2.1.11.103.md` |

Release Tag 就是 `v` + 版本号，所以拿到版本号即可拼出资产直链，全程不需要 API。

改动要点：

- 新增 `Models/Helper/LauncherReleaseSource.cs`：`TryGetVersionAsync(UpdateType)`、`TryGetChangelogAsync(version)`、`GetDownloadUrl(version)`、`ToTag(version)`，版本号用 `^\d+(\.\d+){1,3}$` 校验。
- `CheckUpdate` 拆出 `CheckReleaseAsync`：**API 抛错且通道是正式版/预览版**时才读 version 分支；两条通道都读不到就把原异常继续抛出，界面照旧弹网络错误，**不会误报「已是最新」**。
- `UpdateCheckResult` 增加 `FallbackTag` / `FallbackBody` / `DirectDownloadUrl` 三个可选字段；弹窗标题、更新内容、任务下载分别兼容它们（`directUrl = _directUrl ?? Result?.DirectDownloadUrl`）。
- **预览版 Tag 由版本号拼出**，`LauncherDirectDownloadHelper.PreviewTag` 常量删除，`GetUrl` 改为 `ResolveUrlAsync`：预览版必须有版本号，正式版读不到时退回 `latest` 直链。预览版因此第一次有了「强制下载最新版」按钮。

---

## 四、文本请求统一走加速源（`44ce1af9`）

原先只有 PreLoad.NET 的文本请求套了 `SourceList.UpdateDownloadSources`，新写的启动器兜底只打 `raw.githubusercontent.com` 单点，GitHub 直连不通时兜底等于没有。

- 新增 `Models/Helper/GithubMirrorUrls.cs`：`Expand(url, extraPrefixes)` = 原始地址 → 专用镜像 → 统一加速源，跳过 `{route}` 模板（只适用于同域名拼接，套完整 URL 会拼出无效地址）。`PreLoadReleaseSource.GetMirroredUrls` 改为委托它，逻辑只剩一份。
- 两条链路都改成「**逐源限时**」：PreLoad 单源 5 秒 / 总 15 秒，启动器单源 4 秒 / 总 12 秒。原来共用一份预算，首个源被墙时是干等超时，后面的镜像根本没机会出手。
- **坏源不再毒死整次查询**：加速源把错误页当 200 返回是常见现象（`gh-proxy.net` 会给 195 字节 HTML）。现在每个候选都过校验，不合格换下一个：
  - 版本号：短、单行、不含 HTML；
  - 更新日志：拦 `<` 开头以及 `404` / `Not Found` 文本。

---

## 五、更新内容为空时的兜底文案（`04d49d4c`）

Release Body、`changelog/<版本号>.md`、Commit 提交信息三者全空时，弹窗里只剩标题和按钮，看起来像界面坏了。新增 `MainPage.Update.ChangelogUnavailable`，六语种齐全：

| 语种 | 文案 |
| --- | --- |
| zh_CN | 更新日志获取失败，请检查网络连接。 |
| zh_TW | 更新日誌取得失敗，請檢查網路連線。 |
| yue_HK | 更新日誌攞唔到，請檢查網絡連線。 |
| en_US | Failed to fetch the release notes. Please check your network connection. |
| ja_JP | 更新履歴を取得できませんでした。ネットワーク接続を確認してください。 |
| ru_RU | Не удалось получить список изменений. Проверьте подключение к сети. |

同一提交里顺带修掉 `zh_CN.axaml` 中 `GitHub.Error.HttpRequest` 多出来的一个双引号。

---

## 六、审阅修复（`6b036644`）

| 级别 | 问题 | 处理 |
| --- | --- | --- |
| 中 | CNB 一律打 `latest`，但记录的是 GitHub 判定出的版本号。CNB 同步慢一步时会下到上一个构建却记成新版本，之后每次比较都「已是最新」，永久停在旧组件 | 已知版本号时改打 `/-/releases/download/{tag}/PreLoad.NET.dll`，文件与记录强行对齐；Tag 取不到才退回 latest |
| 中 | 版本号文本请求共用一份 15 秒预算，首个源挂起会吞掉所有重试机会 | 单源 5 秒 + 总 15 秒双预算 |
| 中 | `IsCurrentVersion` 整体删掉字符 `0` 再 EndsWith：`2.1.11.102` 与 `2.1.11.120` 都被削成 `2.1.11.12`，把「落后一个构建」误判成「已是最新」 | 改为逐段按数字比较，Tag 段数少于程序集版本时按尾部对齐 |
| 低 | PreLoad 版本号取「第一个非空正文」，坏源返回 HTML 会让整次查询失败 | 加内容校验，不合格换源 |
| 低 | 版本号已解析、两条通道都下载失败后，兜底还会再试一次 CNB（同一个源白等一轮超时） | `DownloadRemoteAsync` / `DownloadLatestDirectAsync` 增加 `includeCnb`，兜底只补 GitHub |

---

## 七、影响面

- **无配置项变化**：不新增、不删除任何用户可配置项；「更新启动器前必须先更新 PreLoad.NET」仍是强制行为。
- **无数据迁移**：仍用 `PathsList.PreLoadVersionFile` 记录组件版本。
- **内部 API 变化**：`PreLoadDownloadChannel` → `PreLoadVersionSource`、`GetRemoteVersionAsync(bool)` → `GetRemoteVersionAsync()`、`LauncherDirectDownloadHelper.GetUrl` → `ResolveUrlAsync`。均为启动器内部调用，无外部契约。
- **网络行为**：每次启动多一次 GitHub API 请求（原来就有）+ 最多一次 raw 请求；不再有 CNB 页面抓取（−177 KB/次）。
- **平台**：仅 Windows / Linux 提供启动器自更新；其它平台兜底直接给 `null`，不会把 Windows 产物当成更新包。

---

## 八、实测记录

| 项目 | 结果 |
| --- | --- |
| `PreLoad.NET-Release` main `version` | 200，`26.11.27.1741`（13 字节） |
| CNB latest 直链 | 200，10,386,432 字节，`Content-Type: application/vnd.microsoft.portable-executable` |
| CNB 按 Tag 直链 | 200，10,386,432 字节；Range 请求返回 206，可多线程分片 |
| `BedrockBoot` `version/info/commit` | 200，`2026.11.27.1834` |
| `version/info/release`、`info/preview` | 404（尚未发布过一次，下次正式版/预览版发布后生成） |
| raw 镜像 | `github1.roundstudio.top` 200、`gh-proxy.com` 200、`gh-proxy.org` 200、`gitproxy.click` 302（自动跟随）；`gh.tianpao.top` 502 |
| 编译 | `dotnet build -c Release` 通过，0 错误 |

---

## 九、遗留项

1. `info/release`、`info/preview`、`changelog/*.md` 还没真正产生，兜底链路目前只在「读不到」这一侧验证过；上游发布一次后需要回归「读到并可下载」。
2. `PreLoadReleaseSource` 里的 `ReleaseNotesUrl`、`GetReleaseNotesUrls`、`TryGetReleaseNotesAsync` 仍是无调用方的占位，等上游给出 PreLoad.NET 的更新内容格式。
3. `PreLoadReleaseChannel.Preview` 与 `version-preview` 仍是占位，未与上游确认文件名。
4. `ru_RU.axaml` 缺 5 个 `Task.LeviLamina.Status.Download*` 键（既有问题，非本次引入）。
5. CNB 与 GitHub 的同一版本资产没有做哈希比对，目前只靠「体积 > 1 MB」拦错误页；若同步链路出现重打包，需要补强。
6. `MainPage.Update` 的 catch 里会先 `ResolveUrlAsync` 再弹窗，最坏多等 12 秒才看到错误提示；可考虑先弹窗、点击时再解析直链。
