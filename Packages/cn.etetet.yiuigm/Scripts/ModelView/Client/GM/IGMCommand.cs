using System.Collections.Generic;
using YIUIFramework;

namespace ET.Client
{
    //GM命令接口
    /// <summary>
    /// GM命令统一执行接口，配合 [GM] 特性即可自动注册到GM调试面板。
    /// </summary>
    public interface IGMCommand
    {
        //返回这个命令所需参数列表
        /// <summary>返回该命令所需的输入参数定义列表。</summary>
        List<GMParamInfo> GetParams();
        //执行命令方法  返回值=执行完毕后是否关闭GM面板
        /// <summary>执行GM命令逻辑，返回true表示执行完毕后自动关闭GM窗口。</summary>
        ETTask<bool>      Run(Scene clientScene, ParamVo paramVo);
    }
}