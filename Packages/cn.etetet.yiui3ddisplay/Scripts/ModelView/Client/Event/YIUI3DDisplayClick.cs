using System;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// YIUI 3D展示模型点击回调非泛型基础接口。
    /// </summary>
    public interface IYIUI3DDisplayClick
    {
        void OnClick(Entity self, UI3DDisplay display, GameObject target, GameObject root);
    }

    /// <summary>
    /// YIUI 3D展示模型点击回调泛型系统接口。
    /// </summary>
    public interface IYIUI3DDisplayClick<in T1> : ISystemType, IYIUI3DDisplayClick
    {
    }

    /// <summary>
    /// YIUI 3D展示模型点击事件EntitySystem抽象基类。
    /// 宿主UI组件编写 [EntitySystem] YIUI3DDisplayClick 方法后，由源码生成器自动派生此基类完成注册。
    /// </summary>
    [EntitySystem]
    public abstract class YIUI3DDisplayClickSystem<T1, T2, T3, T4> : SystemObject, IYIUI3DDisplayClick<T1>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUI3DDisplayClick<T1>);
        }

        void IYIUI3DDisplayClick.OnClick(Entity self, UI3DDisplay display, GameObject target, GameObject root)
        {
            YIUI3DDisplayClick((T1)self, display, target, root);
        }

        protected abstract void YIUI3DDisplayClick(T1 self, UI3DDisplay display, GameObject target, GameObject root);
    }
}