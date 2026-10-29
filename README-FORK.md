# PCL 和姚修改版（heyaoLauncher）

> 本程序是**第三方**基于 [PCL CE](https://github.com/PCL-Community/PCL-CE) 源码独立制作的
> Minecraft 启动器修改版，**不是** PCL 官方或 PCL Community 的发行版本。
> Minecraft 启动器原作：龙腾猫跃（[赞助原作者](https://ifdian.net/a/LTCat)）。

- 程序名：`PCL 和姚修改版`
- 内部代号：`heyaoLauncher`
- 版本：`2.15.1-heyao.2`（基于 PCL CE 2.15.1 / dev 分支 `dc4abf8`）
- 平台：Windows 10 1809 及以上，需要 [.NET 10 Desktop Runtime](https://get.dot.net/10)
- 运行方式：单文件、免安装、便携（配置与数据保存在 exe 同级的 `PCL` 文件夹）

## 相对 PCL CE 做了哪些改动

| 位置 | 改动 |
| --- | --- |
| `metadata.json` | 名称改为 `PCL 和姚修改版`，版本后缀 `heyao.1` |
| `.csproj` | 程序集标题/产品名/公司/版权改为和姚修改版，新增 `InformationalVersion` |
| `FormMain.xaml` | 窗口标题改为 `PCL 和姚修改版`；标题栏去掉 `CE` 角标，换成「和姚修改版」文字标识 |
| `ModSetup.cs` | 强制隐藏 `CELogo` 角标并显示修改版标识 |
| `Images/icon.ico`、`icon.png` | 重新绘制的蓝底「姚」字图标（256/128/64/48/32/24/16 多尺寸） |
| `Images/Heads/Logo-Heyao.png`、`HeYao.png` | 关于页使用的修改版头像/标志 |
| `PageSetupAbout.xaml(.cs)` | 关于页第二行改为「和姚（第三方修改）」，并注明非官方；外链指向上游 PCL CE 仓库 |
| `UpdateManager.cs` | **关闭自动更新检查与社区公告**，避免被 PCL CE 官方版本覆盖 |
| `Config.cs` | 默认隐藏「设置 → 检查更新」「设置 → 反馈」「工具 → 联机大厅」等依赖 PCL CE 服务的页面 |
| `PageLaunchRight.xaml.cs` | 不再展示 PCL CE 的社区版横幅 |
| 语言文件（zh-CN / zh-TW / en-US / en-GB） | 约 560 处 `PCL CE` / `Plain Craft Launcher Community Edition` 字样改为本修改版名称，并修正由此产生的别扭文案 |
| `Resources/hints/*.txt` | 启动页随机提示同步改名，并重写涉及社区关系的几条 |
| `Modules/Minecraft/RestrictedFeaturePolicy.cs` | **解除上游针对中国大陆（及区域无法判定）的 Minecraft 下载限制** |
| `Modules/ModSelfLink.cs`、`Pages/PageTools/PageToolsSelfLink.xaml(.cs)` | 新增「工具 → 联机」：用房间号自建联机（基于 EasyTier） |

> 说明：PCL CE 的「联机大厅」依赖 PCL CE 官方服务与 Natayark 账号体系，个人修改版默认隐藏，
> 以免使用非官方客户端访问其服务。需要时可在「设置 → 启动器 → 界面隐藏」里临时取消隐藏。

## 自建联机（新功能）

「工具 → 联机」页可以不开任何账号就和好友组网：

1. 房主点「创建房间」拿到 6 位房间号；
2. 好友填同一个房间号点「加入房间」；
3. 房主在 Minecraft 里「对局域网开放」（或开服务器，端口默认 25565）；
4. 好友在「直接连接」里填 `127.0.0.1:25565`。

实现方式：房间号经 SHA-256 推导出 EasyTier 的 `network-name` / `network-secret`，房主固定虚拟 IP `10.114.51.41`，
好友用 DHCP + `--port-forward` 把远端 25565 映射到本地 25565。全程不经过 PCL CE 的大厅服务。

**关键前提：需要一个双方都能连上的节点。** 按可靠性从高到低：

| 方式 | 填什么 | 说明 |
| --- | --- | --- |
| 房主有公网 IP | `tcp://房主公网IP:11020` | 路由器把 11020/TCP+UDP 映射到房主电脑即可，无需任何服务器 |
| 自建中继 | `tcp://你的服务器IP:11010` | 服务器上跑 `easytier-core -l 11010`（EasyTier 官方发行版即可） |
| 公共节点 | 默认值 | `public.easytier.cn` / `public.easytier.top:11010`，部分网络（含实测的国内家宽）会被屏蔽 |

`EasyTier` 运行文件放在启动器同目录的 `EasyTier\easytier-windows-x86_64\` 下（含 `easytier-core.exe`、
`easytier-cli.exe`、`Packet.dll`、`WinDivert64.sys`）。缺失时页面会提示。

## 关于解除区域限制

上游 PCL CE 的 `RegionPolicy` 会对中国大陆（以及区域信号无法判定）屏蔽 Minecraft 下载、补全与整合包安装，
这是上游自己的合规选择。本修改版在 `RestrictedFeaturePolicy` 里用 `DisableRegionRestriction = true` 关掉了它，
**相关合规责任由使用者自行承担**；若要恢复，把该常量改成 `false` 重新编译即可。

## 微软登录（需要自备 Azure 客户端 ID）

上游在 CI 里通过密钥注入 Microsoft OAuth client id，本仓库没有这个密钥，所以默认用不了微软登录
（会提示 `Microsoft OAuth client id is not configured`）。自己注册一个即可，免费、不需要订阅：

1. 打开 <https://portal.azure.com> → **Microsoft Entra ID** → **应用注册** → **新注册**
2. 名称随便填（例如 `PCL-Heyao`），受支持的帐户类型选 **「仅个人 Microsoft 帐户」**
3. 注册完成后复制 **「应用程序(客户端) ID」**
4. 左侧 **「身份验证」** → 拉到最下面 → **「允许公共客户端流」** 选 **「是」** → 保存
   （设备码登录必须打开这一项，不需要配置重定向 URI）
5. 在启动器同目录的 `PCL` 文件夹里新建 `MicrosoftClientId.txt`，把客户端 ID 粘进去（一行，别带空格）

读取顺序：编译期注入的密钥 → 环境变量 `PCL_MS_CLIENT_ID` → `PCL\MicrosoftClientId.txt`。
改完重启启动器，重新登录微软账号即可。不想配置的话，用「离线登录」也能正常玩单机。

## 如何自己构建

```powershell
# 1. 安装 .NET SDK 10.0.4xx（本仓库 CI 使用 10.0.x 最新版；10.0.100 因 Roslyn/引用程序集版本过旧会编译失败）
# 2. 发布单文件
dotnet publish "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" `
  -c Release -p:Platform=x64 -p:DeleteExistingFiles=true -o .\dist --no-self-contained
# 3. 产物：dist\Plain Craft Launcher 2.exe（可自行改名，例如 PCL和姚修改版.exe）
```

本机实测环境：.NET SDK `10.0.403`，Windows 11 (10.0.26300)，x64。

> 首次构建若提示 `CS9248 分部属性必须具有实现部分` 或 `CS0012 System.Private.Windows.GdiPlus`，
> 说明 SDK 版本过旧（需要 10.0.2xx/10.0.4xx），请升级 SDK。

### 可选：注入自己的密钥

PCL CE 通过环境变量读取若干服务密钥（Microsoft 登录、CurseForge API、Sentry 等），
未注入时相关功能会不可用或降级，其余功能正常。参见 `.github/workflows/reusable-build.yml`：

```
PCL_WRITE_SECRET / PCL_MS_CLIENT_ID / PCL_CURSEFORGE_API_KEY / PCL_SENTRY_DSN / ...
```

## 再次修改时的常用位置

| 想改什么 | 改哪里 |
| --- | --- |
| 显示名称、版本号 | `Plain Craft Launcher 2/metadata.json`、`Plain Craft Launcher 2/Plain Craft Launcher 2.csproj` |
| 窗口标题、标题栏标识 | `Plain Craft Launcher 2/FormMain.xaml`（`Title`、`LabTitleFork`） |
| 图标 | 替换 `Plain Craft Launcher 2/Images/icon.ico` 与 `icon.png` |
| 关于页文案、外链 | `PCL.Core/App/Localization/Languages/*.xaml` 中的 `Setup.About.*`，以及 `Pages/PageSetup/PageSetupAbout.xaml` |
| 界面里出现的启动器名称 | `PCL.Core/App/Localization/Languages/*.xaml` 中的 `PCL CE` 字样 |
| 更新行为 | `Modules/Updates/UpdateManager.cs` |

## 许可证与合规（务必阅读）

`Plain Craft Launcher 2/` 目录沿用上游的 **《PCL 分发有限许可》**（见 `Plain Craft Launcher 2/LICENCE`），
其余目录为 Apache-2.0。基于该仓库修改属于「重度使用」，因此本修改版遵守以下要求：

1. 明确标注为**第三方基于 PCL 独立二次创作**的产物（关于页与横幅均已注明）。
2. 名称以 `Plain Craft Launcher` / `PCL` 开头并带第三方后缀（本作：`PCL 和姚修改版`）。
3. 关于页首位保留**龙腾猫跃**署名与赞助链接，并**公开源代码**、保留该许可文件。
4. 保留 Minecraft 正版购买劝导逻辑（`McLaunchPrecheck` 未被削弱）。
5. 界面主色相保持蓝色，不实现与「赞助解锁」类似的机制。

**如果要分发给别人**：请把本仓库（含你的修改）公开，并保留上述署名与许可文件；
不要使用会暗示「官方出品」的名称（如 `PCL Pro`、`PCL 3`、`PCL 手机版` 等），也不要收费分发。

## 免责声明

本项目与 Mojang Studios、Microsoft、龙腾猫跃、PCL Community 均无隶属关系。
Minecraft 是 Mojang Studios 的商标。请通过官方渠道购买正版游戏。
