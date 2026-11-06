using UnityEngine;

namespace ET
{
    /// <summary>
    /// 挂载在 Unity 场景 <see cref="GameObject"/> 上的实体弱引用桥接组件（<see cref="MonoBehaviour"/>）。
    /// 用于在碰撞检测、射线拾取或 UI 交互等拿到 <see cref="GameObject"/> 时，能够安全地反向查找到其对应的逻辑层或表现层 <see cref="ET.Entity"/>。
    /// </summary>
    public class GameObjectEntityRef: MonoBehaviour
    {
        /// <summary>通过 <see cref="EntityRef{T}"/> 安全持有实体引用，防止实体销毁后产生悬空引用。</summary>
        private EntityRef<Entity> entityRef;
        
#if UNITY_EDITOR && ENABLE_VIEW
        /// <summary>编辑器下关联的 Hierarchy 视图调试节点引用。</summary>
        public GameObject gameObjectRef;
#endif
        
        /// <summary>
        /// 获取或设置当前 GameObject 关联的 ET <see cref="ET.Entity"/>（若实体已销毁，Getter 将安全返回 null）。
        /// </summary>
        public Entity Entity
        {
            get
            {
                return this.entityRef;
            }
            set
            {
                this.entityRef = value;

#if UNITY_EDITOR && ENABLE_VIEW
                this.gameObjectRef = value.ViewGO;
#endif
            }
        }
    }
}