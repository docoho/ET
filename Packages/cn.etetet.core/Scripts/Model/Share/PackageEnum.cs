namespace ET
{
    /// <summary>
    /// UPM 包类型枚举反射单例，维护 <see cref="PackageType"/> 常量名与整型编号的映射表。
    /// </summary>
    [CodeProcess]
    public class PackageEnum: EnumSingleton<PackageEnum>, ISingletonAwake
    {
        public void Awake()
        {
            Init(typeof(PackageType));
        }
    }
}