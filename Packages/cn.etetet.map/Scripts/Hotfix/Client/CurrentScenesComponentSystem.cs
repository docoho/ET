using System;

namespace ET.Client
{
    /// <summary>
    /// 客户端当前场景组件扩展方法。
    /// </summary>
    public static partial class CurrentScenesComponentSystem
    {
        /// <summary>
        /// 从客户端根场景（<paramref name="root"/>）快捷获取当前活动的子地图场景（<see cref="SceneType.Current"/>）。
        /// </summary>
        public static Scene CurrentScene(this Scene root)
        {
            return root.GetComponent<CurrentScenesComponent>()?.Scene;
        }
    }
}