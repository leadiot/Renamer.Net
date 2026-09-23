using System;

namespace Com.Scm.Renamer
{
    public class FileItem
    {
        /// <summary>
        /// 文件路径，完整路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 原始名称（含扩展名）
        /// </summary>
        public string SrcName { get; set; }

        /// <summary>
        /// 在本批次中的序号（0 基），数字/字母流水按此值计算
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 文件创建时间（Path 不可读时取当前时间）
        /// </summary>
        public DateTime CreationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 文件修改时间（Path 不可读时取当前时间）
        /// </summary>
        public DateTime LastWriteTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 文件访问时间（Path 不可读时取当前时间）
        /// </summary>
        public DateTime LastAccessTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最终名称（AutoSuffix 消解冲突后为消解后的名称）
        /// </summary>
        public string DstName { get; set; }

        /// <summary>
        /// 预览/提交状态
        /// </summary>
        public RenameStatus Status { get; set; } = RenameStatus.Ok;

        /// <summary>
        /// 状态说明（自动消解说明或错误描述）
        /// </summary>
        public string IssueMessage { get; set; }
    }
}
