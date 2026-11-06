namespace ET.Server
{
    /// <summary>
    /// 服务端处理客户端请求施放技能（C2M_SpellCast）的位置消息处理器。
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_SpellCastHandler: MessageLocationHandler<Unit, C2M_SpellCast>
    {
        /// <summary>
        /// 更新施法者的目标坐标并调用 <see cref="SpellHelper.Cast(Unit,int,Buff)"/> 触发技能施放。
        /// </summary>
        protected override async ETTask Run(Unit unit, C2M_SpellCast message)
        {
            unit.GetComponent<TargetComponent>().Position = message.TargetPosition;
            SpellHelper.Cast(unit, message.SpellConfigId);
            await ETTask.CompletedTask;
        }
    }
}