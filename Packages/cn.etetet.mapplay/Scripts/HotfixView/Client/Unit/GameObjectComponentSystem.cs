using System;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端 Unit 挂载的 Unity GameObject 包装组件系统：在实体销毁时同步销毁对应的场景 GameObject。
    /// </summary>
    [EntitySystemOf(typeof(GameObjectComponent))]
    public static partial class GameObjectComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this GameObjectComponent self)
        {
            UnityEngine.Object.Destroy(self.GameObject);
        }
        
        [EntitySystem]
        private static void Awake(this GameObjectComponent self)
        {
        }
    }
}