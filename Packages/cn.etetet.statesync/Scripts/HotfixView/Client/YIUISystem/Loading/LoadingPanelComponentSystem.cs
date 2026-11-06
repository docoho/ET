using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2024.10.28
    /// Desc    场景加载过渡界面（LoadingPanel）系统
    /// </summary>
    public static partial class LoadingPanelComponentSystem
    {
        /// <summary>
        /// YIUI 面板初始化回调
        /// </summary>
        [EntitySystem]
        private static void YIUIInitialize(this LoadingPanelComponent self)
        {
        }

        /// <summary>
        /// 面板销毁回调
        /// </summary>
        [EntitySystem]
        private static void Destroy(this LoadingPanelComponent self)
        {
        }

        /// <summary>
        /// YIUI 面板打开生命周期回调
        /// </summary>
        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LoadingPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
