namespace Com.Scm.Renamer.Dvo
{
    public class QuickTagItem : ScmDvo
    {
        private RenamePattern _pattern;

        public RenamePattern Pattern
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
