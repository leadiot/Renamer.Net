using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo
{
    /// <summary>
    /// 表达式基类
    /// </summary>
    public class RenamerPattern
    {
        public const char PATTERN_START = '<';
        public const char PATTERN_END = '>';

        /// <summary>
        /// 左起标识
        /// </summary>
        public const char PATTERN_DIR_LEFT = '\\';
        /// <summary>
        /// 右起标识
        /// </summary>
        public const char PATTERN_DIR_RIGHT = '/';

        /// <summary>
        /// 转大写
        /// </summary>
        public const char PATTERN_OPT_UPPER = '\\';
        /// <summary>
        /// 转小写
        /// </summary>
        public const char PATTERN_OPT_LOWER = '/';

        /// <summary>
        /// 分隔标记
        /// </summary>
        public const char PATTERN_SPLIT = ':';
        /// <summary>
        /// 转义
        /// </summary>
        public const char PATTERN_ESCAPE = '|';

        /// <summary>
        /// 操作：追加
        /// </summary>
        public const char PATTERN_OPT_INSERT = '\\';
        /// <summary>
        /// 操作：删除
        /// </summary>
        public const char PATTERN_OPT_DELETE = '/';
        /// <summary>
        /// 操作：替换
        /// </summary>
        public const char PATTERN_OPT_REPLACE = '\\';

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

        public PatternType Type { get; protected set; }

        public string Title { get; protected set; }

        public string Pattern { get; protected set; }

        /// <summary>
        /// 表达式生成
        /// </summary>
        public virtual void Encode()
        {
        }

        /// <summary>
        /// 表达式解析
        /// </summary>
        /// <returns></returns>
        public virtual bool Decode()
        {
            return true;
        }

        public virtual bool GenName(FileItem item)
        {
            item.DstName += "";
            return false;
        }

        protected string ToPattern(string pattern)
        {
            return PATTERN_START + pattern + PATTERN_END;
        }
    }
}
