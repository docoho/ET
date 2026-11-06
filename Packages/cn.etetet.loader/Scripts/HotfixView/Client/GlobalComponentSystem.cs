using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端全局表现层组件系统（<see cref="GlobalComponent"/> 的逻辑扩展）。
    /// </summary>
    [EntitySystemOf(typeof(GlobalComponent))]
    public static partial class GlobalComponentSystem
    {
        /// <summary>
        /// 组件唤醒时查找并缓存 Unity 场景中的 <c>/Global</c>、<c>/Global/Unit</c> 根节点 Transform，并加载 <c>Resources/GlobalConfig</c> 配置。
        /// </summary>
        [EntitySystem]
        private static void Awake(this GlobalComponent self)
        {
            self.Global = GameObject.Find("/Global").transform;
            self.Unit = GameObject.Find("/Global/Unit").transform;
            self.GlobalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
        }
    }
}