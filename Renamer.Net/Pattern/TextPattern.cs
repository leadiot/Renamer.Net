using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    internal class TextPattern : RenamerPattern
    {
        public TextPattern()
        {
            Type = PatternType.Text;
            Title = "固定文本";
            Pattern = "文本";
        }

        public string Text { get; set; }

        public override void Encode()
        {
            Pattern = Text;
        }
    }
}
