using Com.Scm.Renamer;
using System.Globalization;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    /// <summary>
    /// 数字序列。
    /// 语法：|*[length][:start[:step]]
    /// length 为最小位宽，不足左侧补 0；超出位宽时原样输出（不截断）。
    /// start 默认 0，step 默认 1，均支持负数；序列值 = start + step * 文件序号。
    /// </summary>
    internal class NumberPattern : RenamerPattern
    {
        public NumberPattern()
        {
            Type = PatternType.Number;
            Title = "数字序列";
            Step = 1;
        }

        /// <summary>
        /// 最小位宽（左侧补 0），0 表示不补齐
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 起始值
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
            if (body == null || !body.StartsWith(PATTERN_NUMBER))
            {
                throw new TemplateParseException("数字序列表达式必须以 |* 开头", bodyPos);
            }

            int i = PATTERN_NUMBER.Length;
            if (i < body.Length && char.IsDigit(body[i]))
            {
                Length = (int)TemplateParser.ReadDigits(body, ref i, bodyPos, "位宽");
            }

            Start = 0;
            Step = 1;
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
            TemplateParser.RequireEnd(body, i, bodyPos);
            return true;
        }

        public override void Encode()
        {
            var pattern = PATTERN_NUMBER;
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
            long value = Start + (long)Step * item.Index;

            bool negative = value < 0;
            string digits = (negative ? -value : value).ToString(CultureInfo.InvariantCulture);
            if (Length > 0)
            {
                int width = negative && Length > 1 ? Length - 1 : Length;
                if (digits.Length < width)
                {
                    digits = digits.PadLeft(width, '0');
                }
            }

            item.DstName += negative ? "-" + digits : digits;
            return true;
        }
    }
}
