using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 条件表达式词法分析器，将原始表达式字符串切分为标识符、数字（含负数）、比较符、逻辑运算符（&&、||、!）及括号/冒号/逗号 Token 流。
    /// </summary>
    [EnableClass]
    public class ConditionExprLexer
    {
        private readonly string expr;
        private readonly List<ConditionToken> tokens = new();
        private int index;

        public ConditionExprLexer(string expr)
        {
            this.expr = expr ?? string.Empty;
        }

        /// <summary>
        /// 扫描整个表达式字符串并生成以 ConditionTokenType.End 结尾的词法单元列表。
        /// </summary>
        public List<ConditionToken> Tokenize()
        {
            while (this.index < this.expr.Length)
            {
                char c = this.expr[this.index];
                if (char.IsWhiteSpace(c))
                {
                    ++this.index;
                    continue;
                }

                if (this.IsIdentifierStart(c))
                {
                    this.ReadIdentifier();
                    continue;
                }

                if (char.IsDigit(c) || c == '-' && this.index + 1 < this.expr.Length && char.IsDigit(this.expr[this.index + 1]))
                {
                    this.ReadNumber();
                    continue;
                }

                this.ReadOperator();
            }

            this.tokens.Add(new ConditionToken(ConditionTokenType.End, string.Empty, 0));
            return this.tokens;
        }

        private void ReadIdentifier()
        {
            int start = this.index++;
            while (this.index < this.expr.Length && this.IsIdentifierPart(this.expr[this.index]))
            {
                ++this.index;
            }

            string text = this.expr.Substring(start, this.index - start);
            this.tokens.Add(new ConditionToken(ConditionTokenType.Identifier, text, 0));
        }

        private void ReadNumber()
        {
            int start = this.index++;
            while (this.index < this.expr.Length && char.IsDigit(this.expr[this.index]))
            {
                ++this.index;
            }

            string text = this.expr.Substring(start, this.index - start);
            if (!long.TryParse(text, out long number))
            {
                throw new Exception($"condition number parse error: {text}");
            }

            this.tokens.Add(new ConditionToken(ConditionTokenType.Number, text, number));
        }

        private void ReadOperator()
        {
            char c = this.expr[this.index];
            switch (c)
            {
                case '>':
                    this.AddIfNext('=', ConditionTokenType.GreaterEqual, ConditionTokenType.Greater);
                    return;
                case '<':
                    this.AddIfNext('=', ConditionTokenType.LessEqual, ConditionTokenType.Less);
                    return;
                case '=':
                    this.ExpectNext('=', ConditionTokenType.Equal);
                    return;
                case '!':
                    this.AddIfNext('=', ConditionTokenType.NotEqual, ConditionTokenType.Not);
                    return;
                case '&':
                    this.ExpectNext('&', ConditionTokenType.And);
                    return;
                case '|':
                    this.ExpectNext('|', ConditionTokenType.Or);
                    return;
                case '(':
                    this.tokens.Add(new ConditionToken(ConditionTokenType.LeftParen, "(", 0));
                    ++this.index;
                    return;
                case ')':
                    this.tokens.Add(new ConditionToken(ConditionTokenType.RightParen, ")", 0));
                    ++this.index;
                    return;
                case ':':
                    this.tokens.Add(new ConditionToken(ConditionTokenType.Colon, ":", 0));
                    ++this.index;
                    return;
                case ',':
                    this.tokens.Add(new ConditionToken(ConditionTokenType.Comma, ",", 0));
                    ++this.index;
                    return;
                default:
                    throw new Exception($"condition token error at {this.index}: {c}");
            }
        }

        private void AddIfNext(char next, ConditionTokenType matchType, ConditionTokenType singleType)
        {
            char c = this.expr[this.index];
            if (this.index + 1 < this.expr.Length && this.expr[this.index + 1] == next)
            {
                this.tokens.Add(new ConditionToken(matchType, $"{c}{next}", 0));
                this.index += 2;
                return;
            }

            this.tokens.Add(new ConditionToken(singleType, c.ToString(), 0));
            ++this.index;
        }

        private void ExpectNext(char next, ConditionTokenType tokenType)
        {
            char c = this.expr[this.index];
            if (this.index + 1 >= this.expr.Length || this.expr[this.index + 1] != next)
            {
                throw new Exception($"condition token error at {this.index}: {c}");
            }

            this.tokens.Add(new ConditionToken(tokenType, $"{c}{next}", 0));
            this.index += 2;
        }

        private bool IsIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private bool IsIdentifierPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '.';
        }
    }
}
