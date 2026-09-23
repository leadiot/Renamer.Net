using Com.Scm.Renamer;
using System;
using System.Globalization;
using System.Text;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    /// <summary>
    /// 字母/字符序列。
    /// 语法（字符集必须显式指定，裸 |? 编译报错）：
    /// |?/[length][:start[:step]]       小写字母序列（a-z 为字符集，左侧补 a）
    /// |?\[length][:start[:step]]       大写字母序列（A-Z 为字符集，左侧补 A）
    /// |?charset[:length:start:step]    自定义字符集，charset 取到第一个 ':' 为止
    /// 序列按字符集长度做进制换算（a=0…z=25，其后为 ba、bb），length 为最小位宽，超出不截断。
    /// </summary>
    internal class LetterPattern : RenamerPattern
    {
        private static readonly char[] LowerAlphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] UpperAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        public LetterPattern()
        {
            Type = PatternType.Letter;
            Title = "字符序列";
            Step = 1;
        }

        /// <summary>
        /// 是否使用大写内置字符集（自定义字符集时忽略，按字符集原样输出）
        /// </summary>
        public bool IsUpper { get; set; }

        /// <summary>
        /// 自定义字符集；为 null 时使用内置大小写字母表
        /// </summary>
        public char[] Letters { get; set; }

        /// <summary>
        /// 最小位宽，0 表示不补齐
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 起始序数
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// 步增量（可为负）
        /// </summary>
        public int Step { get; set; } = 1;

        /// <summary>
        /// 解析表达式内容（不含 &lt; &gt;）
        /// </summary>
        public override bool Decode(string body, int bodyPos)
        {
            if (body == null || !body.StartsWith(PATTERN_LETTER))
            {
                throw new TemplateParseException("字母序列表达式必须以 |? 开头", bodyPos);
            }

            int i = PATTERN_LETTER.Length;
            Start = 0;
            Step = 1;

            // 必须显式指定大小写字母表或自定义字符集，不允许缺省
            if (i >= body.Length)
            {
                throw new TemplateParseException("字母序列必须指定字符集：小写使用 |?/，大写使用 |?\\，或直接给出自定义字符集", bodyPos + i);
            }

            if (body[i] == PATTERN_OPT_LOWER || body[i] == PATTERN_OPT_UPPER)
            {
                // 内置大小写字母表：|?/n 或 |?\n，定长直接跟数字、无冒号
                IsUpper = body[i] == PATTERN_OPT_UPPER;
                Letters = null;
                Title = IsUpper ? "大写字母序列" : "小写字母序列";
                i++;

                if (i < body.Length && char.IsDigit(body[i]))
                {
                    Length = (int)TemplateParser.ReadDigits(body, ref i, bodyPos, "位宽");
                }
                if (i < body.Length)
                {
                    TemplateParser.ExpectChar(body, ref i, PATTERN_SPLIT, bodyPos);
                    Start = (int)TemplateParser.ReadSignedInt(body, ref i, bodyPos, "起始值");
                    if (i < body.Length)
                    {
                        TemplateParser.ExpectChar(body, ref i, PATTERN_SPLIT, bodyPos);
                        Step = (int)TemplateParser.ReadSignedInt(body, ref i, bodyPos, "步增量");
                    }
                }
            }
            else
            {
                // 自定义字符集：|?abcd[:length:start:step]
                int colon = body.IndexOf(PATTERN_SPLIT, i);
                string charset = colon < 0 ? body.Substring(i) : body.Substring(i, colon - i);
                if (charset.Length == 0)
                {
                    throw new TemplateParseException("自定义字符集不能为空", bodyPos + i);
                }
                Letters = charset.ToCharArray();
                Title = "自定义字符序列";

                if (colon >= 0)
                {
                    i = colon + 1;
                    if (i < body.Length && char.IsDigit(body[i]))
                    {
                        Length = (int)TemplateParser.ReadDigits(body, ref i, bodyPos, "位宽");
                    }
                    if (i < body.Length)
                    {
                        TemplateParser.ExpectChar(body, ref i, PATTERN_SPLIT, bodyPos);
                        Start = (int)TemplateParser.ReadSignedInt(body, ref i, bodyPos, "起始值");
                        if (i < body.Length)
                        {
                            TemplateParser.ExpectChar(body, ref i, PATTERN_SPLIT, bodyPos);
                            Step = (int)TemplateParser.ReadSignedInt(body, ref i, bodyPos, "步增量");
                        }
                    }
                }
            }

            TemplateParser.RequireEnd(body, i, bodyPos);
            return true;
        }

        public override void Encode()
        {
            var pattern = PATTERN_LETTER;
            if (Letters != null && Letters.Length > 0)
            {
                pattern += string.Concat(Letters);
                if (Length > 0 || Start != 0 || Step != 1)
                {
                    pattern += PATTERN_SPLIT
                        + Length.ToString(CultureInfo.InvariantCulture) + PATTERN_SPLIT
                        + Start.ToString(CultureInfo.InvariantCulture) + PATTERN_SPLIT
                        + Step.ToString(CultureInfo.InvariantCulture);
                }
                Pattern = ToPattern(pattern);
                return;
            }

            pattern += IsUpper ? PATTERN_OPT_UPPER : PATTERN_OPT_LOWER;
            if (Length > 0)
            {
                pattern += Length.ToString(CultureInfo.InvariantCulture);
            }
            if (Start != 0)
            {
                pattern += PATTERN_SPLIT + Start.ToString(CultureInfo.InvariantCulture);
            }
            if (Step != 1)
            {
                pattern += PATTERN_SPLIT + Step.ToString(CultureInfo.InvariantCulture);
            }
            Pattern = ToPattern(pattern);
        }

        public override bool GenName(FileItem item)
        {
            char[] alphabet = Letters ?? (IsUpper ? UpperAlphabet : LowerAlphabet);
            long value = Start + (long)Step * item.Index;
            if (value < 0)
            {
                throw new InvalidOperationException("字母序列值不能为负数：" + value);
            }

            int radix = alphabet.Length;
            var sb = new StringBuilder();
            do
            {
                sb.Insert(0, alphabet[(int)(value % radix)]);
                value /= radix;
            } while (value > 0);

            if (Length > sb.Length)
            {
                sb.Insert(0, new string(alphabet[0], Length - sb.Length));
            }

            item.DstName += sb.ToString();
            return true;
        }
    }

    internal class LowerLetterPattern : LetterPattern
    {
        public LowerLetterPattern()
        {
            Type = PatternType.Letter;
            Title = "小写字母序列";
            IsUpper = false;
        }
    }

    internal class UpperLetterPattern : LetterPattern
    {
        public UpperLetterPattern()
        {
            Type = PatternType.Letter;
            Title = "大写字母序列";
            IsUpper = true;
        }
    }
}
