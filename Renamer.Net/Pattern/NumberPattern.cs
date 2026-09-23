using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    internal class NumberPattern : RenamerPattern
    {
        public NumberPattern()
        {
            Type = PatternType.Number;
            Title = "数字序列";
            Pattern = "<|*>";
            Start = 0;
            Length = 0;
        }

        private int _Start;
        public int Start
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
            var pattern = ":" + Length + "," + Start;
            Pattern = "<" + pattern + ">";
        }
    }
}
