using Com.Scm.Renamer;
using Com.Scm.Renamer.Pattern;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    /// <summary>
    /// 条件分隔符 &lt;.&gt;：原文件存在扩展名时输出一个点，否则输出空串。
    /// 扩展名判定与 NamePattern.SplitName 完全一致（取最后一个点，
    /// 点开头文件 .gitignore 与无点文件视为无扩展名）。
    /// 典型用法：&lt;*&gt;&lt;.&gt;&lt;?&gt;，无扩展名文件不会产生尾随点。
    /// </summary>
    public class SeparatorPattern : RenamerPattern
    {
        public SeparatorPattern()
        {
            Type = PatternType.Separator;
            Title = "条件分隔符";
        }

        public override void Encode()
        {
            Pattern = ToPattern(PATTERN_SEPARATOR);
        }

        public override bool GenName(FileItem item)
        {
            NamePattern.SplitName(item.SrcName ?? string.Empty, out _, out string extension);
            if (extension.Length > 0)
            {
                item.DstName += PATTERN_SEPARATOR;
            }
            return true;
        }
    }
}
