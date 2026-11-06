using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端主角虚拟相机（Cinemachine）组件：保存相机引用与跟随目标节点（Follow/Head）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class CinemachineComponent: Entity, IAwake
    {
        public Cinemachine.CinemachineFreeLook FreeLook;
        /// <summary>场景中的 Cinemachine 虚拟相机组件</summary>
        public Cinemachine.CinemachineVirtualCamera VirtualCamera;

        /// <summary>相机实际 LookAt 与 Follow 的代理 Transform 节点</summary>
        public Transform Follow { get; set; }

        /// <summary>角色模型头部绑点 Transform</summary>
        public Transform Head { get; set; }
    }
}