using System;
using YIUIFramework;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ET.Client
{
    /// <summary>
    /// GM命令列表项UI组件系统，负责绑定命令名称、描述、参数循环列表以及触发命令执行。
    /// </summary>
    [FriendOf(typeof(GMCommandItemComponent))]
    public static partial class GMCommandItemComponentSystem
    {
        /// <summary>
        /// YIUI初始化生命周期，创建参数横向/纵向循环滚动列表子组件。
        /// </summary>
        [EntitySystem]
        private static void YIUIInitialize(this GMCommandItemComponent self)
        {
            self.m_GMParamLoop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type>(self.u_ComParamLoop, typeof(GMParamItemComponent));
        }

        [EntitySystem]
        private static void YIUILoopRenderer(this GMCommandItemComponent self, GMParamItemComponent item, GMParamInfo data, int index, bool select)
        {
            item.ResetItem(data);
        }

        [EntitySystem]
        private static void Destroy(this GMCommandItemComponent self)
        {
        }

        public static void ResetItem(this GMCommandItemComponent self, GMCommandComponent commandComponent, GMCommandInfo info)
        {
            self.m_CommandComponent = commandComponent;
            self.Info               = info;
            self.u_DataName.SetValue(info.GMName);
            self.u_DataDesc.SetValue(info.GMDesc);
            self.u_DataShowParamLoop.SetValue(info.ParamInfoList.Count >= 1);
            self.WaitRefresh().Coroutine();
        }

        private static async ETTask WaitRefresh(this GMCommandItemComponent self)
        {
            await self.GMParamLoop.SetDataRefresh(self.Info.ParamInfoList);
        }

        #region YIUIEvent开始

        [YIUIInvoke(GMCommandItemComponent.OnEventRunInvoke)]
        private static void OnEventRunInvoke(this GMCommandItemComponent self)
        {
            self.CommandComponent?.Run(self.Info).Coroutine();
        }

        #endregion YIUIEvent结束
    }
}