namespace ET
{
    /// <summary>
    /// 进程级默认日志单例（当在非 Fiber 线程或 Fiber 尚未创建前调用 <see cref="ET.Log"/> 时，作为兜底日志输出器）。
    /// </summary>
    public class Logger: Singleton<Logger>, ISingletonAwake
    {
        private ILog log;

        public ILog Log
        {
            set
            {
                this.log = value;
            }
            get
            {
                return this.log;
            }
        }
        
        public void Awake()
        {
        }
    }
}