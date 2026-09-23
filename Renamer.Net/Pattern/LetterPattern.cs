using Com.Scm.Renamer;

namespace Com.Scm.Filexp.Rename.Dvo.Pattern
{
    /// <summary>
    /// 字符序列
    /// </summary>
    internal class LetterPattern : RenamerPattern
    {
        public LetterPattern()
        {
            Type = PatternType.Letter;
            Title = "字符序列";
        }

        private char[] _Letters;
        public char[] Letters
        {
            get
            {
                return _Letters;
            }
            set
            {
                _Letters = value;
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
            var pattern = PATTERN_LETTER;
            if (Letters != null && Letters.Length > 0)
            {
                pattern += string.Join("", Letters);
            }
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

    internal class LowerLetterPattern : LetterPattern
    {
        public LowerLetterPattern()
        {
            Type = PatternType.Letter;
            Title = "小写字母序列";
            Pattern = "<|?/>";
            Start = 0;
            Length = 0;
        }

        public override void Encode()
        {
            var pattern = PATTERN_LETTER + PATTERN_OPT_LOWER;

            if (Length > 0)
            {
                pattern += PATTERN_SPLIT + Length;
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
    internal class UpperLetterPattern : LetterPattern
    {
        public UpperLetterPattern()
        {
            Type = PatternType.Letter;
            Title = "大写字母序列";
            Pattern = "<|?\\>";
            Start = 0;
            Length = 0;
        }

        public override void Encode()
        {
            var pattern = PATTERN_LETTER + PATTERN_OPT_UPPER;

            if (Length > 0)
            {
                pattern += PATTERN_SPLIT + Length;
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
