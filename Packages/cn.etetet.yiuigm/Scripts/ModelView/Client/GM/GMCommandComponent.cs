using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端GM命令管理组件，按分类ID缓存所有已注册的GMCommandInfo列表。
    /// 文档: https://lib9kmxvq7k.feishu.cn/wiki/NYADwMydliVmQ7kWXOuc0yxGn7p
    /// </summary>
    [ComponentOf(typeof (Scene))]
    public class GMCommandComponent: Entity, IAwake, IDestroy
    {
        public Dictionary<int, List<GMCommandInfo>> AllCommandInfo { get; set; }
    }

    //GM相关消息 关闭GMView
    /// <summary>
    /// 通知关闭GM主窗口的动态事件消息。
    /// </summary>
    public struct OnGMEventClose
    {
    }
}