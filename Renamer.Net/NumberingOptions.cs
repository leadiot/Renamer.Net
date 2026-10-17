using System;
using System.Collections.Generic;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 流水发号的排序依据。
    /// </summary>
    public enum NumberingOrderBy
    {
        /// <summary>保持传入列表顺序（默认）</summary>
        None = 0,

        /// <summary>按主文件名（自然排序：数字段按数值比较，如 2 排在 10 前）</summary>
        Name,

        /// <summary>按文件创建时间</summary>
        CreationTime,

        /// <summary>按文件修改时间</summary>
        LastWriteTime,

        /// <summary>按文件访问时间</summary>
        LastAccessTime
    }

    /// <summary>
    /// 流水发号策略：决定 <see cref="FileItem.Index"/> 的分配方式（发号顺序 + 是否按目录复位）。
    /// 由 <see cref="Renamer.Numbering"/> 承载，Preview/Commit 自动生效；为 null 时保持原有行为
    /// （按传入列表顺序全局连续发号）。
    /// </summary>
    public class NumberingOptions
    {
        /// <summary>发号排序依据，默认保持传入列表顺序</summary>
        public NumberingOrderBy OrderBy { get; set; } = NumberingOrderBy.None;

        /// <summary>升序（默认）；仅当 OrderBy 非 None 时生效</summary>
        public bool Ascending { get; set; } = true;

        /// <summary>按目录复位：每个目录从起始值独立发号；默认 false（全批次连续发号）</summary>
        public bool ResetPerDirectory { get; set; }
    }

    /// <summary>
    /// 时间排序比较器：路径不可读（时间为空）的文件恒排末尾，与排序方向无关。
    /// </summary>
    internal sealed class TimeKeyComparer : IComparer<DateTime?>
    {
        private readonly bool _ascending;

        public TimeKeyComparer(bool ascending)
        {
            _ascending = ascending;
        }

        public int Compare(DateTime? x, DateTime? y)
        {
            if (!x.HasValue && !y.HasValue)
            {
                return 0;
            }
            if (!x.HasValue)
            {
                return 1;
            }
            if (!y.HasValue)
            {
                return -1;
            }
            int c = x.Value.CompareTo(y.Value);
            return _ascending ? c : -c;
        }
    }
}
