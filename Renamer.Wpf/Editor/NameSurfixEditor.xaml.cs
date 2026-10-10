using Com.Scm.Filexp.Rename.Dvo.Pattern;
using System.Windows;
using System.Windows.Input;

namespace Renamer.Wpf.Editor
{
    /// <summary>
    /// 文件后缀编辑窗体：编辑 <see cref="NameSuffixPattern"/> 的文字操作配置。
    /// 字段面板复用 <see cref="NameEditor"/>；确认后仅回写内存模型，由调用方负责 Encode 与刷新。
    /// </summary>
    public partial class NameSurfixEditor : Window
    {
        private readonly NameSuffixPattern _pattern;

        public NameSurfixEditor(NameSuffixPattern pattern)
        {
            _pattern = pattern;
            InitializeComponent();

            Title = pattern.Title;
            Fields.LoadFrom(pattern);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Fields.ApplyTo(_pattern);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Fields.ApplyTo(_pattern);
                DialogResult = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
