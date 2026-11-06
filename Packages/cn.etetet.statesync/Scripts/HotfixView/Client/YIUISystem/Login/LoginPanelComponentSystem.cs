using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 登录界面（<see cref="LoginPanelComponent"/>）系统，处理账号密码输入与点击登录请求
    /// </summary>
    public static partial class LoginPanelComponentSystem
    {
        /// <summary>
        /// YIUI 面板初始化回调
        /// </summary>
        [EntitySystem]
        private static void YIUIInitialize(this LoginPanelComponent self)
        {
        }

        /// <summary>
        /// 面板销毁回调
        /// </summary>
        [EntitySystem]
        private static void Destroy(this LoginPanelComponent self)
        {
        }

        /// <summary>
        /// YIUI 面板打开回调
        /// </summary>
        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LoginPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始

        /// <summary>
        /// 响应登录按钮点击事件：读取输入框账号密码并调用 <see cref="LoginHelper.Login"/> 执行登录流程
        /// </summary>
        [YIUIInvoke(LoginPanelComponent.OnEventLoginInvoke)]
        private static async ETTask OnEventLoginInvoke(this LoginPanelComponent self)
        {
            Log.Info($"登录");
            GlobalComponent globalComponent = self.Root().GetComponent<GlobalComponent>();
            await LoginHelper.Login(self.Root(), globalComponent.GlobalConfig.Address, self.u_ComAccount.text, self.u_ComPassword.text);
        }

        #endregion YIUIEvent结束
    }
}
