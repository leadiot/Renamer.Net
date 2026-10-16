namespace Com.Scm.Renamer
{
    /// <summary>
    /// 一条阻断性问题
    /// </summary>
    public class RenameIssue
    {
        public RenameIssue(FileItem item, IssueKind kind, string message, string otherPath = null)
        {
            Item = item;
            Kind = kind;
            Message = message;
            OtherPath = otherPath;
        }

        /// <summary>
        /// 出问题的文件
        /// </summary>
        public FileItem Item { get; }

        /// <summary>
        /// 问题类型
        /// </summary>
        public IssueKind Kind { get; }

        /// <summary>
        /// 可展示的问题描述
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 冲突另一方的完整路径（如有）
        /// </summary>
        public string OtherPath { get; }

        public override string ToString()
        {
            return "[" + Kind + "] " + Message;
        }
    }
}
