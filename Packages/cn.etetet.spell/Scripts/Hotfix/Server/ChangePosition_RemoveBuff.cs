namespace ET.Server
{
    /// <summary>
    /// 监听单位移动事件（<see cref="ChangePosition"/>），自动打断并移除单位身上带有 <see cref="BuffFlags.MoveRemove"/> 标记的 Buff（如移动打断读条/引导技能）。
    /// </summary>
    [Event(SceneType.Map)]
    public class ChangePosition_RemoveBuff: AEvent<Scene, ChangePosition>
    {
        /// <summary>
        /// 移除位置变化单位身上所有带 <see cref="BuffFlags.MoveRemove"/> 标记的 Buff。
        /// </summary>
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            BuffHelper.RemoveBuffFlag(unit, BuffFlags.MoveRemove);
            await ETTask.CompletedTask;
        }
    }
}