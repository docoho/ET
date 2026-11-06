namespace ET.Server
{
    /// <summary>
    /// AOI 视野可见性附加判定辅助类（可扩展隐身、反隐等自定义可见性规则）。
    /// </summary>
    public static partial class AOISeeCheckHelper
    {
        /// <summary>
        /// 判断观察者 <paramref name="a"/> 是否可以看见目标 <paramref name="b"/>。
        /// </summary>
        public static bool IsCanSee(AOIEntity a, AOIEntity b)
        {
            return true;
        }
    }
}