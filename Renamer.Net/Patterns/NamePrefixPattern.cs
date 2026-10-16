using Com.Scm.Renamer.Pattern;

namespace Com.Scm.Renamer.Patterns
{
    /// <summary>
    /// 文件名称前缀表达式
    /// </summary>
    public class NamePrefixPattern : NamePattern
    {
        /// <summary>
        /// 保持不变
        /// </summary>
        public const int OPTION_0 = 0;
        /// <summary>
        /// 左侧截取
        /// </summary>
        public const int OPTION_1 = 1;
        /// <summary>
        /// 右侧截取
        /// </summary>
        public const int OPTION_2 = 2;

        public NamePrefixPattern()
        {
            Type = PatternType.Prefix;
            Title = "文件前缀";
            IsExtension = false;
        }

        private int _Option;
        public int Option
        {
            get
            {
                return _Option;
            }
            set
            {
                _Option = value;
            }
        }

        public override void Encode()
        {
            var pattern = GenPattern(PATTERN_PREFIX);

            Pattern = ToPattern(pattern);
        }
    }
}
