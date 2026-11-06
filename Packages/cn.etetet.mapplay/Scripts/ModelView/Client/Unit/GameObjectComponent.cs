using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端 Unit 挂载的场景 GameObject 与 Transform 引用组件。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class GameObjectComponent: Entity, IAwake, IDestroy
    {
        private GameObject gameObject;

        /// <summary>角色对应的 Unity GameObject 实例（赋值时自动缓存其 Transform）</summary>
        public GameObject GameObject
        {
            get
            {
                return this.gameObject;
            }
            set
            {
                this.gameObject = value;
                this.Transform = value.transform;
            }
        }

        /// <summary>缓存的角色 Transform 引用</summary>
        public Transform Transform { get; private set; }
    }
}