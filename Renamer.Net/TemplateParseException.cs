using System;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 模板解析错误。Position 为出错字符在整个模板中的 0 基位置。
    /// </summary>
    public class TemplateParseException : Exception
    {
        /// <summary>
        /// 出错位置（0 基）
        /// </summary>
        public int Position { get; }

        public TemplateParseException(string message, int position)
            : base(message)
        {
            Position = position;
        }
    }
}
