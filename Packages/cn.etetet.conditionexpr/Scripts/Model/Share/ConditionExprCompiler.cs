using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 条件表达式编译器门面类，负责将形如 "Level >= 10 && ItemCount(1001) >= 1" 的字符串先后经过词法分析（Lexer）与语法分析（Parser）编译为可由 BTDispatcher 执行的 ConditionRoot 树。
    /// </summary>
    public static class ConditionExprCompiler
    {
        public static ConditionRoot Compile(string expr, int defaultErrorCode = 1)
        {
            ConditionExprLexer lexer = new(expr);
            List<ConditionToken> tokens = lexer.Tokenize();
            ConditionExprParser parser = new(tokens, defaultErrorCode);
            return parser.Parse();
        }
    }
}
