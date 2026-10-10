using Com.Scm.Filexp.Rename.Dvo.Pattern;
using System.Windows;
using System.Windows.Input;

namespace Renamer.Wpf.Editor
{
    /// <summary>
    /// 数字序列编辑窗体：编辑 <see cref="NumberPattern"/> 的位宽、起始值与步增量。
    /// 确认后仅回写内存模型，由调用方负责 Encode 与刷新。
    /// </summary>
    public partial class NumberEditor : Window
    {
        private readonly NumberPattern _pattern;

        public NumberEditor(NumberPattern pattern)
        {
            _pattern = pattern;
            InitializeComponent();

            Title = pattern.Title;
            LoadFields();
        }

        /// <summary>从模型加载当前值到输入控件（后续可扩展为双向绑定）。</summary>
        private void LoadFields()
        {
            LenBox.Text = _pattern.Length.ToString();
            StartBox.Text = _pattern.Start.ToString();
            StepBox.Text = _pattern.Step.ToString();
        }

        /// <summary>确认后将输入控件值回写到模型。</summary>
        private void ApplyFields()
        {
            _pattern.Length = ParseInt(LenBox.Text, 0);
            _pattern.Start = ParseInt(StartBox.Text, 0);
            _pattern.Step = ParseInt(StepBox.Text, 1);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFields();
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
                ApplyFields();
                DialogResult = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private static int ParseInt(string s, int defaultValue)
        {
            return int.TryParse(s?.Trim(), out int v) ? v : defaultValue;
        }
    }
}
