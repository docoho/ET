using System;

namespace ET.Test
{
    /// <summary>
    /// 轻量空白测试场景（SceneType.TestEmpty）Fiber 初始化处理器，仅在根场景挂载测试数据库自动清理组件。
    /// </summary>
    [Invoke(SceneType.TestEmpty)]
    public class FiberInit_TestEmpty: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            if (root == null)
            {
                throw new Exception("test empty root scene is null");
            }

            root.AddComponent<TestFiberDatabaseCleanupComponent>();

            await ETTask.CompletedTask;
        }
    }
}
