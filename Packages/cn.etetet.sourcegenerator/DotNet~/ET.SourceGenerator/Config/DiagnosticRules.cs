using Microsoft.CodeAnalysis;

namespace ET
{
    /// <summary>
    /// 诊断规则（ET1001）：要求标记了 [EntitySystem] 等标签的函数必须声明在 static partial 类中。
    /// </summary>
    public static class ETSystemMethodIsInStaticPartialClassRule
    {
        private const string Title = "ETSystem函数必须声明在静态分部类中";

        private const string MessageFormat = "ETSystem函数所在的类:{0} 不是静态分部类";

        private const string Description = "ETSystem函数必须声明在静态类中.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ETSystemMethodIsInStaticPartialClassRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Generator,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    
    /// <summary>
    /// 诊断规则（ET0008）：在同步方法中调用返回 ETTask 的异步方法时，必须显式调用 .Coroutine() 后缀。
    /// </summary>
    public static class ETTaskInSyncMethodAnalyzerRule
    {
        private const string Title = "ETTask方法调用在非异步方法体内使用错误";

        private const string MessageFormat = "方法: {0} 在非异步方法体内使用时需要添加.Coroutine()后缀";

        private const string Description = "ETTask方法调用在非异步方法体内使用错误.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ETTaskInSyncMethodAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0009）：在异步方法中调用返回 ETTask 的方法时，必须添加 await 前缀或 .Coroutine() 后缀，防止漏掉等待。
    /// </summary>
    public static class ETTaskInAsyncMethodAnalyzerRule
    {
        private const string Title = "ETTask方法调用在异步方法体内使用错误";

        private const string MessageFormat = "方法: {0} 在异步方法体内使用时需要添加await前缀 或.Coroutine()后缀";

        private const string Description = "ETTask方法调用在异步方法体内使用错误.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ETTaskInAsyncMethodAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0011）：标记了 [UniqueId(min, max)] 的类中的 const int/short/long 字段值必须在指定区间内。
    /// </summary>
    public static class UniqueIdRangeAnaluzerRule
    {
        private const string Title = "唯一Id字段数值区间约束";

        private const string MessageFormat = "类: {0} Id字段: {1}的值： {2} 不在约束的区间内, 请修改";

        private const string Description = "唯一Id字段数值区间约束.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.UniqueIdRangeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0012）：同名 [UniqueId] 分部类中的唯一 ID 常量值禁止与其他模块重复。
    /// </summary>
    public static class UniqueIdDuplicateAnalyzerRule
    {
        private const string Title = "唯一Id字段禁止重复";

        private const string MessageFormat = "类: {0} Id字段: {1}的值： {2} 与其他Id字段值重复, 请修改";

        private const string Description = "唯一Id字段禁止重复.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.UniqueIdDuplicateAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0001）：调用 AddChild/AddChildWithId 时，子实体类必须标记对应的 [ChildOf(typeof(Parent))]。
    /// </summary>
    public static class AddChildTypeAnalyzerRule
    {
        private const string Title = "AddChild方法类型约束错误";

        private const string MessageFormat = "Type: {0} 不允许作为实体: {1} 的AddChild函数参数类型! 若要允许该类型作为参数,请使用ChildOfAttribute对child实体类标记父级类型";

        private const string Description = "AddChild方法类型约束错误.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AddChildTypeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0014）：禁止在非所属 Entity 的外部直接访问 Children 或 Components 字典。
    /// </summary>
    public static class DisableAccessEntityChildAnalyzerRule
    {
        private const string Title = "禁止在Entity类中直接调用Child和Component";

        private const string MessageFormat = "禁止在Entity类中直接调用Child和Component";

        private const string Description = "禁止在Entity类中直接调用Child和Component.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableUseChildComponentInEntityAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0007）：调用 AddComponent/GetComponent 时，组件类必须标记对应的 [ComponentOf(typeof(Parent))]。
    /// </summary>
    public static class EntityComponentAnalyzerRule
    {
        private const string Title = "实体类添加或获取组件类型错误";

        private const string MessageFormat = "组件类型: {0} 不允许作为实体: {1} 的组件类型! 若要允许该类型作为参数,请使用ComponentOfAttribute对组件类标记父级实体类型";

        private const string Description = "实体类添加或获取组件类型错误.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityComponentAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0039）：未标记 [AllowInstance] 的 Singleton 禁止通过全局 .Instance 或 World.GetSingleton 访问，以保证 Fiber 线程安全。
    /// </summary>
    public static class DisableSingletonInstanceAccessAnalyzerRule
    {
        private const string Title = "禁止通过全局入口访问 Singleton";

