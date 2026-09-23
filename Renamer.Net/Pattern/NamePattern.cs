using Com.Scm.Filexp.Rename.Dvo;

namespace Com.Scm.Renamer.Pattern
{
    /// <summary>
    /// 文件名称表达式
    /// </summary>
    internal class NamePattern : RenamerPattern
    {
        /// <summary>
        /// 方向
        /// </summary>
        public DirEnum Dir { get; set; }

        /// <summary>
        /// 操作
        /// </summary>
        public OptEnum Opt { get; set; }

        /// <summary>
        /// 转小写
        /// </summary>
        public bool ToLower { get; set; }
        /// <summary>
        /// 转大写
        /// </summary>
        public bool ToUpper { get; set; }

        /// <summary>
        /// 起始位置
        /// </summary>
        public int From { get; set; }
        /// <summary>
        /// 结束位置
        /// </summary>
        public int To { get; set; }

        /// <summary>
        /// 字符长度
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 来源文本
        /// </summary>
        public string SrcText { get; set; }
        /// <summary>
        /// 目标文本
        /// </summary>
        public string DstText { get; set; }

        protected string GenPattern(string tag)
        {
            var pattern = tag;
            if (ToUpper)
            {
                pattern += PATTERN_ESCAPE + PATTERN_OPT_UPPER;
                return pattern;
            }
            if (ToLower)
            {
                pattern += PATTERN_ESCAPE + PATTERN_OPT_LOWER;
                return pattern;
            }

            if (Opt == OptEnum.Insert)
            {
                pattern += GetDir();
                pattern += From;
                pattern += PATTERN_OPT_INSERT + DstText;
                return pattern;
            }

            if (Opt == OptEnum.Delete)
            {
                pattern += GetDir();
                pattern += From;
                pattern += PATTERN_OPT_DELETE + Length;
                return pattern;
            }

            if (Opt == OptEnum.Cut)
            {
                pattern += GetDir();
                pattern += From;
                pattern += PATTERN_SPLIT + To;
                return pattern;
            }

            if (Opt == OptEnum.Replace1)
            {
                pattern += GetDir();
                pattern += From;
                pattern += PATTERN_SPLIT + To;
                pattern += PATTERN_OPT_REPLACE + DstText;
                return pattern;
            }

            if (Opt == OptEnum.Replace2)
            {
                pattern += SrcText;
                pattern += PATTERN_OPT_REPLACE + DstText;
                return pattern;
            }
            return tag;
        }

        protected char GetDir()
        {
            if (Dir == DirEnum.Left)
            {
                return PATTERN_DIR_LEFT;
            }
            if (Dir == DirEnum.Right)
            {
                return PATTERN_DIR_RIGHT;
            }
            return ' ';
        }
    }

    public enum DirEnum
    {
        None,
        Left,
        Right,
    }

    public enum OptEnum
    {
        /// <summary>
        /// 截取
        /// </summary>
        Cut,
        /// <summary>
        /// 追加
        /// </summary>
        Insert,
        /// <summary>
        /// 删除
        /// </summary>
        Delete,
        /// <summary>
        /// 按区间替换
        /// </summary>
        Replace1,
        /// <summary>
        /// 按文字替换
        /// </summary>
        Replace2
    }
}
