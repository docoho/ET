using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.9
    /// Desc    场景头顶信息容器面板（HUDPanel）组件系统，负责承载并管理场景内所有可见单位的头顶血条 <see cref="HPViewComponent"/>
    /// </summary>
    public static partial class HUDPanelComponentSystem
    {
        /// <summary>
        /// YIUI 面板初始化回调
        /// </summary>
        [EntitySystem]
        private static void YIUIInitialize(this HUDPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this HUDPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this HUDPanelComponent self)
        {
            await ETTask.CompletedTask;
            self.m_Unit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (self.Unit == null)
            {
                Log.Error($"没有找到玩家实体");
                return false;
            }

            self.m_UnitConfig = self.Fiber().GetSingleton<UnitConfigCategory>().Get(self.Unit.ConfigId);
            return true;
        }

        [EntitySystem]
        private static async ETTask DynamicEvent(this ET.Client.HUDPanelComponent self, ET.Client.EventMain_ShowHPView param1)
        {
            await ETTask.CompletedTask;
            self.AddHPView(param1.HPView);
        }

        private static void AddHPView(this HUDPanelComponent self, HPViewComponent hpView)
        {
            hpView.UIBase.OwnerRectTransform.SetParent(self.u_ComHPContent);
        }

        private static void UpdateHPView(this HUDPanelComponent self)
        {
            //TODO  Update  所有已知的HP 修改前后顺序
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}
