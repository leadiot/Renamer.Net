using System.Collections.Generic;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 预览结果：包含每个文件的状态、阻断性问题与提示。
    /// CanCommit 为 true 时方可调用 Commit。
    /// </summary>
    public class RenamePlan
    {
        public RenamePlan(List<FileItem> items)
        {
            Items = items ?? new List<FileItem>();
            Issues = new List<RenameIssue>();
            Warnings = new List<RenameWarning>();
        }

        /// <summary>
        /// 参与预览的文件（DstName/Status/IssueMessage 已就地更新）
        /// </summary>
        public List<FileItem> Items { get; }

        /// <summary>
        /// 阻断性问题；为空才允许提交
        /// </summary>
        public List<RenameIssue> Issues { get; }

        /// <summary>
        /// 非阻断性提示
        /// </summary>
        public List<RenameWarning> Warnings { get; }

        /// <summary>
        /// 是否可以提交
        /// </summary>
        public bool CanCommit
        {
            get { return Issues.Count == 0; }
        }
    }
}
