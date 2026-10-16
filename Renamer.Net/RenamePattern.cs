namespace Com.Scm.Renamer
{
    /// <summary>
    /// 模式基类
    /// </summary>
    public class RenamePattern
    {
        /// <summary>
        /// 模式起始标记
        /// </summary>
        public const char PATTERN_START = '<';
        /// <summary>
        /// 模式结束标记
        /// </summary>
        public const char PATTERN_END = '>';

        /// <summary>
        /// 方向：左起标识
        /// </summary>
        public const char PATTERN_DIR_LEFT = '\\';
        /// <summary>
        /// 方向：右起标识
        /// </summary>
        public const char PATTERN_DIR_RIGHT = '/';

        /// <summary>
        /// 分隔标记
        /// </summary>
        public const char PATTERN_SPLIT = ':';
        /// <summary>
        /// 转义标记
        /// </summary>
        public const char PATTERN_ESCAPE = '|';

        /// <summary>
        /// 操作：转大写
        /// </summary>
        public const char PATTERN_OPT_UPPER = '\\';
        /// <summary>
        /// 操作：转小写
        /// </summary>
        public const char PATTERN_OPT_LOWER = '/';

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
        /// 标记：文件名
        /// </summary>
        public const string PATTERN_PREFIX = "*";
        /// <summary>
        /// 标记：扩展名
        /// </summary>
        public const string PATTERN_SUFFIX = "?";
        /// <summary>
        /// 标记：点号
        /// 有扩展名时输出点，无扩展名时输出空串
        /// </summary>
        public const string PATTERN_SEPARATOR = ".";
        /// <summary>
        /// 标记：预留
        /// </summary>
        public const string PATTERN_RESERVED = "\"";

        /// <summary>
        /// 标记：数字
        /// </summary>
        public const string PATTERN_NUMBER = "|*";
        /// <summary>
        /// 标记：字母
        /// </summary>
        public const string PATTERN_LETTER = "|?";
        /// <summary>
        /// 标记：时间
        /// </summary>
        public const string PATTERN_TIME = "|\"";
        /// <summary>
        /// 当前时间（无来源标记，&lt;|"&gt;）
        /// </summary>
        public const string PATTERN_TIME_CURRENT = "|\"";
        /// <summary>
        /// 文件创建时间（&lt;|"c&gt;）
        /// </summary>
        public const string PATTERN_TIME_CREATE = "|\"c";
        /// <summary>
        /// 文件修改时间（&lt;|"m&gt;）
        /// </summary>
        public const string PATTERN_TIME_MODIFY = "|\"m";
        /// <summary>
        /// 文件访问时间（&lt;|"a&gt;）
        /// </summary>
        public const string PATTERN_TIME_ACCESS = "|\"a";

        /// <summary>
        /// 时间默认展示格式
        /// </summary>
        public const string PATTERN_TIME_DEFAULT_FORMAT = "yyyyMMddHHmmss";

        public PatternType Type { get; protected internal set; }

        public string Title { get; protected internal set; }

        public string Pattern { get; protected internal set; }

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
        public virtual bool Decode(string body, int bodyPos)
        {
            return true;
        }

        /// <summary>
        /// 按表达式生成名称片段并追加到 DstName。
        /// 每个表达式只负责追加自己的输出，返回 false 或抛异常表示求值失败。
        /// </summary>
        public virtual bool GenName(FileItem item)
        {
            return true;
        }

        protected string ToPattern(string pattern)
        {
            return PATTERN_START + pattern + PATTERN_END;
        }
    }
}
