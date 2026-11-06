using System;

namespace ET.Client
{
    /// <summary>
    /// YooAssets加载初始化结束事件参数
    /// </summary>
    public struct YIUIEvent_YooAssetsLoad_Completed
    {
        /// <summary>
        /// 已完成初始化的 YIUIYooAssetsLoadComponent 组件引用
        /// </summary>
        public EntityRef<YIUIYooAssetsLoadComponent> YooAssetsLoadRef;
    }
}