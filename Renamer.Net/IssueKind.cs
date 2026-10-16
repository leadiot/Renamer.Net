namespace Com.Scm.Renamer
{
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
}
