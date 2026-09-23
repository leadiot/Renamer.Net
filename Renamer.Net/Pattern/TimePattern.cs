using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    /// <summary>
    /// 时间信息
    /// </summary>
    internal class TimePattern : RenamerPattern
    {
        public TimePattern()
        {
            Type = PatternType.Time;
            Title = "时间信息";
        }

        /// <summary>
        /// 时间来源
        /// </summary>
        public SourceEnum Source { get; set; }

        /// <summary>
        /// 时间格式
        /// </summary>
        public string Format { get; set; }

        public override void Encode()
        {
            var pattern = PATTERN_TIME;
            switch (Source)
            {
                case SourceEnum.CurrentTime:
                    pattern = PATTERN_TIME_CURRENT;
                    break;
                case SourceEnum.CreateTime:
                    pattern = PATTERN_TIME_CREATE;
                    break;
                case SourceEnum.ModifyTime:
                    pattern = PATTERN_TIME_MODIFY;
                    break;
                case SourceEnum.AccessTime:
                    pattern = PATTERN_TIME_ACCESS;
                    break;
            }

            if (!string.IsNullOrEmpty(Format))
            {
                pattern += PATTERN_SPLIT + Format;
            }

            Pattern = ToPattern(pattern);
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
