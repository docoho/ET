using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 战斗飘字（伤害/治疗跳字）管理组件（挂载在客户端 <see cref="Scene"/> 上），维护飘字专用 Overlay 画布以及活动节点列表与对象池队列
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class CombatTextComponent : Entity, IAwake, IDestroy, ILateUpdate
    {
        /// <summary>
        /// 战斗飘字运行时常驻根 GameObject
        /// </summary>
        public GameObject RootGameObject;

        /// <summary>
        /// 战斗飘字顶层 Overlay Canvas
        /// </summary>
        public Canvas Canvas;

        /// <summary>
        /// 画布的 RectTransform 引用
        /// </summary>
        public RectTransform CanvasRect;

        /// <summary>
        /// 当前正在播放动画的活动飘字节点列表
        /// </summary>
        public readonly List<CombatTextNodeData> ActiveNodes = new();

        /// <summary>
        /// 已播放完毕待复用的飘字节点对象池队列
        /// </summary>
        public readonly Queue<CombatTextNodeData> CachedNodes = new();
    }

    /// <summary>
    /// 单个战斗飘字节点的运行时数据与 UI 引用结构体
    /// </summary>
    public struct CombatTextNodeData
    {
        /// <summary>
        /// 飘字节点 GameObject
        /// </summary>
        public GameObject GameObject;

        /// <summary>
        /// 飘字节点 RectTransform
        /// </summary>
        public RectTransform RectTransform;

        /// <summary>
        /// TextMeshPro 文本渲染组件
        /// </summary>
        public TextMeshProUGUI Text;

        /// <summary>
        /// 当前屏幕空间移动速度向量（像素/秒）
        /// </summary>
        public Vector2 Velocity;

        /// <summary>
        /// 飘字总生命周期时长（秒）
        /// </summary>
        public float Lifetime;

        /// <summary>
        /// 飘字已存活时间（秒）
        /// </summary>
        public float Elapsed;

        /// <summary>
        /// 初始缩放倍率
        /// </summary>
        public float StartScale;

        /// <summary>
        /// 结束缩放倍率
        /// </summary>
        public float EndScale;
    }
}
