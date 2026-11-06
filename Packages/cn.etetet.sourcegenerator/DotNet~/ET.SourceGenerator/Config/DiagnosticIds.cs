namespace ET
{
    /// <summary>
    /// ET 框架自定义 Roslyn 编译诊断规则（Analyzer Rule）的唯一编号（Diagnostic ID）集中定义。
    /// <para>以 "ET00xx" 命名的通常是编码规范与架构约束检查，以 "ET10xx" 命名的通常是源码生成或深度调用链检查。</para>
    /// </summary>
    public static class DiagnosticIds
    {
        /// <summary>AddChild 方法子实体类型约束检查（要求标记 [ChildOf]）。</summary>
        public const string AddChildTypeAnalyzerRuleId = "ET0001";

        /// <summary>实体类禁止多层继承检查（必须直接继承 Entity 或 LSEntity）。</summary>
        public const string EntityClassDeclarationAnalyzerRuleId = "ET0003";

        /// <summary>Hotfix 程序集禁止声明非静态字段或属性检查。</summary>
        public const string HotfixProjectFieldDeclarationAnalyzerRuleId = "ET0004";

        /// <summary>Hotfix 程序集只允许声明静态类（除非标记 [EnableClass]）检查。</summary>
        public const string ClassDeclarationInHotfixAnalyzerRuleId = "ET0005";

        /// <summary>Hotfix 程序集禁止声明结构体（除非标记 [EnableHotfixStruct]）检查。</summary>
        public const string StructDeclarationInHotfixAnalyzerRuleId = "ET0033";
        
        /// <summary>Entity 子类内部禁止直接声明方法（除非标记 [EnableMethod]）检查。</summary>
        public const string EntityMethodDeclarationAnalyzerRuleId = "ET0006";

        /// <summary>AddComponent/GetComponent 组件类型约束检查（要求标记 [ComponentOf]）。</summary>
        public const string EntityComponentAnalyzerRuleId = "ET0007";
        
        /// <summary>非异步方法内调用返回 ETTask 的方法必须加 .Coroutine() 检查。</summary>
        public const string ETTaskInSyncMethodAnalyzerRuleId = "ET0008";
        
        /// <summary>异步方法内调用返回 ETTask 的方法必须使用 await 或 .Coroutine() 检查。</summary>
        public const string ETTaskInAsyncMethodAnalyzerRuleId = "ET0009";

        /// <summary>实体类内部禁止声明委托字段或属性检查。</summary>
        public const string DelegateAnalyzerRuleId = "ET0010";

        /// <summary>[UniqueId] 唯一 ID 常量值超出允许区间检查。</summary>
        public const string UniqueIdRangeAnalyzerRuleId = "ET0011";
        
        /// <summary>[UniqueId] 唯一 ID 常量值重复检查。</summary>
        public const string UniqueIdDuplicateAnalyzerRuleId = "ET0012";

        /// <summary>静态类之间跨模块循环依赖检查。</summary>
        public const string StaticClassCircularDedendencyAnalyzerRuleId = "ET0013";

        /// <summary>禁止在非所属 Entity 的外部直接访问 Child 和 Component 集合检查。</summary>
        public const string DisableUseChildComponentInEntityAnalyzerRuleId = "ET0014";

        /// <summary>静态字段声明必须标记 [StaticField] 标签检查。</summary>
        public const string StaticFieldDeclarationAnalyzerRule = "ET0015";

        /// <summary>含 ETCancellationToken 参数的异步方法在 await 后必须检查 IsCancel()。</summary>
        public const string ETCancellationTokenAnalyzerRuleId = "ET0016";

        /// <summary>含 ETCancellationToken 参数的异步方法内调用其他可取消 await 时必须透传同一个 token。</summary>
        public const string AwaitExpressionCancelTokenParamAnalyzerRuleId = "ET0017";

        /// <summary>异步方法声明中的 ETCancellationToken 参数禁止设置默认值。</summary>
        public const string AsyncMethodWithCancelTokenParamAnalyzerRuleId = "ET0018";

        /// <summary>调用含 ETCancellationToken 参数的方法时禁止传入 null。</summary>
        public const string ExpressionWithCancelTokenParamAnalyzerRuleId = "ET0019";

        /// <summary>实体类内部禁止直接声明 Entity 强引用字段（必须改用 EntityRef/EntityWeakRef）。</summary>
        public const string EntityFieldDeclarationInEntityAnalyzerRuleId = "ET0020";

        /// <summary>禁止声明返回值为 async void 的异步方法（必须返回 ETTask）。</summary>
        public const string AsyncMethodReturnTypeAnalyzerRuleId = "ET0021";

        /// <summary>禁止在 Server 侧代码中引用 ET.Client 命名空间。</summary>
        public const string ClientClassInServerAnalyzerRuleId = "ET0022";

        /// <summary>帧同步实体 LSEntity 中禁止声明 float/double 浮点数字段或属性（必须使用定点数 FP）。</summary>
        public const string LSEntityFloatMemberAnalyzerRuleId = "ET0023";

        /// <summary>Entity 实现了生命周期接口（如 IAwake）但在对应 System 类中缺少生命周期方法实现检查。</summary>
        public const string EntitySystemAnalyzerRuleId = "ET0024";

        /// <summary>[EntitySystem] 方法必须位于标记了 [EntitySystemOf] 的静态类中检查。</summary>
        public const string EntitySystemMethodNeedSystemOfAttrAnalyzerRuleId = "ET0025";

        /// <summary>实体类或含实体参数的方法内必须使用 Fiber.Log 而不是全局 Log 检查。</summary>
        public const string FiberLogAnalyzerRuleId = "ET0026";
        
        
        /// <summary>实体类全名 LongHashCode 哈希冲突检查。</summary>
        public const string EntityHashCodeAnalyzerRuleId = "ET0027";

        /// <summary>实体类禁止同时标记 [ComponentOf] 和 [ChildOf] 检查。</summary>
        public const string EntityComponentChildAnalyzerRuleId = "ET0028";

        /// <summary>实体类禁止声明为泛型类检查。</summary>
        public const string EntityCannotDeclareGenericTypeRuleId = "ET0029";

        /// <summary>网络消息类禁止包含 Entity 字段检查。</summary>
        public const string NetMessageAnalyzerRuleId = "ET0030";

        /// <summary>标记了 [DisableNew] 的类型禁止使用 new 关键字实例化检查。</summary>
        public const string DisableNewAnalyzerRuleId = "ET0031";

        /// <summary>Model/ModelView 程序集中禁止声明非 Object/Entity 类（除非标记 [EnableClass]）检查。</summary>
        public const string DisableNormalClassDeclaratonInModelAssemblyAnalyzerRuleId = "ET0032";
        
        /// <summary>ETSystem 生成器要求生命周期扩展方法必须位于 static partial 类中。</summary>
        public const string ETSystemMethodIsInStaticPartialClassRuleId = "ET1001";
        
        /// <summary>跨 await 异步等待后未校验 Entity 是否已销毁检查。</summary>
        public const string ETAwaitEntityAnalyzerRuleId = "ET1002";
        
        /// <summary>跨模块直接访问 Entity 字段（缺少对应 [Module] 权限）检查。</summary>
        public const string ETFieldAccessDiagnosticId = "ET1003";
        /// <summary>模块间循环调用检查。</summary>
        public const string ETCyclicCallDiagnosticId = "ET1004";

        /// <summary>禁止使用 using 语句包裹 CoroutineLock 检查。</summary>
        public const string CoroutineLockUsingAnalyzerRuleId = "ET0034";

        /// <summary>禁止手动调用 CoroutineLock.Dispose 方法检查。</summary>
        public const string CoroutineLockDisposeAnalyzerRuleId = "ET0035";

        /// <summary>测试用例命名规范检查。</summary>
        public const string TestCaseNamingAnalyzerRuleId = "ET0036";
        
        /// <summary>标记了 [DisableGetComponent] 的组件禁止直接通过 GetComponent 获取（除非方法标记 [EnableGetComponent]）。</summary>
        public const string DisableGetComponentAnalyzerRuleId = "ET0037";
        
        /// <summary>System 类所在相对目录必须与对应 Entity 类所在相对目录一致检查。</summary>
        public const string EntitySystemLocationAnalyzerRuleId = "ET0038";

        /// <summary>未标记 [AllowInstance] 的 Singleton 禁止通过 .Instance 或 World.GetSingleton 访问检查。</summary>
        public const string DisableSingletonInstanceAccessAnalyzerRuleId = "ET0039";

        /// <summary>实现 IPool 的类型必须提供或可自动生成显式 IPool.Clear() 方法检查。</summary>
        public const string IPoolClearAnalyzerRuleId = "ET0040";
    }
}