        private const string MessageFormat = "类型: {0} 未标记 [AllowInstance]，禁止通过 .Instance、World.GetSingleton<{0}>() 或 World.TryGetSingleton<{0}>() 访问，请改用 Entity.GetSingleton<{0}>() 或 Fiber.GetSingleton<{0}>()";

        private const string Description = "在 Model/ModelView/Hotfix/HotfixView 程序集中，未标记 [AllowInstance] 的 Singleton 禁止通过全局入口访问.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableSingletonInstanceAccessAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0015）：所有非 const 静态字段必须显式标记 [StaticField]，明确提示开发者注意多 Fiber 并发问题。
    /// </summary>
    public static class StaticFieldDeclarationAnalyzerRule
    {
        private const string Title = "Static字段声明需要标记标签";

        private const string MessageFormat = "Static字段声明 {0} 需要标记标签";

        private const string Description = "Static字段声明需要标记标签.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.StaticFieldDeclarationAnalyzerRule,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0016）：带有 ETCancellationToken 参数的异步方法在 await 返回后必须立即检查 token.IsCancel()。
    /// </summary>
    public static class CheckETCancellTokenAfterAwaitAnalyzerRule
    {
        private const string Title = "含有ETCancelToken参数的异步函数内调用await表达式后必须判断CancelToken.IsCancel";

        private const string MessageFormat = "含有ETCancelToken参数的异步函数内调用await表达式后必须判断CancelToken.IsCancel";

        private const string Description = "含有ETCancelToken参数的异步函数内调用await表达式后必须判断CancelToken.IsCancel.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ETCancellationTokenAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0017）：带有 ETCancellationToken 参数的异步方法内调用其他支持取消的异步方法时必须传递同一个 CancelToken。
    /// </summary>
    public static class AwaitExpressionCancelTokenParamAnalyzerRule
    {
        private const string Title = "含有ETCancelToken参数的异步函数内调用await表达式必须传入同一个CancelToken";

        private const string MessageFormat = "含有ETCancelToken参数的异步函数内调用await表达式必须传入同一个CancelToken";

        private const string Description = "含有ETCancelToken参数的异步函数内调用await表达式必须传入同一个CancelToken.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AwaitExpressionCancelTokenParamAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0018）：异步方法声明处的 ETCancellationToken 参数禁止设置默认值（如 = null）。
    /// </summary>
    public static class AsyncMethodWithCancelTokenParamAnalyzerRule
    {
        private const string Title = "异步函数声明处的ETCancelToken参数禁止声明默认值";

        private const string MessageFormat = "异步函数声明处的ETCancelToken参数禁止声明默认值";

        private const string Description = "异步函数声明处的ETCancelToken参数禁止声明默认值.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AsyncMethodWithCancelTokenParamAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0019）：调用含 ETCancellationToken 参数的方法时禁止显式传入 null。
    /// </summary>
    public static class ExpressionWithCancelTokenParamAnalyzerRule
    {
        private const string Title = "函数调用处的ETCancelToken参数禁止传入null";

        private const string MessageFormat = "函数调用处的ETCancelToken参数禁止传入null";

        private const string Description = "函数调用处的ETCancelToken参数禁止传入null.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ExpressionWithCancelTokenParamAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0003）：实体类必须直接继承自 Entity 或 LSEntity，禁止实体类之间的多层继承。
    /// </summary>
    public static class EntityClassDeclarationAnalyzerRule
    {
        private const string Title = "实体类限制多层继承";

        private const string MessageFormat = "类: {0} 不能继承{1}的子类 请直接继承{1}";

        private const string Description = "实体类限制多层继承.";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.EntityClassDeclarationAnalyzerRuleId,
            Title,
            MessageFormat,
            DiagnosticCategories.All,
            DiagnosticSeverity.Error, true, Description);
    }

    /// <summary>
    /// 诊断规则（ET0010）：实体类内部禁止声明委托字段或属性（符合データ与逻辑分离及热重载安全原则）。
    /// </summary>
    public static class EntityDelegateDeclarationAnalyzerRule
    {
        private const string Title = "实体类禁止声明委托字段或属性";

        private const string MessageFormat = "实体类: {0} 不能在类内部声明委托字段或属性: {1}";

        private const string Description = "实体类禁止声明委托字段或属性.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DelegateAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0020）：实体类内部禁止直接持有其他 Entity 的强引用字段，必须改用 EntityRef&lt;T&gt; 或 EntityWeakRef&lt;T&gt;。
    /// </summary>
    public static class EntityFieldDeclarationInEntityAnalyzerRule
    {
        private const string Title = "实体类禁止声明实体字段";

        private const string MessageFormat = "实体类: {0} 不能在类内部声明实体或含有实体类参数的泛型类字段: {1} 请使用EntityRef代替";

