using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    internal class TimePattern : RenamerPattern
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        public const int OPTION_0 = 0;
        /// <summary>
        /// 修改时间
        /// </summary>
        public const int OPTION_1 = 1;
        /// <summary>
        /// 访问时间
        /// </summary>
        public const int OPTION_2 = 2;
        /// <summary>
        /// 当前时间
        /// </summary>
        public const int OPTION_3 = 3;

        /// <summary>
        /// 时间戳
        /// </summary>
        public const int FORMAT_0 = 0;
        /// <summary>
        /// 长格式
        /// </summary>
        public const int FORMAT_1 = 1;
        /// <summary>
        /// 自定义
        /// </summary>
        public const int FORMAT_2 = 2;

        public TimePattern()
        {
            Type = PatternType.Time;
            Title = "时间信息";
            Pattern = "<|\">";
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

        private int _Format;
        public int Format
        {
            get
            {
                return _Format;
            }
            set
            {
                _Format = value;
            }
        }

        private string _Input;
        public string Input
        {
            get
            {
                return _Input;
            }
            set
            {
                _Input = value;
            }
        }

        public override void Encode()
        {
            var pattern = "";
            switch (Option)
            {
                case OPTION_0:
                    pattern += "<\\";
                    break;
                case OPTION_1:
                    pattern += "</";
                    break;
                case OPTION_2:
                    pattern += "<|";
                    break;
                case OPTION_3:
                    pattern += "<\"";
                    break;
                default:
                    return;
            }

            switch (Format)
            {
                case FORMAT_0:
                    pattern += "l>";
                    break;
                case FORMAT_1:
                    pattern += "t>";
                    break;
                case FORMAT_2:
                    pattern = Input;
                    break;
            }

            Pattern = ToPattern(pattern);
        }
    }
}
