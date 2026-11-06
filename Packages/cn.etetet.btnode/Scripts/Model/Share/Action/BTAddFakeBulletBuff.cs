namespace ET
{
    /// <summary>
    /// 动作节点配置：向目标单位添加追踪假子弹 Buff（根据施法者与目标的距离及飞行速度自动计算过期时间）。
    /// </summary>
    [System.Serializable]
    public class BTAddFakeBulletBuff: BTAction
    {
        /// <summary>输入黑板变量名：发射假子弹的施法者单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：假子弹追踪的目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>输入黑板变量名：触发该节点的源技能/父 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>输出黑板变量名：新创建的假子弹 Buff 实例</summary>
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTInput(typeof(Buff))]
        public string OutputBuff;

        /// <summary>假子弹对应的 Buff 配置表 ID</summary>
        public int ConfigId;

        /// <summary>假子弹飞行速度（用于计算到达目标所需的持续时间）</summary>
        public int Speed;
    }
}