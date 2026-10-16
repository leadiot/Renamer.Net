namespace Com.Scm.Renamer
{

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
        Auto,
    }
}
