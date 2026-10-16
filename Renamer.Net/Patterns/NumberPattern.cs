using System.Globalization;

namespace Com.Scm.Renamer.Patterns
{
    /// <summary>
    /// 数字序列。
    /// 语法：|*[length][:start[:step]]
    /// length 为最小位宽，不足左侧补 0；超出位宽时原样输出（不截断）。
    /// start 默认 1，step 默认 1，均仅支持自然数（≥ 1）；序列值 = start + step * 文件序号。
    /// </summary>
    public class NumberPattern : RenamePattern
    {
        public NumberPattern()
        {
            Type = PatternType.Number;
            Title = "数字序列";
            Start = 1;
            Step = 1;
        }

        /// <summary>
        /// 最小位宽（左侧补 0），0 表示不补齐
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 起始值（自然数，≥ 1）
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// 步增量（自然数，≥ 1）
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

            Start = 1;
            Step = 1;
            if (i < body.Length)
            {
                TemplateParser.ExpectChar(body, ref i, PATTERN_SPLIT, bodyPos);
                int startPos = i;
                Start = (int)TemplateParser.ReadDigits(body, ref i, bodyPos, "起始值");
                if (Start < 1)
                {
                    throw new TemplateParseException("起始值必须是自然数（≥ 1）", bodyPos + startPos);
                }
                if (i < body.Length)
                {
                    TemplateParser.ExpectChar(body, ref i, PATTERN_SPLIT, bodyPos);
                    int stepPos = i;
                    Step = (int)TemplateParser.ReadDigits(body, ref i, bodyPos, "步增量");
                    if (Step < 1)
                    {
                        throw new TemplateParseException("步增量必须是自然数（≥ 1）", bodyPos + stepPos);
                    }
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
            if (Start != 1)
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
            // start、step 均为自然数，序列值恒为正
            long value = Start + (long)Step * item.Index;
            string digits = value.ToString(CultureInfo.InvariantCulture);
            if (Length > 0 && digits.Length < Length)
            {
                digits = digits.PadLeft(Length, '0');
            }

            item.DstName += digits;
            return true;
        }
    }
}
