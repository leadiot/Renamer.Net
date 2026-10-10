using Com.Scm.Renamer;
using Com.Scm.Renamer.Pattern;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    /// <summary>
    /// 文件名称后缀表达式
    /// </summary>
    public class NameSuffixPattern : NamePattern
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

        public NameSuffixPattern()
        {
            Type = PatternType.Suffix;
            Title = "文件后缀";
            IsExtension = true;
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
            var pattern = GenPattern(PATTERN_SUFFIX);

            Pattern = ToPattern(pattern);
        }
    }
}
