using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端全局表现层组件（挂载在根 <see cref="Scene"/> 上）。
    /// 保存 Unity 场景中全局层级节点（如 <c>/Global</c>、<c>/Global/Unit</c>）的 <see cref="Transform"/> 引用以及全局启动配置 <see cref="GlobalConfig"/>。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class GlobalComponent: Entity, IAwake
    {
        /// <summary>Unity 场景中的 <c>/Global</c> 根 Transform。</summary>
        public Transform Global;
        /// <summary>场景中存放所有角色/单位 GameObject 的 <c>/Global/Unit</c> 父节点 Transform。</summary>
        public Transform Unit { get; set; }

        /// <summary>全局初始化配置资产引用。</summary>
        public GlobalConfig GlobalConfig { get; set; }
    }
}