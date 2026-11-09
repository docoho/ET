namespace ET.Client
{
    /// <summary>
    /// 新手 03：挂在 Scene 上的计数组件。Entity 只保存数据，行为写在对应的 System 中。
    /// IAwake&lt;int&gt; 表示创建时接收一个 int 参数；IDestroy 表示需要销毁回调。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class PracticeCounterComponent : Entity, IAwake<int>, IDestroy
    {
        /// <summary>当前计数，必须由 Awake 初始化，不要依赖对象池中旧对象的字段值。</summary>
        public int Value;
    }
}
