# macOS 本地客户端验证

1. 修复前复现：Mac.app 启动日志持续出现 Insecure connection not allowed，无法显示登录页。
2. 确认 GlobalConfig 为 Client / StateSync，登录地址与运行中服务端的实际监听端口一致（进程编号不是端口）。
3. 资源模式为 OfflinePlayMode 时，启动使用 StreamingAssets 中的 DefaultPackage，无需远端 CDN。
4. Build Tool 能正确加载 GlobalConfig 和 YooConfig；构建使用 Build Settings 中启用且存在的场景。
5. macOS 构建生成 Release/ET.app；构建报告必须成功，应用启动后显示登录页且没有启动异常。
6. 输入测试账号登录本机服务端，确认进入 StateSync 地图，客户端与服务端日志无登录或场景切换错误。
7. CodeMode 切换后重新构建资源包，确认不会加载旧的 ClientServer DLL；启动不得出现 MongoDB.Driver.MongoClient TypeLoadException。

## 2026-10-25 验证结果

- `dotnet build ET.sln` 通过：0 错误，9 个程序集引用警告。
- HybridCLR 裁剪清单已更新，四个热更新程序集及 PDB 已覆盖复制到资源源目录，保留原有文件和 `.meta`。
- 内置资源版本为 `2026-10-25-232348`；资源构建与 macOS 应用构建均成功。
- `Release/ET.app` 已实际显示登录页，使用本地测试账号登录，经过大厅进入地图，角色及游戏界面正常显示。
- 服务端进程编号为 `28657`，登录路由实际监听 `127.0.0.1:10101`，接口返回本地 Realm 和 Router 地址。
- 最终客户端日志无此前的 HTTP 禁止或 MongoClient 加载异常；地图资源仍有缺失脚本警告，未阻止本次进入地图。

## 2026-10-26 iOS 真机验证

- 环境：Unity 2022.3.62f3、Xcode 27.1、iPhone 16 Pro Max / iOS 27.0.1。
- iOS 补齐 `ET10`、`YIUI`、`INITED` 编译标记；最低 iOS 版本调整为当前 Xcode 支持的 15.0。Odin 自动切换 iOS 的 NoEmit 程序集导入配置。
- `ET/Loader/Build iOS` 导出 `Release/iOS/ET/Unity-iPhone.xcodeproj`；需先切换到 iOS，重新生成该平台的 HybridCLR DLL 和内置资源。
- iOS 热更新 DLL/PDB 共 8 个、补充元数据 DLL 共 10 个，复制后哈希一致；内置资源版本为 `2026-10-25-235412`。
- 修复首版启动的 `UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption` / SIGTRAP：构建后处理添加场景声明和原生场景委托，转发现有 Unity 前后台回调；连续执行两次后处理通过。
- `dotnet build ET.sln` 通过：0 错误、9 个程序集引用警告；修复版 Xcode 原生构建为 `BUILD SUCCEEDED`。
- 已验证真机安装、启动、登录和进入地图，角色与游戏界面正常显示；用户已确认 iPhone 正常运行。日志保留早期局域网连接失败、会话断开及既有地图缺失脚本记录，本次未扩展到所有玩法或长期后台联网验证。
- 最终 `Release/iOS/ET.ipa` 压缩包完整性、解包签名校验通过；主程序与 UnityFramework 的哈希与真机安装的修复版一致。
- 本次包连接 Mac 局域网测试服务器 `192.168.31.100:10101`；源 `GlobalConfig.Address` 已恢复构建前值。开发描述文件覆盖当前测试 iPhone，有效期至台北时间 `2026-11-02 00:15:12`。
- 详细产物、日志及截图见 `Release/iOS-Validation-20261025/验证报告.md`。iOS 27 生命周期要求参考 [Apple 官方说明](https://developer.apple.com/documentation/uikit/transitioning-to-the-uikit-scene-based-life-cycle) 与 [Unity 官方说明](https://discussions.unity.com/t/info-apple-update-your-editor-to-receive-uiscene-lifecycle-support/1709065)。
