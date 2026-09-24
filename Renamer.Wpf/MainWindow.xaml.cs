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
        public ObservableCollection<QuickTagItem> QuickTags { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            ResultDataGrid.ItemsSource = FileItems;
            QuickTagList.ItemsSource = QuickTags;
            LoadTemplates();
            InitDefaultQuickTags();
            UpdateStatus();
        }

        private void InitDefaultQuickTags()
        {
            QuickTags.Add(new QuickTagItem { Name = "name", Pattern = "<*>" });
            QuickTags.Add(new QuickTagItem { Name = "ext", Pattern = "<?>" });
            QuickTags.Add(new QuickTagItem { Name = "index", Pattern = "<|*3>" });
            QuickTags.Add(new QuickTagItem { Name = "date", Pattern = "<|\">" });
            QuickTags.Add(new QuickTagItem { Name = "sep", Pattern = "<.>" });
        }

        private void LoadTemplates()
        {
            string templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "template.json");
            if (!File.Exists(templatePath)) return;

            try
            {
                var json = File.ReadAllText(templatePath);
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var templates = System.Text.Json.JsonSerializer.Deserialize<List<TemplateItem>>(json, options);
                if (templates == null || templates.Count == 0) return;

                var menu = new System.Windows.Controls.ContextMenu();
                foreach (var t in templates)
                {
                    var item = new System.Windows.Controls.MenuItem
                    {
                        Header = $"{t.Name}    {t.Pattern}",
                        Tag = t.Pattern,
                        FontFamily = (System.Windows.Media.FontFamily)FindResource("MonoFont")
                    };
                    item.Click += TemplateMenuItem_Click;
                    menu.Items.Add(item);
                }
                ExpressionMenuButton.ContextMenu = menu;
            }
            catch
            {
                // 模板加载失败不阻断启动
            }
        }

        private void TemplateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem mi && mi.Tag is string pattern)
            {
                ExpressionTextBox.Text = pattern;
                ExpressionTextBox.Focus();
                ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
            }
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
            if (ExpressionMenuButton.ContextMenu != null)
            {
                ExpressionMenuButton.ContextMenu.PlacementTarget = ExpressionMenuButton;
                ExpressionMenuButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                ExpressionMenuButton.ContextMenu.IsOpen = true;
            }
            else
            {
                System.Windows.MessageBox.Show("未找到模板文件 template.json", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void QuickTagAdd_Click(object sender, RoutedEventArgs e)
        {
            var (name, pattern, ok) = ShowTagEditor(null, null);
            if (ok && !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(pattern))
            {
                QuickTags.Add(new QuickTagItem { Name = name.Trim(), Pattern = pattern.Trim() });
            }
        }

        private void QuickTagItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
            {
                ExpressionTextBox.Text += tag;
                ExpressionTextBox.Focus();
                ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
            }
        }

        private void QuickTagItem_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is QuickTagItem item)
            {
                var menu = new System.Windows.Controls.ContextMenu();

                var insertItem = new System.Windows.Controls.MenuItem { Header = "插入到表达式" };
                insertItem.Click += (s, _) =>
                {
                    ExpressionTextBox.Text += item.Pattern;
                    ExpressionTextBox.Focus();
                    ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
                };
                menu.Items.Add(insertItem);

                var editItem = new System.Windows.Controls.MenuItem { Header = "编辑..." };
                editItem.Click += (s, _) =>
                {
                    var (name, pattern, ok) = ShowTagEditor(item.Name, item.Pattern);
                    if (ok)
                    {
                        item.Name = name.Trim();
                        item.Pattern = pattern.Trim();
                    }
                };
                menu.Items.Add(editItem);

                var deleteItem = new System.Windows.Controls.MenuItem { Header = "删除" };
                deleteItem.Click += (s, _) => QuickTags.Remove(item);
                menu.Items.Add(deleteItem);

                menu.PlacementTarget = btn;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                menu.IsOpen = true;
                e.Handled = true;
            }
        }

        private (string name, string pattern, bool ok) ShowTagEditor(string? initName, string? initPattern)
        {
            var dlg = new Window
            {
                Title = initName == null ? "添加快捷表达式" : "编辑快捷表达式",
                Width = 360,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush"),
                FontFamily = (System.Windows.Media.FontFamily)FindResource("AppFont")
            };

            var nameBox = new System.Windows.Controls.TextBox
            {
                Style = (Style)FindResource("OutlinedTextBox"),
                Text = initName ?? "",
                Margin = new Thickness(16, 16, 16, 4)
            };
            var patternBox = new System.Windows.Controls.TextBox
            {
                Style = (Style)FindResource("OutlinedTextBox"),
                Text = initPattern ?? "",
                Margin = new Thickness(16, 4, 16, 8),
                FontFamily = (System.Windows.Media.FontFamily)FindResource("MonoFont")
            };
            var btnPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 16, 16)
            };
            var okBtn = new System.Windows.Controls.Button
            {
                Style = (Style)FindResource("FilledButton"),
                Content = "确定",
                Margin = new Thickness(0, 0, 8, 0)
            };
            var cancelBtn = new System.Windows.Controls.Button
            {
                Style = (Style)FindResource("OutlinedButton"),
                Content = "取消"
            };
            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "名称", Margin = new Thickness(16, 16, 16, 2), Foreground = (System.Windows.Media.Brush)FindResource("OnSurfaceVariantBrush") });
            panel.Children.Add(nameBox);
            panel.Children.Add(new TextBlock { Text = "表达式", Margin = new Thickness(16, 4, 16, 2), Foreground = (System.Windows.Media.Brush)FindResource("OnSurfaceVariantBrush") });
            panel.Children.Add(patternBox);
            panel.Children.Add(btnPanel);
            dlg.Content = panel;

            string resultName = initName ?? "";
            string resultPattern = initPattern ?? "";
            bool confirmed = false;

            okBtn.Click += (s, _) => { resultName = nameBox.Text; resultPattern = patternBox.Text; confirmed = true; dlg.Close(); };
            cancelBtn.Click += (s, _) => dlg.Close();

            dlg.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter) { resultName = nameBox.Text; resultPattern = patternBox.Text; confirmed = true; dlg.Close(); }
                if (ev.Key == Key.Escape) dlg.Close();
            };

            dlg.ShowDialog();
            return (resultName, resultPattern, confirmed);
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

    public class TemplateItem
    {
        public string Name { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
    }

    public class QuickTagItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _pattern = string.Empty;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Pattern
        {
            get => _pattern;
            set { _pattern = value; OnPropertyChanged(nameof(Pattern)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