        private const string Description = "实体类禁止声明实体字段.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityFieldDeclarationInEntityAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0021）：禁止声明返回值为 async void 的方法，异步方法必须返回 ETTask。
    /// </summary>
    public static class AsyncMethodReturnTypeAnalyzerRule
    {
        private const string Title = "禁止声明返回值为void的异步方法";

        private const string MessageFormat = "禁止声明返回值为void的异步方法";

        private const string Description = "禁止声明返回值为void的异步方法.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.AsyncMethodReturnTypeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0022）：禁止在 Server 侧代码中引用 ET.Client 命名空间。
    /// </summary>
    public static class ClientClassInServerAnalyzerRule
    {
        private const string Title = "禁止在Server程序集内引用ET.Client命名空间";

        private const string MessageFormat = "禁止在Server程序集内引用ET.Client命名空间";

        private const string Description = "禁止在Server程序集内引用ET.Client命名空间.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.ClientClassInServerAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0023）：帧同步实体 LSEntity 中禁止声明 float/double 浮点数字段或属性，以保证跨平台确定性计算。
    /// </summary>
    public static class LSEntityFloatMemberAnalyzer
    {
        private const string Title = "LSEntity类禁止声明浮点数字段或属性";

        private const string MessageFormat = "LSEntity类: {0} 禁止声明浮点数字段或属性: {1}";

        private const string Description = "LSEntity类禁止声明浮点数字段或属性.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.LSEntityFloatMemberAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0024）：Entity 声明了生命周期接口（如 IAwake/IDestroy），但在对应的 System 类中存在尚未编写的生命周期方法。
    /// </summary>
    public static class EntitySystemAnalyzerRule
    {
        private const string Title = "Entity类存在未生成的生命周期函数";

        private const string MessageFormat = "Entity类: {0} 存在未生成的生命周期函数";

        private const string Description = "Entity类存在未生成的生命周期函数.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntitySystemAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0025）：[EntitySystem] 方法标签只能添加在标记了 [EntitySystemOf] 的静态类中。
    /// </summary>
    public static class EntitySystemMethodNeedSystemOfAttrAnalyzerRule
    {
        private const string Title = "EntitySystem标签只能添加在含有EntitySystemOf标签的静态类中";

        private const string MessageFormat = "方法:{0}的{1}标签只能添加在含有{2}标签的静态类中";

        private const string Description = "EntitySystem标签只能添加在含有EntitySystemOf标签的静态类中.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntitySystemMethodNeedSystemOfAttrAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Model,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0038）：标记了 [EntitySystemOf] 的 System 类所在相对目录必须与对应 Entity 所在的相对目录一致。
    /// </summary>
    public static class EntitySystemLocationAnalyzerRule
    {
        private const string Title = "EntitySystemOf的System类位置不一致";

        private const string MessageFormat = "System类:{0} 位于:{1} 需与Entity:{2} 的相对目录一致，期望目录:{3}";

        private const string Description = "Entity在Model/ModelView的相对位置，System必须在Hotfix/HotfixView对应的相对位置.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntitySystemLocationAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.Hotfix,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0026）：在 Entity 类或包含 Entity 参数的方法内部，必须使用 Fiber 专属日志（self.Fiber().Log）而非全局 Log。
    /// </summary>
    public static class FiberLogAnalyzerRule
    {
        private const string Title = "实体类内部或含有实体类参数的函数内部必须使用Fiber输出日志";

        private const string MessageFormat = "实体类内部或含有实体类参数的函数内部必须使用Fiber输出日志";

        private const string Description = "实体类内部或含有实体类参数的函数内部必须使用Fiber输出日志.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.FiberLogAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0027）：不同实体类的全名 BKDR HashCode 发生哈希碰撞时报错，要求修改类名以保证唯一。
    /// </summary>
    public static class EntityHashCodeAnalyzerRule
    {
        private const string Title = "实体类HashCode禁止重复";

        private const string MessageFormat = "{0} 与 {1} 类名HashCode相同:{2}, 请修改类名保证实体类HashCode唯一";

        private const string Description = "实体类HashCode禁止重复.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityHashCodeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0028）：同一个实体类禁止同时标记 [ComponentOf] 和 [ChildOf]。
    /// </summary>
    public static class EntityComponentChildAnalyzerRule
    {
        private const string Title = "实体类禁止同时标记为Component和Child";

        private const string MessageFormat = "实体类:{0} 禁止同时标记为Component和Child";

        private const string Description = "实体类禁止同时标记为Component和Child.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityComponentChildAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0029）：实体类禁止声明为包含泛型类型参数的泛型类。
    /// </summary>
    public static class EntityCannotDeclareGenericTypeRule
    {
        private const string Title = "实体类禁止声明为泛型实体类";

