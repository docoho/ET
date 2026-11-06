using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 战斗主界面（<see cref="MainPanelComponent"/>）系统，承载主角头像框、目标信息框、施法条与底部技能栏等子组件
    /// </summary>
    public static partial class MainPanelComponentSystem
    {
        /// <summary>
        /// YIUI 面板初始化回调
        /// </summary>
        [EntitySystem]
        private static void YIUIInitialize(this MainPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this MainPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this MainPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
