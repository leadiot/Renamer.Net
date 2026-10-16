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
}
