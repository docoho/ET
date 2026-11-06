using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 服务端向客户端发送/广播地图消息的目标范围枚举。
    /// </summary>
    [LabelText("广播客户端类型")]
    public enum NoticeType
    {
        /// <summary>
        /// 不通知任何客户端。
        /// </summary>
        [LabelText("无")]
        NoNotice,

        /// <summary>
        /// 仅通知单位自身的客户端。
        /// </summary>
        [LabelText("自己")]
        Self,

        /// <summary>
        /// 广播给视野内所有看得见该单位的玩家（包含自己）。
        /// </summary>
        [LabelText("全体")]
        Broadcast,

        /// <summary>
        /// 广播给视野内除自己以外的其他玩家。
        /// </summary>
        [LabelText("除自己外")]
        BroadcastWithoutSelf
    }
}
