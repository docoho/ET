using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 压测机器人管理组件（挂载在根 <see cref="Scene"/> 上），维护所有活动机器人账号名到其客户端纤程 ID 的映射
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class RobotManagerComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 已创建的机器人字典（Key: 机器人账号名，Value: 机器人客户端 FiberId）
        /// </summary>
        public Dictionary<string, long> robots = new();
    }
}
