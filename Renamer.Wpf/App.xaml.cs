namespace Renamer.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public const int MAJOR = 1;

        public const int MINOR = 0;

        public const int PATCH = 0;

        public const int BUILD = 1;

        public static readonly string VersionCode = $"{MAJOR}.{MINOR}.{PATCH}.{BUILD}";
        public static readonly string VersionDate = "20261006";
    }
}
