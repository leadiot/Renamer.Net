using Com.Scm.Renamer.Patterns;
using System.Collections.Generic;
using System.Globalization;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 模板解析器：把模板扫描为“字面量 + 表达式”的 AST 片段列表。
    /// 表达式定界符 &lt; &gt; 是 Windows 文件名字符，天然不会出现在生成结果中，
    /// 因此只需找到成对的 &lt; &gt;，不支持嵌套。
    /// </summary>
    public static class TemplateParser
    {
        /// <summary>
        /// 解析模板，返回按顺序排列的表达式片段。
        /// </summary>
        public static List<RenamePattern> Parse(string template)
        {
            var segments = new List<RenamePattern>();
            if (string.IsNullOrEmpty(template))
            {
                return segments;
            }

            int literalStart = 0;
            int i = 0;
            while (i < template.Length)
            {
                if (template[i] != RenamePattern.PATTERN_START)
                {
                    i++;
                    continue;
                }

                // 先结算字面量
                if (i > literalStart)
                {
                    segments.Add(CreateText(template.Substring(literalStart, i - literalStart)));
                }

                int end = template.IndexOf(RenamePattern.PATTERN_END, i + 1);
                if (end < 0)
                {
                    throw new TemplateParseException("表达式缺少结束符 >", i);
                }

                string body = template.Substring(i + 1, end - i - 1);
                segments.Add(ParseExpression(body, i + 1, template.Substring(i, end - i + 1)));

                i = end + 1;
                literalStart = i;
            }

            if (literalStart < template.Length)
            {
                segments.Add(CreateText(template.Substring(literalStart)));
            }

            return segments;
        }

        private static TextPattern CreateText(string text)
        {
            return new TextPattern
            {
                Text = text,
                Pattern = text
            };
        }

        private static RenamePattern ParseExpression(string body, int bodyPos, string raw)
        {
            if (body.Length == 0)
            {
                throw new TemplateParseException("表达式内容为空", bodyPos);
            }

            RenamePattern pattern;
            char first = body[0];

            if (first == RenamePattern.PATTERN_PREFIX[0])
            {
                var p = new NamePrefixPattern();
                p.Decode(body, bodyPos);
                pattern = p;
            }
            else if (first == RenamePattern.PATTERN_SUFFIX[0])
            {
                var p = new NameSuffixPattern();
                p.Decode(body, bodyPos);
                pattern = p;
            }
            else if (first == RenamePattern.PATTERN_ESCAPE)
            {
                if (body.Length < 2)
                {
                    throw new TemplateParseException("表达式类型不完整（|* 数字、|? 字母、|\" 时间）", bodyPos);
                }

                switch (body[1])
                {
                    case '*':
                        {
                            var p = new NumberPattern();
                            p.Decode(body, bodyPos);
                            pattern = p;
                            break;
                        }
                    case '?':
                        {
                            var p = new LetterPattern();
                            p.Decode(body, bodyPos);
                            pattern = p;
                            break;
                        }
                    case '"':
                        {
                            var p = new TimePattern();
                            p.Decode(body, bodyPos);
                            pattern = p;
                            break;
                        }
                    default:
                        throw new TemplateParseException(
                            "无法识别的表达式类型（|* 数字、|? 字母、|\" 时间）", bodyPos + 1);
                }
            }
            else if (body == RenamePattern.PATTERN_SEPARATOR)
            {
                pattern = new SeparatorPattern();
            }
            else if (first == RenamePattern.PATTERN_SEPARATOR[0])
            {
                throw new TemplateParseException(
                    "条件分隔符只能是 <.>（点号后不能有其他字符）", bodyPos + 1);
            }
            else
            {
                throw new TemplateParseException(
                    "表达式必须以 *、?、| 开头（条件分隔符为 <.>）：" + body, bodyPos);
            }

            pattern.Pattern = raw;
            return pattern;
        }

        /// <summary>
        /// 读取非负整数（连续数字）
        /// </summary>
        internal static long ReadDigits(string s, ref int i, int basePos, string what)
        {
            int start = i;
            while (i < s.Length && char.IsDigit(s[i]))
            {
                i++;
            }
            if (i == start)
            {
                throw new TemplateParseException("缺少" + what, basePos + i);
            }

            if (!long.TryParse(s.Substring(start, i - start), NumberStyles.None,
                CultureInfo.InvariantCulture, out long value))
            {
                throw new TemplateParseException(what + "数值超出范围", basePos + start);
            }
            return value;
        }

        /// <summary>
        /// 读取带符号整数（允许负号）
        /// </summary>
        internal static long ReadSignedInt(string s, ref int i, int basePos, string what)
        {
            int start = i;
            if (i < s.Length && s[i] == '-')
            {
                i++;
            }

            long value = ReadDigits(s, ref i, basePos, what);
            if (start < i && s[start] == '-')
            {
                value = -value;
            }
            return value;
        }

        /// <summary>
        /// 要求当前位置为指定字符并跳过
        /// </summary>
        internal static void ExpectChar(string s, ref int i, char expected, int basePos)
        {
            if (i >= s.Length || s[i] != expected)
            {
                throw new TemplateParseException("应为 '" + expected + "'", basePos + i);
            }
            i++;
        }

        /// <summary>
        /// 要求表达式已读取到末尾
        /// </summary>
        internal static void RequireEnd(string s, int i, int basePos)
        {
            if (i < s.Length)
            {
                throw new TemplateParseException("表达式存在无法识别的尾部内容：" + s.Substring(i), basePos + i);
            }
        }
    }
}
