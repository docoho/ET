using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端 Buff 关联的特效 GameObject 容器组件（随 Buff 生命周期一起创建与销毁）。
    /// </summary>
    [ComponentOf(typeof(Buff))]
    public class BuffGameObjectComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 该 Buff 在客户端实例化的所有特效 GameObject 列表。
        /// </summary>
        public List<GameObject> GameObjects { get; set; } = new();
    }
}