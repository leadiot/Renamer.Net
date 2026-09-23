using Com.Scm.Renamer;
using System.Globalization;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    /// <summary>
    /// 时间信息。
    /// 语法：|"[source][:format]
    /// source 缺省为当前时间；c=创建时间、m=修改时间、a=访问时间。
    /// 来源标记仅在“单独出现”或“紧跟 :”时才被识别，因此自定义格式必须写在 : 之后，
    /// 例如 |"c:yyyy-MM-dd、|":yyyy-MM-dd。缺省格式为 yyyyMMddHHmmss。
    /// </summary>
    internal class TimePattern : RenamerPattern
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
        internal void DecodeBody(string body, int bodyPos)
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
                return;
            }

            int i = 0;
            // 来源标记：单独出现，或后面紧跟 ':'
            if ((rest.Length == 1 || rest[1] == PATTERN_SPLIT) && TryParseSource(rest[0], out SourceEnum source))
            {
                Source = source;
                i = 1;
                if (i == rest.Length)
                {
                    return;
                }
            }
            else if (rest[0] != PATTERN_SPLIT)
            {
                throw new TemplateParseException(
                    "无法识别的时间来源（c 创建、m 修改、a 访问），自定义格式前请加 :", bodyPos + PATTERN_TIME.Length);
            }

            TemplateParser.ExpectChar(rest, ref i, PATTERN_SPLIT, bodyPos + PATTERN_TIME.Length);
            Format = rest.Substring(i);
            if (Format.Length == 0)
            {
                throw new TemplateParseException("时间格式不能为空", bodyPos + body.Length);
            }
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

    internal enum SourceEnum
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
