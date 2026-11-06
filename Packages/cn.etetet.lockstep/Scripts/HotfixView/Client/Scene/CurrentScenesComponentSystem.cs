using System;

namespace ET.Client
{
    /// <summary>
    /// 当前子场景管理组件扩展工具类
    /// </summary>
    public static partial class CurrentScenesComponentSystem
    {
        /// <summary>
        /// 获取客户端根场景下当前激活的子场景实例
        /// </summary>
        public static Scene CurrentScene(this Scene root)
        {
            return root.GetComponent<CurrentScenesComponent>()?.Scene;
        }
    }
}