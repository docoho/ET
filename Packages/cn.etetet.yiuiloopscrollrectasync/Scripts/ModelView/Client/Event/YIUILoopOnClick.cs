using System;

namespace ET.Client
{
    /// <summary>
    /// YIUI循环列表项点击回调非泛型基础接口。
    /// </summary>
    public interface IYIUILoopOnClick
    {
        /// <summary>
        /// 点击事件
        /// 调用SetOnClick方法设置点击事件信息后生效
        /// </summary>
        void OnClick(Entity self, Entity item, object data, int index, bool select);
    }

    /// <summary>
    /// YIUI循环列表项点击回调泛型系统接口（T1:宿主UI组件，T2:列表项UI组件，T3:数据类型）。
    /// </summary>
    public interface IYIUILoopOnClick<in T1, in T2, in T3> : ISystemType, IYIUILoopOnClick
    {
    }

    /// <summary>
    /// YIUI循环列表项点击事件EntitySystem抽象基类。
    /// </summary>
    [EntitySystem]
    public abstract class YIUILoopOnClickSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUILoopOnClick<T1, T2, T3>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUILoopOnClick<T1, T2, T3>);
        }

        void IYIUILoopOnClick.OnClick(Entity self, Entity item, object data, int index, bool select)
        {
            YIUILoopOnClick((T1)self, (T2)item, (T3)data, index, select);
        }

        protected abstract void YIUILoopOnClick(T1 self, T2 item, T3 data, int index, bool select);
    }
}