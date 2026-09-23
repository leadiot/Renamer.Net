using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo
{
    public class RenamerPattern
    {
        public const string PATTERN_START = "<";
        public const string PATTERN_END = ">";
        /// <summary>
        /// 文件前缀
        /// </summary>
        public const string PATTERN_PREFIX = "*";
        /// <summary>
        /// 文件后缀
        /// </summary>
        public const string PATTERN_SUFFIX = "?";
        /// <summary>
        /// 预留
        /// </summary>
        public const string PATTERN_RESERVED_1 = "\"";
        /// <summary>
        /// 数字
        /// </summary>
        public const string PATTERN_NUMBER = "|*";
        /// <summary>
        /// 字母
        /// </summary>
        public const string PATTERN_LETTER = "|?";
        /// <summary>
        /// 预留
        /// </summary>
        public const string PATTERN_TIME = "|\"";
        public const string PATTERN_TEXT = "?";

        public PatternType Type { get; protected set; }

        public string Title { get; protected set; }

        public string Pattern { get; protected set; }

        public virtual void Encode()
        {
        }

        public virtual bool Decode()
        {
            return true;
        }
    }
}
