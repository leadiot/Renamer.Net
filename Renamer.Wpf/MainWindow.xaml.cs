using Com.Scm.Filexp.Rename.Dvo;
using Com.Scm.Filexp.Rename.Dvo.Pattern;
using Com.Scm.Renamer.Pattern;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
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

            InitView();
            InitData();
        }

        private void InitView()
        {
            QuickTagList.ItemsSource = QuickTags;
            ResultDataGrid.ItemsSource = FileItems;
        }

        private void InitData()
        {
            LoadTemplates();
            InitDefaultQuickTags();
            showPattern();
            UpdateStatus();
        }

        private void InitDefaultQuickTags()
        {
            var prefix = new NamePrefixPattern();
            var surfix = new NameSuffixPattern();
            var number = new NumberPattern { Length = 3 };
            var separator = new SeparatorPattern();

            QuickTags.Add(new QuickTagItem { Pattern = prefix });
            QuickTags.Add(new QuickTagItem { Pattern = number });
            QuickTags.Add(new QuickTagItem { Pattern = separator });
            QuickTags.Add(new QuickTagItem { Pattern = surfix });
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

        private void showPattern()
        {
            var value = "";
            foreach (var tag in QuickTags)
            {
                var pattern = tag.Pattern;
                if (pattern == null)
                {
                    continue;
                }

                pattern.Encode();
                value += pattern.Pattern;
            }
            ExpressionTextBox.Text = value;
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

        private void QuickTagAddStart_Click(object sender, RoutedEventArgs e)
        {
            var menu = new System.Windows.Controls.ContextMenu();
            AddTypeMenu(menu, "文件前缀", RenamerCore.PatternType.Prefix, true);
            AddTypeMenu(menu, "文件后缀", RenamerCore.PatternType.Suffix, true);
            AddTypeMenu(menu, "固定文本", RenamerCore.PatternType.Text, true);
            AddTypeMenu(menu, "数字序列", RenamerCore.PatternType.Number, true);
            AddTypeMenu(menu, "字符序列", RenamerCore.PatternType.Letter, true);
            AddTypeMenu(menu, "时间信息", RenamerCore.PatternType.Time, true);
            AddTypeMenu(menu, "条件分隔符", RenamerCore.PatternType.Separator, true);
            menu.PlacementTarget = (UIElement)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void QuickTagAddEnd_Click(object sender, RoutedEventArgs e)
        {
            var menu = new System.Windows.Controls.ContextMenu();
            AddTypeMenu(menu, "文件前缀", RenamerCore.PatternType.Prefix, false);
            AddTypeMenu(menu, "文件后缀", RenamerCore.PatternType.Suffix, false);
            AddTypeMenu(menu, "固定文本", RenamerCore.PatternType.Text, false);
            AddTypeMenu(menu, "数字序列", RenamerCore.PatternType.Number, false);
            AddTypeMenu(menu, "字符序列", RenamerCore.PatternType.Letter, false);
            AddTypeMenu(menu, "时间信息", RenamerCore.PatternType.Time, false);
            AddTypeMenu(menu, "条件分隔符", RenamerCore.PatternType.Separator, false);
            menu.PlacementTarget = (UIElement)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void AddTypeMenu(System.Windows.Controls.ContextMenu menu, string header, RenamerCore.PatternType type, bool start)
        {
            var item = new System.Windows.Controls.MenuItem { Header = header, Tag = type };
            item.Click += (s, _) => OnPatternTypeChosen(type, start);
            menu.Items.Add(item);
        }

        private void OnPatternTypeChosen(RenamerCore.PatternType type, bool start)
        {
            RenamerPattern pattern;
            bool ok;
            switch (type)
            {
                case RenamerCore.PatternType.Prefix:
                    pattern = new NamePrefixPattern();
                    ok = new Editor.PrefixEditor((NamePrefixPattern)pattern) { Owner = this }.ShowDialog() == true;
                    break;
                case RenamerCore.PatternType.Suffix:
                    pattern = new NameSuffixPattern();
                    ok = new Editor.SurfixEditor((NameSuffixPattern)pattern) { Owner = this }.ShowDialog() == true;
                    break;
                case RenamerCore.PatternType.Text:
                    pattern = new TextPattern();
                    ok = new Editor.TextEditor((TextPattern)pattern) { Owner = this }.ShowDialog() == true;
                    break;
                case RenamerCore.PatternType.Number:
                    pattern = new NumberPattern();
                    ok = new Editor.NumberEditor((NumberPattern)pattern) { Owner = this }.ShowDialog() == true;
                    break;
                case RenamerCore.PatternType.Letter:
                    pattern = new LetterPattern();
                    ok = new Editor.LetterEditor((LetterPattern)pattern) { Owner = this }.ShowDialog() == true;
                    break;
                case RenamerCore.PatternType.Time:
                    pattern = new TimePattern();
                    ok = new Editor.TimeEditor((TimePattern)pattern) { Owner = this }.ShowDialog() == true;
                    break;
                case RenamerCore.PatternType.Separator:
                    pattern = new SeparatorPattern();
                    ok = true;
                    break;
                default:
                    return;
            }
            if (!ok) return;

            pattern.Encode();
            if (start)
            {
                QuickTags.Insert(0, new QuickTagItem { Pattern = pattern });
            }
            else
            {
                QuickTags.Add(new QuickTagItem { Pattern = pattern });
            }

            showPattern();
        }

        private QuickTagItem? _dragItem;
        private System.Windows.Point _dragStartPoint;
        private bool _dragArmed;
        private bool _suppressNextChipClick;
        private System.Windows.Controls.Grid? _dropHintGrid;

        private void QuickTagItem_Click(object sender, RoutedEventArgs e)
        {
            // 拖拽结束后系统可能仍补发一次 Click，此时不弹编辑窗口
            if (_suppressNextChipClick)
            {
                _suppressNextChipClick = false;
                return;
            }
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is QuickTagItem item)
            {
                EditQuickTag(item);
            }
        }

        private void QuickTagDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is QuickTagItem item)
            {
                QuickTags.Remove(item);
            }
        }

        private void QuickTagItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _suppressNextChipClick = false;
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is QuickTagItem item)
            {
                _dragItem = item;
                _dragStartPoint = e.GetPosition(null);
                _dragArmed = true;
            }
            else
            {
                _dragArmed = false;
            }
        }

        private void QuickTagItem_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_dragArmed || _dragItem == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            System.Windows.Point pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            _dragArmed = false;
            var source = _dragItem;
            if (sender is System.Windows.Controls.Button btn)
            {
                DragDrop.DoDragDrop(btn, source, System.Windows.DragDropEffects.Move);
            }
            _suppressNextChipClick = true;
            _dragItem = null;
        }

        private void QuickTagItem_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(QuickTagItem)) || sender is not System.Windows.Controls.Grid grid)
            {
                e.Effects = System.Windows.DragDropEffects.None;
                return;
            }

            e.Effects = System.Windows.DragDropEffects.Move;
            bool insertAfter = e.GetPosition(grid).X > grid.ActualWidth / 2.0;
            ShowDropHint(grid, insertAfter);
            e.Handled = true;
        }

        private void QuickTagItem_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            // 鼠标移到子元素也会触发 DragLeave，延后判定是否真正离开当前项
            if (sender is System.Windows.Controls.Grid grid)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_dropHintGrid == grid && !grid.IsMouseOver)
                    {
                        ClearDropHint();
                    }
                }));
            }
        }

        private void QuickTagItem_Drop(object sender, System.Windows.DragEventArgs e)
        {
            ClearDropHint();
            if (!e.Data.GetDataPresent(typeof(QuickTagItem))) return;
            if (e.Data.GetData(typeof(QuickTagItem)) is not QuickTagItem source) return;
            if (sender is not System.Windows.Controls.Grid grid || grid.DataContext is not QuickTagItem target) return;
            if (ReferenceEquals(source, target))
            {
                e.Handled = true;
                return;
            }

            bool insertAfter = e.GetPosition(grid).X > grid.ActualWidth / 2.0;
            int from = QuickTags.IndexOf(source);
            if (from < 0) return;

            QuickTags.RemoveAt(from);
            int targetIndex = QuickTags.IndexOf(target);
            if (targetIndex < 0) return;

            int insertAt = insertAfter ? targetIndex + 1 : targetIndex;
            QuickTags.Insert(insertAt, source);
            e.Handled = true;
        }

        private void ShowDropHint(System.Windows.Controls.Grid grid, bool insertAfter)
        {
            if (_dropHintGrid != grid)
            {
                ClearDropHint();
                _dropHintGrid = grid;
            }
            SetDropBar(grid, "InsertBeforeBar", !insertAfter);
            SetDropBar(grid, "InsertAfterBar", insertAfter);
        }

        private void ClearDropHint()
        {
            if (_dropHintGrid == null) return;
            SetDropBar(_dropHintGrid, "InsertBeforeBar", false);
            SetDropBar(_dropHintGrid, "InsertAfterBar", false);
            _dropHintGrid = null;
        }

        private static void SetDropBar(System.Windows.Controls.Grid grid, string barName, bool visible)
        {
            if (FindChild<System.Windows.Shapes.Rectangle>(grid, barName) is { } bar)
            {
                bar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static T? FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typed && typed.Name == name)
                {
                    return typed;
                }
                if (FindChild<T>(child, name) is { } result)
                {
                    return result;
                }
            }
            return null;
        }

        private void QuickTagItem_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is QuickTagItem item && item.Pattern != null)
            {
                var menu = new System.Windows.Controls.ContextMenu();

                var insertItem = new System.Windows.Controls.MenuItem { Header = "插入到表达式" };
                insertItem.Click += (s, _) =>
                {
                    ExpressionTextBox.Text += item.Expression;
                    ExpressionTextBox.Focus();
                    ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
                };
                menu.Items.Add(insertItem);

                menu.PlacementTarget = btn;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                menu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void EditQuickTag(QuickTagItem item)
        {
            if (item.Pattern == null) return;
            bool ok = item.Pattern.Type switch
            {
                RenamerCore.PatternType.Prefix => new Editor.PrefixEditor((NamePrefixPattern)item.Pattern) { Owner = this }.ShowDialog() == true,
                RenamerCore.PatternType.Suffix => new Editor.SurfixEditor((NameSuffixPattern)item.Pattern) { Owner = this }.ShowDialog() == true,
                RenamerCore.PatternType.Text => new Editor.TextEditor((TextPattern)item.Pattern) { Owner = this }.ShowDialog() == true,
                RenamerCore.PatternType.Number => new Editor.NumberEditor((NumberPattern)item.Pattern) { Owner = this }.ShowDialog() == true,
                RenamerCore.PatternType.Letter => new Editor.LetterEditor((LetterPattern)item.Pattern) { Owner = this }.ShowDialog() == true,
                RenamerCore.PatternType.Time => new Editor.TimeEditor((TimePattern)item.Pattern) { Owner = this }.ShowDialog() == true,
                RenamerCore.PatternType.Separator => true,
                _ => false
            };
            if (!ok) return;

            item.Pattern.Encode();
            item.Refresh();
            showPattern();
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
        private RenamerPattern? _pattern;

        public RenamerPattern? Pattern
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

        public string Name => _pattern is TextPattern t && !string.IsNullOrEmpty(t.Text)
            ? t.Text
            : _pattern?.Title ?? string.Empty;

        public string Expression => _pattern?.Pattern ?? string.Empty;

        public void Refresh()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Expression));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
