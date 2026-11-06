namespace ET
{
    /// <summary>
    /// 条件表达式词法单元（Token）类型枚举。
    /// </summary>
    public enum ConditionTokenType
    {
        End = 0,
        Identifier = 1,
        Number = 2,
        Greater = 3,
        GreaterEqual = 4,
        Less = 5,
        LessEqual = 6,
        Equal = 7,
        NotEqual = 8,
        And = 9,
        Or = 10,
        Not = 11,
        LeftParen = 12,
        RightParen = 13,
        Colon = 14,
        Comma = 15,
    }

    /// <summary>
    /// 条件表达式词法单元只读结构体，保存 Token 类型、原始文本及解析出的整型数值。
    /// </summary>
    public readonly struct ConditionToken
    {
        public ConditionToken(ConditionTokenType type, string text, long number)
        {
            this.Type = type;
            this.Text = text;
            this.Number = number;
        }

        public ConditionTokenType Type { get; }
        public string Text { get; }
        public long Number { get; }
    }
}
