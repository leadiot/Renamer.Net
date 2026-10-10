using System.Windows;
using System.Windows.Input;
using Com.Scm.Filexp.Rename.Dvo.Pattern;

namespace Renamer.Wpf.Editor
{
    /// <summary>
    /// 时间信息编辑窗体：编辑 <see cref="TimePattern"/> 的时间来源与格式。
    /// 确认后仅回写内存模型，由调用方负责 Encode 与刷新。
    /// </summary>
    public partial class TimeEditor : Window
    {
        private readonly TimePattern _pattern;

        public TimeEditor(TimePattern pattern)
        {
            _pattern = pattern;
            InitializeComponent();

            Title = pattern.Title;
            LoadFields();
        }

        /// <summary>从模型加载当前值到输入控件。</summary>
        private void LoadFields()
        {
            SrcBox.SelectedIndex = _pattern.Source switch
            {
                SourceEnum.CreateTime => 1,
                SourceEnum.ModifyTime => 2,
                SourceEnum.AccessTime => 3,
                _ => 0
            };
            FmtBox.Text = _pattern.Format ?? "";
        }

        /// <summary>确认后将输入控件值回写到模型。</summary>
        private void ApplyFields()
        {
            _pattern.Source = SrcBox.SelectedIndex switch
            {
                1 => SourceEnum.CreateTime,
                2 => SourceEnum.ModifyTime,
                3 => SourceEnum.AccessTime,
                _ => SourceEnum.CurrentTime
            };
            _pattern.Format = string.IsNullOrWhiteSpace(FmtBox.Text) ? null : FmtBox.Text;
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
    }
}
