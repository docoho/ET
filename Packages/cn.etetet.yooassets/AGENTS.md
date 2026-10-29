# cn.etetet.yooassets

## 目录与配置

- `Runtime/ET` 为 ET 资源加载适配；`Resources/YooConfig.asset` 为资源加载模式配置。
- 本地打包使用 `OfflinePlayMode` 加载 `StreamingAssets/Bundles` 中随包分发的资源；此模式只控制资源来源，游戏服务端地址由 `GlobalConfig.Address` 控制。
- `HostPlayMode` 需要可用的资源服务器及匹配的版本清单；使用 HTTP 时还需匹配 Unity Player 的 HTTP 策略。
- 修改运行时代码按 harness 的 `et-code`、`et-async` 规范；不要手工生成 `.meta` 或修改 `.csproj`。
