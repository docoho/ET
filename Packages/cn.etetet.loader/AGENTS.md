# cn.etetet.loader

## 概述

加载器包，负责 `ET.Loader` 的 DotNet 入口、加载流程和相关运行时代码。

## 详细文档

- **编译构建**：请查看 `/et-build` skill
- **架构规范**：请查看 `/et-code` skill

## 核心目录

| 路径 | 说明 |
|------|------|
| `DotNet~` | `ET.Loader.csproj` 与相关生成脚本 |
| `Scripts` | Loader 层共享与服务端代码 |
| `CodeMode` | Loader 的 CodeMode 扩展代码 |

## 客户端本地打包

- `ET/Loader/Build macOS` 使用 Build Settings 中启用的场景，输出 `Release/ET.app`；打包要求 `GlobalConfig.CodeMode` 为 `Client`。
- `GlobalConfig` 和 `YooConfig` 通过 `Resources.Load` 获取，避免初始化包迁移后继续引用旧资源路径。
- 本地验证使用 `YooConfig.EPlayMode = OfflinePlayMode` 从包内加载资源；游戏服务器地址仍由 `GlobalConfig.Address` 指定，资源离线模式不影响游戏联网。
- 验证步骤见 `Test.md`，必须验证实际应用启动、登录和进入地图。
- 切换 CodeMode 或重新生成热更新 DLL 后，先生成 HybridCLR 裁剪清单，再执行 `ET/Loader/Build Bundles (Keep Existing Files)` 更新内置资源，最后打包应用；旧资源包中的 ClientServer 程序集会导致客户端加载已裁剪的服务端类型失败。
- 资源构建入口沿用收集配置、压缩和加密服务配置，使用新版本目录、关闭缓存清理并采用 `OnlyCopyAll`，保留已有文件。
- 使用 `HybridCLR/Generate/LinkXml` 生成清单后，将当前平台 `HybridCLRData/HotUpdateDlls` 下的 `ET.Model`、`ET.ModelView`、`ET.Hotfix`、`ET.HotfixView` 的 DLL/PDB 覆盖复制到 `Bundles/Code` 对应 `.bytes` 文件，避免内置资源继续收集旧程序集；保留原 `.meta`，无需清空目录。
