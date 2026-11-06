namespace ET.Server
{
    /// <summary>
    /// 服务端网络地址与端口初始化辅助类。
    /// 支持优先从环境变量（如 Aspire 容器编排注入的 <see cref="AddressSingleton"/>）读取 IP 和端口，若未注入则回退到启动配置表（<see cref="StartProcessConfig"/> / <see cref="StartSceneConfig"/>）。
    /// </summary>
    public static class AddressHelper
    {
        /// <summary>
        /// 根据当前进程配置（<paramref name="startProcessConfig"/>）回填 <see cref="AddressSingleton"/> 中尚未通过环境变量指定的内网 IP、外网 IP 和内网端口。
        /// </summary>
        /// <param name="fiber">当前纤程。</param>
        /// <param name="startProcessConfig">当前进程的启动配置。</param>
        public static void SetInnerIPInnerPortOuterIP(Fiber fiber, StartProcessConfig startProcessConfig)
        {
            AddressSingleton addressSingleton = fiber.GetSingleton<AddressSingleton>();
            addressSingleton.InnerIP ??= startProcessConfig.GetInnerIP(fiber);
            addressSingleton.OuterIP ??= startProcessConfig.GetOuterIP(fiber);
            addressSingleton.InnerPort = addressSingleton.InnerPort > 0 ? addressSingleton.InnerPort : startProcessConfig.Port;
        }
        
        // 注意一个进程中会有多个OuterPort,所以不能只使用 addressSingleton.OuterPort
        // 如果 addressSingleton.OuterPort 大于0，说明aspire有传入port，那么说明进程中只有一个Scene,并且配置了具体的port，可以直接使用
        // 否则，必须从sceneconfig中获取
        /// <summary>
        /// 获取指定场景的外网监听端口：若环境变量已显式指定 <see cref="AddressSingleton.OuterPort"/> 则优先使用，否则从 <see cref="StartSceneConfigCategory"/> 配置表中按场景名查询。
        /// </summary>
        /// <param name="fiber">当前纤程。</param>
        /// <param name="sceneName">场景名称。</param>
        /// <returns>外网监听端口号。</returns>
        public static int GetSceneOuterPort(Fiber fiber, string sceneName)
        {
            AddressSingleton addressSingleton = fiber.GetSingleton<AddressSingleton>();
            int outerPort = addressSingleton.OuterPort;

            if (outerPort > 0)
            {
                return outerPort;
            }

            StartSceneConfig startSceneConfig = fiber.GetSingleton<StartSceneConfigCategory>().GetBySceneName(sceneName);
            return startSceneConfig.Port;
        }
    }
}
