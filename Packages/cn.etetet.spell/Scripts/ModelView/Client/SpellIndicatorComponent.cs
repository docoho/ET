using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端技能地面指示器组件（管理指示器预制体缓存、当前激活的指示器 Transform、最大施法距离以及等待点击完成的任务源）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class SpellIndicatorComponent: Entity, IAwake, IUpdate
    {
        /// <summary>
        /// 已实例化的指示器 GameObject 缓存字典（Key 为预制体 InstanceID）。
        /// </summary>
        public Dictionary<int, GameObject> Cache = new();

        /// <summary>
        /// 当前正在地面上跟随鼠标移动的指示器 Transform。
        /// </summary>
        public Transform Current;

        /// <summary>
        /// 等待玩家左键点击确认施法位置的异步任务源。
        /// </summary>
        public ETTask<Vector3> Task;

        /// <summary>
        /// 当前技能允许的最大施法距离（毫米）。
        /// </summary>
        public int MaxDistance;
    }
}