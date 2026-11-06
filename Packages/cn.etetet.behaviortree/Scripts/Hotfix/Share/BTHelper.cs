using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 行为树执行辅助类，封装行为树从根节点开始的入口调用及编辑器下的运行路径事件发布。
    /// </summary>
    public static class BTHelper
    {
        /// <summary>
        /// 运行指定的行为树根节点，并在 Unity 编辑器模式下发布 BTRunTreeEvent 以便可视化高亮执行路径。
        /// </summary>
        public static int RunTree(BTRoot btRoot, BTEnv env)
        {
            env.SetTreeId(btRoot.TreeId);
            int ret = BTDispatcher.Instance.Handle(btRoot, env);
        
#if UNITY_EDITOR
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = btRoot, Env = env});
#endif
            
            return ret;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void PublishRunPath(BTEnv env)
        {
#if UNITY_EDITOR
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env});
#endif
        }
    }
}
