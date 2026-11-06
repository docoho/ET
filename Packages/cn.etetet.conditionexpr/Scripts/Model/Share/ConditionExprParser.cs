using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET
{
    /// <summary>
    /// 条件表达式递归下降语法分析器，按优先级（|| -> && -> ! -> 括号/比较项）将 Token 流转换为行为树节点组合（BTSelector、BTSequence、BTNot 及具体的 BTCondition 子类）。
    /// 支持形如 "Target.HP(param) >= 100:200101" 的宿主指定、参数列表及自定义错误码语法。
    /// </summary>
    [EnableClass]
    public class ConditionExprParser
    {
        private readonly List<ConditionToken> tokens;
        private readonly int defaultErrorCode;
        private int position;
        private int nextNodeId = 1;

        public ConditionExprParser(List<ConditionToken> tokens, int defaultErrorCode)
        {
            this.tokens = tokens;
            this.defaultErrorCode = defaultErrorCode;
        }

        /// <summary>
        /// 解析 Token 序列并返回构建好的 ConditionRoot 行为树根节点。
        /// </summary>
        public ConditionRoot Parse()
        {
            ConditionRoot root = this.CreateNode<ConditionRoot>();
            if (this.Current.Type == ConditionTokenType.End)
            {
                return root;
            }

            root.Children.Add(this.ParseExpression());
            this.Expect(ConditionTokenType.End);
            return root;
        }

        private BTNode ParseExpression()
        {
            return this.ParseOr();
        }

        private BTNode ParseOr()
        {
            BTNode left = this.ParseAnd();
            while (this.Match(ConditionTokenType.Or))
            {
                BTNode right = this.ParseAnd();
                BTSelector selector = this.CreateNode<BTSelector>();
                this.AddSelectorChild(selector, left);
                this.AddSelectorChild(selector, right);
                left = selector;
            }

            return left;
        }

        private BTNode ParseAnd()
        {
            BTNode left = this.ParseUnary();
            while (this.Match(ConditionTokenType.And))
            {
                BTNode right = this.ParseUnary();
                BTSequence sequence = this.CreateNode<BTSequence>();
                this.AddSequenceChild(sequence, left);
                this.AddSequenceChild(sequence, right);
                left = sequence;
            }

            return left;
        }

        private BTNode ParseUnary()
        {
            if (!this.Match(ConditionTokenType.Not))
            {
                return this.ParsePrimary();
            }

            BTNot not = this.CreateNode<BTNot>();
            not.ErrorCode = this.defaultErrorCode;
            not.Children.Add(this.ParseUnary());
            if (this.Match(ConditionTokenType.Colon))
            {
                not.ErrorCode = this.ReadErrorCode();
            }

            return not;
        }

        private BTNode ParsePrimary()
        {
            if (this.Match(ConditionTokenType.LeftParen))
            {
                BTNode node = this.ParseExpression();
                this.Expect(ConditionTokenType.RightParen);
                if (this.Match(ConditionTokenType.Colon))
                {
                    this.ApplyErrorCode(node, this.ReadErrorCode());
                }
                return node;
            }

            return this.ParseCompare();
        }

        private BTNode ParseCompare()
        {
            ConditionToken variableToken = this.Expect(ConditionTokenType.Identifier);
            this.ReadVariable(variableToken.Text, out string ownerKey, out string variable, out bool hasOwnerKey);
            string[] paramValues = this.ReadParams(out bool hasParams);
            ConditionCompareOp op = this.ReadCompareOp();
            ConditionToken valueToken = this.Expect(ConditionTokenType.Number);
            int errorCode = this.defaultErrorCode;
            if (this.Match(ConditionTokenType.Colon))
            {
                errorCode = this.ReadErrorCode();
            }

            ConditionVariableRegistry registry = ConditionVariableRegistry.Instance;
            if (registry == null)
            {
                throw new Exception("condition variable registry is not initialized");
            }

            if (!registry.TryGetNodeType(variable, out Type nodeType))
            {
                throw new Exception($"condition variable not registered: {variable}");
            }

            BTCondition node = Activator.CreateInstance(nodeType) as BTCondition;
            if (node == null)
            {
                throw new Exception($"condition variable node create error: {nodeType.FullName}");
            }

            node.Id = this.NextNodeId();
            if (node is BTNumericCompare numericCompare)
            {
                if (hasParams)
                {
                    this.SetParamsField(numericCompare, paramValues);
                }

                if (registry.TryGetNumericType(variable, out NumericType numericType))
                {
                    numericCompare.NumericType = numericType;
                }

                this.SetOwnerKeyField(numericCompare, ownerKey);
                numericCompare.Op = op;
                numericCompare.Value = valueToken.Number;
                numericCompare.ErrorCode = errorCode;
                return numericCompare;
            }

            if (hasOwnerKey)
            {
                this.SetOwnerKeyField(node, ownerKey);
            }

            if (hasParams)
            {
                this.SetParamsField(node, paramValues);
            }

            this.SetCompareField(node, nameof(BTNumericCompare.Op), op);
            this.SetCompareField(node, nameof(BTNumericCompare.Value), valueToken.Number);
            this.SetCompareField(node, nameof(BTNumericCompare.ErrorCode), errorCode);
            return node;
        }

        private void ReadVariable(string text, out string ownerKey, out string variable, out bool hasOwnerKey)
        {
            ownerKey = ConditionExprEnvKeys.Unit;
            variable = text;
            hasOwnerKey = false;

            int dotIndex = text.IndexOf('.');
            if (dotIndex < 0)
            {
                return;
            }

            if (dotIndex == 0 || dotIndex == text.Length - 1 || dotIndex != text.LastIndexOf('.'))
            {
                throw new Exception($"condition variable reference invalid: {text}");
            }

            ownerKey = text.Substring(0, dotIndex);
            variable = text.Substring(dotIndex + 1);
            hasOwnerKey = true;
        }

        private string[] ReadParams(out bool hasParams)
        {
            hasParams = false;
            if (!this.Match(ConditionTokenType.LeftParen))
            {
                return Array.Empty<string>();
            }

            hasParams = true;
            List<string> paramValues = new();
            if (this.Match(ConditionTokenType.RightParen))
            {
                return paramValues.ToArray();
            }

            while (true)
            {
                ConditionToken paramToken = this.Expect(ConditionTokenType.Identifier);
                paramValues.Add(paramToken.Text);
                if (!this.Match(ConditionTokenType.Comma))
                {
                    break;
                }
            }

            this.Expect(ConditionTokenType.RightParen);
            return paramValues.ToArray();
        }

        private ConditionCompareOp ReadCompareOp()
        {
            ConditionToken token = this.Current;
            ++this.position;
            return token.Type switch
            {
                ConditionTokenType.Greater => ConditionCompareOp.Greater,
                ConditionTokenType.GreaterEqual => ConditionCompareOp.GreaterEqual,
                ConditionTokenType.Less => ConditionCompareOp.Less,
                ConditionTokenType.LessEqual => ConditionCompareOp.LessEqual,
                ConditionTokenType.Equal => ConditionCompareOp.Equal,
                ConditionTokenType.NotEqual => ConditionCompareOp.NotEqual,
                _ => throw new Exception($"condition compare op expected, actual: {token.Text}")
            };
        }

        private int ReadErrorCode()
        {
            ConditionToken token = this.Expect(ConditionTokenType.Number);
            if (token.Number < int.MinValue || token.Number > int.MaxValue)
            {
                throw new Exception($"condition error code out of range: {token.Number}");
            }

            return (int)token.Number;
        }

        private void ApplyErrorCode(BTNode node, int errorCode)
        {
            if (node is BTNumericCompare numericCompare)
            {
                numericCompare.ErrorCode = errorCode;
                return;
            }

            if (node is BTCondition condition)
            {
                this.SetCompareField(condition, nameof(BTNumericCompare.ErrorCode), errorCode);
                return;
            }

            if (node is BTNot not)
            {
                not.ErrorCode = errorCode;
                return;
            }

            throw new Exception($"condition group error code only support leaf or not node: {node.GetType().Name}");
        }

        private void SetCompareField(BTCondition node, string fieldName, object value)
        {
            FieldInfo fieldInfo = node.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (fieldInfo == null)
            {
                throw new Exception($"condition node field not found: {node.GetType().FullName}.{fieldName}");
            }

            fieldInfo.SetValue(node, value);
        }

        private void SetOwnerKeyField(BTCondition node, string ownerKey)
        {
            FieldInfo fieldInfo = node.GetType().GetField(nameof(BTNumericCompare.OwnerKey), BindingFlags.Instance | BindingFlags.Public);
            if (fieldInfo == null)
            {
                throw new Exception($"condition node owner key field not found: {node.GetType().FullName}.{nameof(BTNumericCompare.OwnerKey)}");
            }

            if (fieldInfo.FieldType != typeof(string))
            {
                throw new Exception($"condition node owner key field must be string: {node.GetType().FullName}.{nameof(BTNumericCompare.OwnerKey)}");
            }

            fieldInfo.SetValue(node, ownerKey);
        }

        private void SetParamsField(BTCondition node, string[] paramValues)
        {
            FieldInfo fieldInfo = node.GetType().GetField("Params", BindingFlags.Instance | BindingFlags.Public);
            if (fieldInfo == null)
            {
                throw new Exception($"condition node params field not found: {node.GetType().FullName}.Params");
            }

            if (fieldInfo.FieldType != typeof(string[]))
            {
                throw new Exception($"condition node params field must be string[]: {node.GetType().FullName}.Params");
            }

            fieldInfo.SetValue(node, paramValues);
        }

        private void AddSequenceChild(BTSequence sequence, BTNode child)
        {
            if (child is BTSequence childSequence)
            {
                sequence.Children.AddRange(childSequence.Children);
                return;
            }

            sequence.Children.Add(child);
        }

        private void AddSelectorChild(BTSelector selector, BTNode child)
        {
            if (child is BTSelector childSelector)
            {
                selector.Children.AddRange(childSelector.Children);
                return;
            }

            selector.Children.Add(child);
        }

        private T CreateNode<T>() where T : BTNode, new()
        {
            return new T { Id = this.NextNodeId() };
        }

        private int NextNodeId()
        {
            return this.nextNodeId++;
        }

        private bool Match(ConditionTokenType type)
        {
            if (this.Current.Type != type)
            {
                return false;
            }

            ++this.position;
            return true;
        }

        private ConditionToken Expect(ConditionTokenType type)
        {
            ConditionToken token = this.Current;
            if (token.Type != type)
            {
                throw new Exception($"condition token expected: {type}, actual: {token.Text}");
            }

            ++this.position;
            return token;
        }

        private ConditionToken Current => this.tokens[this.position];
    }
}
