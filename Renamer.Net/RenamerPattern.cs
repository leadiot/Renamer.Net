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
        /// 时间表达式前缀
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
        /// <summary>
        /// 条件分隔符 &lt;.&gt;：有扩展名时输出点，无扩展名时输出空串
        /// </summary>
        public const string PATTERN_SEPARATOR = ".";

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
