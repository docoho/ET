using System;

namespace ET
{
    /// <summary>
    /// 技能施法打断标记枚举。
    /// </summary>
    public enum SpellFlags
    {
        MoveInterrupt = 1, // 移动
        ChangeRotationInterrupt = 2, // 改变朝向
        StuneInterrupt = 3, // 眩晕
        RideInterrupt = 4, // 骑行
        NewSpellInterrupt = 5, // 新技能打断 
    }
}