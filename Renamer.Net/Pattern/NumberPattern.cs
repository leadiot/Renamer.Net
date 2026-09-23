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

        private int _Step;
        public int Step
        {
            get
            {
                return _Step;
            }
            set
            {
                _Step = value;
            }
        }

        public override void Encode()
        {
            var pattern = PATTERN_NUMBER;
            if (Length > 0)
            {
                pattern += Length;
            }
            if (Start > 0)
            {
                pattern += PATTERN_SPLIT + Start;
            }
            if (Step > 0)
            {
                pattern += PATTERN_SPLIT + Step;
            }

            Pattern = ToPattern(pattern);
        }
    }
}
