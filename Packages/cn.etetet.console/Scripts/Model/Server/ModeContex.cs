namespace ET.Server
{
    /// <summary>
    /// <see cref="ModeContex"/> 组件的生命周期系统类。
    /// </summary>
    [EntitySystemOf(typeof(ModeContex))]
    public static partial class ModeContexSystem
    {
        [EntitySystem]
        private static void Awake(this ModeContex self)
        {
            self.Mode = "";
        }
        
        [EntitySystem]
        private static void Destroy(this ModeContex self)
        {
            self.Mode = "";
        }
    }
    


    /// <summary>
    /// 控制台当前交互子模式上下文组件（挂载在 <see cref="ConsoleComponent"/> 上）。
    /// <para>当用户输入某个命令进入子模式后，该组件会记录当前所处的 <see cref="Mode"/>，后续输入将直接路由给该模式的处理器，直至输入 <c>exit</c> 或由处理器主动移除该组件。</para>
    /// </summary>
    [ComponentOf(typeof(ConsoleComponent))]
    public class ModeContex: Entity, IAwake, IDestroy
    {
        /// <summary>当前所处的控制台命令模式名称。</summary>
        public string Mode = "";
    }
}