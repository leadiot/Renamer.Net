using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    internal class LetterPattern : RenamerPattern
    {
        public LetterPattern()
        {
            Type = PatternType.Letter;
            Title = "字母序列";
            Pattern = "<|?>";
            Start = "a";
            Length = 0;
        }

        private string _Start;
        public string Start
        {
            get
            {
                return _Start;
            }
            set
            {
                _Start = value;
            }
        }

        private int _Length;
        public int Length
        {
            get
            {
                return _Length;
            }
            set
            {
                _Length = value;
            }
        }

        public override void Encode()
        {
            var pattern = "\"" + Length + "," + Start;
            Pattern = "<" + pattern + ">";
        }
    }
}
