using Com.Scm.Filexp.Rename.Dvo;

namespace Com.Scm.Renamer.Dvo
{
    public class QuickTagItem : ScmDvo
    {
        private RenamerPattern _pattern;

        public RenamerPattern Pattern
        {
            get => _pattern;
            set
            {
                _pattern = value;
                OnPropertyChanged(nameof(Pattern));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Expression));
            }
        }

        public string Name => _pattern?.Title ?? string.Empty;

        public string Expression => _pattern?.Pattern ?? string.Empty;

        public void Refresh()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Expression));
        }
    }
}
