using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    internal class SuffixPattern : RenamerPattern
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

        public SuffixPattern()
        {
            Type = PatternType.Suffix;
            Title = "文件后缀";
            Pattern = "<?>";
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

        private int _Length;
        public int Length
        {
            get
            {
                return _Length;
            }
            set
            {
                _Length = value;
            }
        }

        public override void Encode()
        {
            var pattern = "?";
            switch (Option)
            {
                case 0:
                    break;
                case OPTION_1:
                    pattern += "\\" + Length;
                    break;
                case OPTION_2:
                    pattern += "/" + Length;
                    break;
                default:
                    return;
            }

            Pattern = "<" + pattern + ">";
        }
    }
}
