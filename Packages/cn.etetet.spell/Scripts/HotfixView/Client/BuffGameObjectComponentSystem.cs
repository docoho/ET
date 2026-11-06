using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端 Buff 关联特效 GameObject 管理组件系统（在 Buff 销毁时自动销毁其挂载的所有特效对象）。
    /// </summary>
    [EntitySystemOf(typeof(BuffGameObjectComponent))]
    public static partial class BuffGameObjectComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this BuffGameObjectComponent self)
        {

        }

        /// <summary>
        /// 组件销毁时，遍历并调用 <see cref="UnityEngine.Object.Destroy(UnityEngine.Object)"/> 销毁所有关联的特效 GameObject。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this BuffGameObjectComponent self)
        {
            foreach (GameObject gameObject in self.GameObjects)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}