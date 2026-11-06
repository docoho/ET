using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 循环滚动列表演示单项UI组件系统。
    /// </summary>
    [FriendOf(typeof(LoopScrollRectDemoItemComponent))]
    public static partial class LoopScrollRectDemoItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this LoopScrollRectDemoItemComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this LoopScrollRectDemoItemComponent self)
        {
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(LoopScrollRectDemoItemComponent.OnEventSelectInvoke)]
        private static void OnEventSelectInvoke(this LoopScrollRectDemoItemComponent self)
        {

        }
        #endregion YIUIEvent结束
    }
}
