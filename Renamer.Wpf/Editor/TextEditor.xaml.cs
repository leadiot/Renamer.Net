using System.Windows;
using System.Windows.Input;
using Com.Scm.Filexp.Rename.Dvo.Pattern;

namespace Renamer.Wpf.Editor
{
    /// <summary>
    /// 固定文本编辑窗体：编辑 <see cref="TextPattern"/> 的文本内容。
    /// 确认后仅回写内存模型，由调用方负责 Encode 与刷新。
    /// </summary>
    public partial class TextEditor : Window
    {
        private readonly TextPattern _pattern;

        public TextEditor(TextPattern pattern)
        {
            _pattern = pattern;
            InitializeComponent();

            Title = pattern.Title;
            LoadFields();
        }

        /// <summary>从模型加载当前值到输入控件。</summary>
        private void LoadFields()
        {
            ContentBox.Text = _pattern.Text ?? "";
        }

        /// <summary>表达式语法定界符，固定文本中禁止出现。</summary>
        private static readonly char[] SyntaxChars = new[] { '<', '>' };

        /// <summary>确认前校验：固定文本不能包含语法字符。</summary>
        private bool ValidateFields()
        {
            var text = ContentBox.Text ?? "";
            int idx = text.IndexOfAny(SyntaxChars);
            if (idx < 0)
            {
                return true;
            }

            System.Windows.MessageBox.Show(this,
                $"固定文本不能包含语法字符 \"<\" 和 \">\"（表达式定界符）。\n第 {idx + 1} 个字符为 '{text[idx]}'，请删除或替换后再保存。",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        /// <summary>确认后将输入控件值回写到模型。</summary>
        private void ApplyFields()
        {
            _pattern.Text = ContentBox.Text;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
            {
                return;
            }
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
                if (!ValidateFields())
                {
                    return;
                }
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
