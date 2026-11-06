using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 游戏大厅界面（<see cref="LobbyPanelComponent"/>）系统，处理点击进入地图等交互事件
    /// </summary>
    public static partial class LobbyPanelComponentSystem
    {
        /// <summary>
        /// YIUI 面板初始化回调
        /// </summary>
        [EntitySystem]
        private static void YIUIInitialize(this LobbyPanelComponent self)
        {
        }

        /// <summary>
        /// 面板销毁回调
        /// </summary>
        [EntitySystem]
        private static void Destroy(this LobbyPanelComponent self)
        {
        }

        /// <summary>
        /// YIUI 面板打开回调
        /// </summary>
        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LobbyPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始

        /// <summary>
        /// 响应大厅界面“进入地图”按钮点击事件：请求进入游戏地图并在成功后关闭大厅面板
        /// </summary>
        [YIUIInvoke(LobbyPanelComponent.OnEventEnterMapInvoke)]
        private static async ETTask OnEventEnterMapInvoke(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            await EnterMapHelper.EnterMapAsync(self.Root());
            self = selfRef;
            await self.UIPanel.CloseAsync();
        }

        #endregion YIUIEvent结束
    }
}