namespace ET
{
    /// <summary>
    /// 协程锁类型常量扩展（DB 数据库模块）。
    /// </summary>
    public static partial class CoroutineLockType
    {
        /// <summary>MongoDB 数据库读写串行化协程锁。</summary>
        public const int MongoDB = PackageType.DB * 1000 + 1;
    }
}