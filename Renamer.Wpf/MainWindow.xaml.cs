using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using RenamerCore = Com.Scm.Renamer;

namespace Renamer.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<RenameItem> FileItems { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            ResultDataGrid.ItemsSource = FileItems;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            SelectedCountText.Text = FileItems.Count.ToString();
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择要重命名的文件所在目录",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folder = dialog.SelectedPath;
                try
                {
                    var files = Directory.EnumerateFiles(folder);
                    foreach (string file in files)
                    {
                        AddFileInternal(file);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"读取目录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                UpdateStatus();
            }
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择要重命名的文件",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    AddFileInternal(file);
                }
                UpdateStatus();
            }
        }

        private void AddFileInternal(string path)
        {
            if (FileItems.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            FileItems.Add(new RenameItem
            {
                Path = path,
                OriginalName = System.IO.Path.GetFileName(path)
            });
        }

        private void RefreshPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            RunPreview();
        }

        private void ExecuteRenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileItems.Count == 0)
            {
                System.Windows.MessageBox.Show("请先添加文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var coreItems = FileItems.Select(x => x.ToCoreItem()).ToList();
            RenamerCore.Renamer? renamer;
            try
            {
                renamer = RenamerCore.Renamer.Compile(ExpressionTextBox.Text);
            }
            catch (RenamerCore.TemplateParseException ex)
            {
                System.Windows.MessageBox.Show($"表达式解析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RenamerCore.RenamePlan plan = renamer.Commit(coreItems, RenamerCore.ConflictMode.AutoSuffix);
            for (int i = 0; i < coreItems.Count; i++)
            {
                FileItems[i].ApplyCoreItem(coreItems[i]);
            }

            if (!plan.CanCommit)
            {
                System.Windows.MessageBox.Show($"执行完成，但有 {plan.Issues.Count} 个错误，请查看表格中的异常信息。", "完成", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                System.Windows.MessageBox.Show($"重命名完成，共处理 {coreItems.Count} 个文件。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "导出重命名计划",
                Filter = "文本文件 (*.txt)|*.txt|CSV 文件 (*.csv)|*.csv",
                DefaultExt = "txt"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("原始名称,结果名称,执行结果,异常信息");
                foreach (RenameItem item in FileItems)
                {
                    sb.AppendLine($"{EscapeCsv(item.OriginalName)},{EscapeCsv(item.ResultName ?? string.Empty)},{EscapeCsv(item.ResultStatus ?? string.Empty)},{EscapeCsv(item.ExceptionMessage ?? string.Empty)}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                System.Windows.MessageBox.Show("导出成功", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private void ExpressionMenuButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 弹出表达式帮助菜单
            System.Windows.MessageBox.Show("表达式帮助（待完善）：\n<*>=原文件名，<?>=扩展名，<|*3>=三位序号，<|\"\">=时间，<.>=条件点号", "表达式");
        }

        private void QuickTagAdd_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 添加自定义快捷标签
            System.Windows.MessageBox.Show("添加自定义标签（待实现）");
        }

        private void QuickTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
            {
                ExpressionTextBox.Text += tag;
                ExpressionTextBox.Focus();
                ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                RunPreview();
            }
        }

        private void RunPreview()
        {
            if (FileItems.Count == 0)
            {
                return;
            }

            var coreItems = FileItems.Select(x => x.ToCoreItem()).ToList();
            RenamerCore.Renamer? renamer;
            try
            {
                renamer = RenamerCore.Renamer.Compile(ExpressionTextBox.Text);
            }
            catch (RenamerCore.TemplateParseException ex)
            {
                System.Windows.MessageBox.Show($"表达式解析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            renamer.Preview(coreItems, RenamerCore.ConflictMode.AutoSuffix);
            for (int i = 0; i < coreItems.Count; i++)
            {
                FileItems[i].ApplyCoreItem(coreItems[i]);
            }
        }
    }

    public class RenameItem : INotifyPropertyChanged
    {
        private string _path = string.Empty;
        private string _originalName = string.Empty;
        private string _resultName = string.Empty;
        private string _exceptionMessage = string.Empty;
        private string _resultStatus = string.Empty;

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(nameof(Path)); }
        }

        public string OriginalName
        {
            get => _originalName;
            set { _originalName = value; OnPropertyChanged(nameof(OriginalName)); }
        }

        public string ResultName
        {
            get => _resultName;
            set { _resultName = value; OnPropertyChanged(nameof(ResultName)); }
        }

        public string ExceptionMessage
        {
            get => _exceptionMessage;
            set { _exceptionMessage = value; OnPropertyChanged(nameof(ExceptionMessage)); }
        }

        public string ResultStatus
        {
            get => _resultStatus;
            set { _resultStatus = value; OnPropertyChanged(nameof(ResultStatus)); }
        }

        public RenamerCore.FileItem ToCoreItem()
        {
            return new RenamerCore.FileItem
            {
                Path = Path,
                SrcName = OriginalName
            };
        }

        public void ApplyCoreItem(RenamerCore.FileItem item)
        {
            Path = item.Path ?? string.Empty;
            OriginalName = item.SrcName ?? System.IO.Path.GetFileName(item.Path ?? string.Empty);
            ResultName = item.DstName ?? string.Empty;
            ExceptionMessage = item.IssueMessage ?? string.Empty;
            ResultStatus = item.Status switch
            {
                RenamerCore.RenameStatus.Ok => "成功",
                RenamerCore.RenameStatus.NoChange => "跳过",
                RenamerCore.RenameStatus.Resolved => "已消解",
                RenamerCore.RenameStatus.Error => "失败",
                _ => string.Empty
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
