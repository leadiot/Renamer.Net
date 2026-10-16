using System.Globalization;

namespace Com.Scm.Renamer.Patterns
{
    /// <summary>
    /// 时间信息。语法：
    /// |"                    当前时间，缺省格式
    /// |"c | |"m | |"a       创建/修改/访问时间，缺省格式
    /// |"c:format            指定来源 + 格式
    /// |":format             当前时间 + 格式（唯一写法，裸格式 |"format 已移除）
    /// 缺省格式为 yyyyMMddHHmmss。
    /// </summary>
    public class TimePattern : RenamePattern
    {
        public TimePattern()
        {
            Type = PatternType.Time;
            Title = "时间信息";
            Source = SourceEnum.CurrentTime;
        }

        /// <summary>
        /// 时间来源
        /// </summary>
        public SourceEnum Source { get; set; }

        /// <summary>
        /// 时间格式；null/空表示使用默认格式
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 解析表达式内容（不含 &lt; &gt;）
        /// </summary>
        public override bool Decode(string body, int bodyPos)
        {
            if (body == null || !body.StartsWith(PATTERN_TIME))
            {
                throw new TemplateParseException("时间表达式必须以 |\" 开头", bodyPos);
            }

            string rest = body.Substring(PATTERN_TIME.Length);
            Source = SourceEnum.CurrentTime;
            Format = null;

            if (rest.Length == 0)
            {
                return true;
            }

            int i = 0;
            // 1) 来源标记单独出现：|"c
            if (rest.Length == 1 && TryParseSource(rest[0], out SourceEnum onlySource))
            {
                Source = onlySource;
                return true;
            }

            // 2) 来源标记 + ':' + 格式：|"c:yyyy-MM-dd
            if (rest.Length > 1 && rest[1] == PATTERN_SPLIT && TryParseSource(rest[0], out SourceEnum withSource))
            {
                Source = withSource;
                i = 2;
            }
            else if (rest[0] == PATTERN_SPLIT)
            {
                // 3) 仅格式（当前时间）：|":yyyy-MM-dd
                i = 1;
            }
            else
            {
                // 裸格式已移除：自定义格式前必须加 ':'
                throw new TemplateParseException(
                    "时间表达式无法识别：来源请用 c/m/a，自定义格式前必须加 ':'，即 |\""
                    + PATTERN_SPLIT + rest, bodyPos + PATTERN_TIME.Length);
            }

            Format = rest.Substring(i);
            if (Format.Length == 0)
            {
                throw new TemplateParseException("时间格式不能为空", bodyPos + body.Length);
            }
            return true;
        }

        private static bool TryParseSource(char c, out SourceEnum source)
        {
            switch (c)
            {
                case 'c':
                    source = SourceEnum.CreateTime;
                    return true;
                case 'm':
                    source = SourceEnum.ModifyTime;
                    return true;
                case 'a':
                    source = SourceEnum.AccessTime;
                    return true;
                default:
                    source = SourceEnum.CurrentTime;
                    return false;
            }
        }

        public override void Encode()
        {
            string pattern;
            switch (Source)
            {
                case SourceEnum.CreateTime:
                    pattern = PATTERN_TIME_CREATE;
                    break;
                case SourceEnum.ModifyTime:
                    pattern = PATTERN_TIME_MODIFY;
                    break;
                case SourceEnum.AccessTime:
                    pattern = PATTERN_TIME_ACCESS;
                    break;
                default:
                    pattern = PATTERN_TIME_CURRENT;
                    break;
            }

            if (!string.IsNullOrEmpty(Format))
            {
                pattern += PATTERN_SPLIT + Format;
            }

            Pattern = ToPattern(pattern);
        }

        public override bool GenName(FileItem item)
        {
            System.DateTime time;
            switch (Source)
            {
                case SourceEnum.CreateTime:
                    time = item.CreationTime;
                    break;
                case SourceEnum.ModifyTime:
                    time = item.LastWriteTime;
                    break;
                case SourceEnum.AccessTime:
                    time = item.LastAccessTime;
                    break;
                default:
                    time = System.DateTime.Now;
                    break;
            }

            string format = string.IsNullOrEmpty(Format) ? PATTERN_TIME_DEFAULT_FORMAT : Format;
            item.DstName += time.ToString(format, CultureInfo.InvariantCulture);
            return true;
        }
    }

    public enum SourceEnum
    {
        /// <summary>
        /// 当前时间
        /// </summary>
        CurrentTime,
        /// <summary>
        /// 创建时间
        /// </summary>
        CreateTime,
        /// <summary>
        /// 修改时间
        /// </summary>
        ModifyTime,
        /// <summary>
        /// 访问时间
        /// </summary>
        AccessTime
    }
}
