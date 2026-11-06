namespace ET
{
    /// <summary>
    /// 协程锁类型扩展定义（ActorLocation 模块）。
    /// </summary>
    public static partial class CoroutineLockType
    {
        /// <summary>
        /// Location 服务端处理同一实体位置操作时的互斥协程锁。
        /// </summary>
        public const int Location = PackageType.ActorLocation * 1000 + 1;                  // location进程上使用

        /// <summary>
        /// <see cref="Server.M essageLocationSender"/> 针对同一目标实体按序排队发送位置消息的协程锁。
        /// </summary>
        public const int MessageLocationSender = PackageType.ActorLocation * 1000 + 2;       // MessageLocationSender中队列消息 

        /// <summary>
        /// Location 持久化数据从数据库懒加载初始化时的协程锁。
        /// </summary>
        public const int LocationPersistence = PackageType.ActorLocation * 1000 + 3;         // location持久化初始化
    }
}
