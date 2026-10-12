using System;

namespace Com.Scm.Renamer
{
    public interface FileItem
    {
        /// <summary>
        /// 文件路径，完整路径
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// 原始名称（含扩展名）
        /// </summary>
        string SrcName { get; set; }

        /// <summary>
        /// 在本批次中的序号（0 基），数字/字母流水按此值计算
        /// </summary>
        int Index { get; set; }

        /// <summary>
        /// 文件创建时间（Path 不可读时取当前时间）
        /// </summary>
        DateTime CreationTime { get; set; }

        /// <summary>
        /// 文件修改时间（Path 不可读时取当前时间）
        /// </summary>
        DateTime LastWriteTime { get; set; }

        /// <summary>
        /// 文件访问时间（Path 不可读时取当前时间）
        /// </summary>
        DateTime LastAccessTime { get; set; }

        /// <summary>
        /// 最终名称（AutoSuffix 消解冲突后为消解后的名称）
        /// </summary>
        string DstName { get; set; }

        /// <summary>
        /// 预览/提交状态
        /// </summary>
        RenameStatus Status { get; set; }

        /// <summary>
        /// 状态说明（自动消解说明或错误描述）
        /// </summary>
        string IssueMessage { get; set; }
    }
}
