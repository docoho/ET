using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 服务端启动配置表名称集合辅助类。
    /// </summary>
    public static class StartConfigHelper
    {
        /// <summary>
        /// 属于服务端拓扑启动配置的配表 Category 名称列表（这些配表按启动配置目录单独加载）。
        /// </summary>
        [StaticField]
        public static List<string> StartConfigs = new List<string>()
        {
            "StartMachineConfigCategory",
            "StartProcessConfigCategory",
            "StartSceneConfigCategory",
            "StartZoneConfigCategory",
        };
    }
}