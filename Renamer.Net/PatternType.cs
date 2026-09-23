namespace Com.Scm.Renamer
{
    public enum PatternType
    {
        None,
        /// <summary>
        /// 文本
        /// </summary>
        Text,
        /// <summary>
        /// 前缀
        /// </summary>
        Prefix,
        /// <summary>
        /// 后缀
        /// </summary>
        Suffix,
        /// <summary>
        /// 时间
        /// </summary>
        Time,
        /// <summary>
        /// 数字
        /// </summary>
        Number,
        /// <summary>
        /// 字母
        /// </summary>
        Letter,
        /// <summary>
        /// 条件分隔符（有扩展名时输出点）
        /// </summary>
        Separator,
    }
}
