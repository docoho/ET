using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.7
    /// Desc    主界面左上角本地玩家信息框（PlayerInfo）组件系统，负责绑定本地玩家单位并展示头像、名称及生命/法力条
    /// </summary>
    public static partial class PlayerInfoComponentSystem
    {
        /// <summary>
        /// 初始化玩家信息框并注册头像点击事件
        /// </summary>
        [EntitySystem]
        private static void YIUIInitialize(this PlayerInfoComponent self)
        {
            self.UpdateInfo();
            self.UIUnitInfo.u_EventClickInfo.Add(self, PlayerInfoComponent.OnEventClickInfoInvoke);
        }

        [EntitySystem]
        private static void Destroy(this PlayerInfoComponent self)
        {
        }

        private static void UpdateInfo(this PlayerInfoComponent self)
        {
            self.m_Unit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (self.Unit == null)
            {
                Log.Error($"没有找到玩家实体");
                return;
            }

            self.m_UnitConfig = self.Fiber().GetSingleton<UnitConfigCategory>().Get(self.Unit.ConfigId);
            self.UIUnitInfo.RefreshUnitInfo(self.Unit);
        }

        #region YIUIEvent开始

        [YIUIInvoke(PlayerInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this PlayerInfoComponent self)
        {
            await ETTask.CompletedTask;
            if (self.Unit == null) return;
            Log.Info($"点击了自己的头像 {self.m_UnitConfig.Name},{self.Unit.Id}");
        }

        #endregion YIUIEvent结束
    }
}
