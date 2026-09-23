using System.Collections.Generic;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 单个文件在预览/提交后的状态
    /// </summary>
    public enum RenameStatus
    {
        /// <summary>
        /// 可正常重命名
        /// </summary>
        Ok,
        /// <summary>
        /// 目标名与原名相同，提交时跳过
        /// </summary>
        NoChange,
        /// <summary>
        /// 存在冲突，已按 AutoSuffix 策略自动消解（DstName 已更新）
        /// </summary>
        Resolved,
        /// <summary>
        /// 求值或校验失败，不可提交
        /// </summary>
        Error,
    }

    /// <summary>
    /// 冲突处理策略
    /// </summary>
    public enum ConflictMode
    {
        /// <summary>
        /// 发现冲突即作为问题上报，不自动消解
        /// </summary>
        Fail,
        /// <summary>
        /// 自动在扩展名前追加“ (n)”后缀消解冲突，消解记录为警告
        /// </summary>
        AutoSuffix,
    }

    /// <summary>
    /// 问题类型（Issues 中的条目均阻断提交）
    /// </summary>
    public enum IssueKind
    {
        /// <summary>
        /// 表达式求值异常
        /// </summary>
        EvalError,
        /// <summary>
        /// 目标文件名为空
        /// </summary>
        EmptyName,
        /// <summary>
        /// 目标文件名含操作系统非法字符
        /// </summary>
        InvalidChar,
        /// <summary>
        /// 批次内多个文件生成了相同目标名（Windows 下大小写不同也算冲突）
        /// </summary>
        DuplicateTarget,
        /// <summary>
        /// 目标路径已被批次外的现有文件占用
        /// </summary>
        TargetExists,
        /// <summary>
        /// 提交阶段磁盘 IO 失败
        /// </summary>
        IoError,
    }

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

    /// <summary>
    /// 一条非阻断性提示（如冲突已被 AutoSuffix 自动消解）
    /// </summary>
    public class RenameWarning
    {
        public RenameWarning(FileItem item, string message)
        {
            Item = item;
            Message = message;
        }

        public FileItem Item { get; }

        public string Message { get; }

        public override string ToString()
        {
            return Message;
        }
    }

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