        private const string MessageFormat = "实体类:{0} 禁止声明为泛型实体类";

        private const string Description = "实体类禁止声明为泛型实体类.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.EntityCannotDeclareGenericTypeRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0030）：网络消息类（实现 IMessage）内部禁止直接声明 Entity 类型的字段。
    /// </summary>
    public static class NetMessageAnalyzerRule
    {
        private const string Title = "消息类禁止声明实体字段";

        private const string MessageFormat = "消息类: {0} 禁止声明实体字段: {1}";

        private const string Description = "消息类禁止声明实体字段.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.NetMessageAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0031）：标记了 [DisableNew] 的类型禁止通过 new 关键字直接构造实例（如 Entity 必须通过 AddComponent/AddChild 创建）。
    /// </summary>
    public static class DisableNewAnalyzerRule
    {
        private const string Title = "含有DisableNew标记的类禁止使用new构造对象";

        private const string MessageFormat = "禁止使用new构造{0}类型的对象";

        private const string Description = "含有DisableNew标记的类禁止使用new构造对象.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableNewAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0037）：标记了 [DisableGetComponent] 的组件禁止直接通过 GetComponent 获取，除非调用方方法显式标记了 [EnableGetComponent]。
    /// </summary>
    public static class DisableGetComponentAnalyzerRule
    {
        private const string Title = "禁止直接使用GetComponent获取被禁用的组件";

        private const string MessageFormat = "组件类型: {0} 禁止直接使用GetComponent获取, 请在方法上添加[EnableGetComponent(typeof({0}))]";

        private const string Description = "被DisableGetComponent标记的组件禁止直接通过GetComponent获取.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableGetComponentAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0032）：Model/ModelView 程序集中禁止声明未继承 Object/Entity 的普通类，除非显式标记 [EnableClass]。
    /// </summary>
    public static class EntityClassDeclarationAnalyzerrRule
    {
        private const string Title = "Model/ModelView程序集禁止声明非实体类";

        private const string MessageFormat = "Model/ModelView程序集禁止声明非Object类:{0}, 除非加上[EnableClass]";

        private const string Description = "Model/ModelView程序集禁止声明非实体类, 除非加上[EnableClass] Attribute.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.DisableNormalClassDeclaratonInModelAssemblyAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0034）：禁止使用 using 语句包裹 CoroutineLock，防止超时自动释放后触发重复释放（应使用 try-finally）。
    /// </summary>
    public static class CoroutineLockUsingAnalyzerRule
    {
        private const string Title = "禁止使用 using 语句获取 CoroutineLock";

        private const string MessageFormat = "禁止使用 using 语句获取 CoroutineLock，因为 CoroutineLock 可能在 using 代码块结束前已经超时释放，导致重复释放问题。请使用 try-finally 或其他方式手动管理。";

        private const string Description = "CoroutineLock 是自管理的锁，会在超时时自动释放，使用 using 语句可能导致重复释放.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.CoroutineLockUsingAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }

    /// <summary>
    /// 诊断规则（ET0035）：禁止在 try-finally 的 finally 块之外直接手动调用 CoroutineLock.Dispose。
    /// </summary>
    public static class CoroutineLockDisposeAnalyzerRule
    {
        private const string Title = "禁止手动调用 CoroutineLock.Dispose";

        private const string MessageFormat = "禁止手动调用 CoroutineLock.Dispose 方法，因为 CoroutineLock 可能已经在超时时自动释放，导致重复释放问题。应该让 CoroutineLock 自动管理其生命周期。";

        private const string Description = "CoroutineLock 的释放由框架自动管理，手动调用 Dispose 可能导致重复释放异常.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.CoroutineLockDisposeAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                true,
                Description);
    }

    /// <summary>
    /// 诊断规则（ET0040）：实现 IPool 的类型必须声明为 partial 以由生成器自动生成 IPool.Clear()，或手写显式接口实现 void IPool.Clear()。
    /// </summary>
    public static class IPoolClearAnalyzerRule
    {
        private const string Title = "IPool类型必须可清理";

        private const string MessageFormat = "Type: {0} 实现了 IPool，但无法生成或找到显式 IPool.Clear() 方法。请将类改为 partial，或手写 void IPool.Clear()。";

        private const string Description = "实现 IPool 的类型必须提供显式 IPool.Clear()，用于对象回池前清理字段，避免与业务 Clear() 冲突.";

        public static readonly DiagnosticDescriptor Rule =
                new DiagnosticDescriptor(DiagnosticIds.IPoolClearAnalyzerRuleId,
                    Title,
                    MessageFormat,
                    DiagnosticCategories.All,
                    DiagnosticSeverity.Error,
                    true,
                    Description);
    }
}
