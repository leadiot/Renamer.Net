namespace Com.Scm.Renamer.Patterns
{
    /// <summary>
    /// 固定文本
    /// </summary>
    public class TextPattern : RenamePattern
    {
        public TextPattern()
        {
            Type = PatternType.Text;
            Title = "固定文本";
        }

        public string Text { get; set; }

        public override void Encode()
        {
            Pattern = Text;
        }

        public override bool GenName(FileItem item)
        {
            item.DstName += Text ?? string.Empty;
            return true;
        }
    }
}
