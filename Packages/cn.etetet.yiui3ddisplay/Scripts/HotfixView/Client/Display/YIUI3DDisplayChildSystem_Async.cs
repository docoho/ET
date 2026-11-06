using System;
using System.Collections.Generic;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 3DDisplay的扩展组件（异步加载与展示模型部分）。
    /// 文档: https://lib9kmxvq7k.feishu.cn/wiki/FhGGwVZSyiCqHCkTVQYcKHQCnKf
    /// </summary>
    public static partial class YIUI3DDisplayChildSystem
    {
        /// <summary>
        /// 异步加载并展示指定资源名称的3D模型及观察摄像机。
        /// </summary>
        /// <param name="self">3D展示子组件实体</param>
        /// <param name="resName">3D模型预制体资源名称</param>
        /// <param name="cameraName">子层级中的目标相机名称（为空则使用默认ShowCamera）</param>
        public static async ETTask<GameObject> ShowAsync(this YIUI3DDisplayChild self, string resName, string cameraName = "")
        {
            if (self.UI3DDisplay == null)
            {
                Debug.LogError($"没有3D显示组件");
                return null;
            }

            EntityRef<YIUI3DDisplayChild> selfRef = self;
            using var _ = await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.YIUIFramework, self.GetHashCode());
            self = selfRef;
            var obj = await self.GetDisplayObjectAsync(resName);
            self = selfRef;
            if (obj == null) return null;
            var camera = string.IsNullOrEmpty(cameraName) ? self.UI3DDisplay.m_ShowCamera : self.GetCamera(obj, cameraName);
            if (camera == null) return obj;
            self.ShowByGameObject(obj, camera);
            return obj;
        }

        private static async ETTask<GameObject> GetDisplayObjectAsync(this YIUI3DDisplayChild self, string resName)
        {
            EntityRef<YIUI3DDisplayChild> selfRef = self;
            if (!self.m_ObjPool.ContainsKey(resName))
            {
                var newObj = await self.CreateObjectAsync(resName);
                if (newObj == null) return null;
                self = selfRef;
                self.m_ObjPool.Add(resName, newObj);
            }

            return self.m_ObjPool[resName];
        }

        private static async ETTask<GameObject> CreateObjectAsync(this YIUI3DDisplayChild self, string resName)
        {
            var obj = await YIUIFactory.InstantiateGameObjectAsync(self.Scene(), "", resName);
            if (obj == null) return null;
            return obj;
        }
    }
}