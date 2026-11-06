using System;

namespace ET.Server
{
    /// <summary>
    /// 服务端多区服数据库管理组件（挂载在 <see cref="Scene"/> 上）。
    /// <para>以子实体（Children）形式管理各个区服（Zone）的 <see cref="DBComponent"/> 实例。</para>
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DBManagerComponent: Entity, IAwake
    {
    }
}