namespace Com.Scm.Renamer
{
    public class FileItem
    {
        /// <summary>
        /// 文件路径，完整路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 原始名称
        /// </summary>
        public string SrcName { get; set; }

        /// <summary>
        /// 最终名称
        /// </summary>
        public string DstName { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
