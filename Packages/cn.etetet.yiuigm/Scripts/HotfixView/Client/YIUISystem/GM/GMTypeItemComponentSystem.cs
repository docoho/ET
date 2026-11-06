using System;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// GM命令分类页签项UI组件系统，负责渲染分类名称及选中高亮状态。
    /// </summary>
    [FriendOf(typeof(GMTypeItemComponent))]
    public static partial class GMTypeItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this GMTypeItemComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this GMTypeItemComponent self)
        {
        }

        /// <summary>
        /// 刷新GM分类页签显示名称。
        /// </summary>
        public static void ResetItem(this GMTypeItemComponent self, int data)
        {
            self.u_DataTypeName.SetValue(GMKeyHelper.GetDesc(data));
        }

        public static void SelectItem(this GMTypeItemComponent self, bool value)
        {
            self.u_DataSelect.SetValue(value);
        }

        #region YIUIEvent开始

        [YIUIInvoke(GMTypeItemComponent.OnEventSelectInvoke)]
        private static void OnEventSelectInvoke(this GMTypeItemComponent self)
        {
        }

        #endregion YIUIEvent结束
    }
}