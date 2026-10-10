using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using RenamerCore = Com.Scm.Renamer;
using Com.Scm.Filexp.Rename.Dvo;
using Com.Scm.Filexp.Rename.Dvo.Pattern;
using Com.Scm.Renamer.Pattern;

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
            var name = new NamePrefixPattern();
            var ext = new NameSuffixPattern();
            var index = new NumberPattern { Length = 3 };
            var date = new TimePattern();
            var sep = new SeparatorPattern();
            name.Encode(); ext.Encode(); index.Encode(); date.Encode(); sep.Encode();
            QuickTags.Add(new QuickTagItem { Pattern = name });
            QuickTags.Add(new QuickTagItem { Pattern = ext });
            QuickTags.Add(new QuickTagItem { Pattern = index });
            QuickTags.Add(new QuickTagItem { Pattern = date });
            QuickTags.Add(new QuickTagItem { Pattern = sep });
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
            var menu = new System.Windows.Controls.ContextMenu();
            AddTypeMenu(menu, "文件前缀", RenamerCore.PatternType.Prefix);
            AddTypeMenu(menu, "文件后缀", RenamerCore.PatternType.Suffix);
            AddTypeMenu(menu, "数字序列", RenamerCore.PatternType.Number);
            AddTypeMenu(menu, "字符序列", RenamerCore.PatternType.Letter);
            AddTypeMenu(menu, "时间信息", RenamerCore.PatternType.Time);
            AddTypeMenu(menu, "条件分隔符", RenamerCore.PatternType.Separator);
            menu.PlacementTarget = (UIElement)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void AddTypeMenu(System.Windows.Controls.ContextMenu menu, string header, RenamerCore.PatternType type)
        {
            var item = new System.Windows.Controls.MenuItem { Header = header, Tag = type };
            item.Click += (s, _) => OnPatternTypeChosen(type);
            menu.Items.Add(item);
        }

        private void OnPatternTypeChosen(RenamerCore.PatternType type)
        {
            RenamerPattern pattern;
            bool ok;
            switch (type)
            {
                case RenamerCore.PatternType.Prefix:
                    pattern = new NamePrefixPattern();
                    ok = ShowNameEditor((NamePattern)pattern);
                    break;
                case RenamerCore.PatternType.Suffix:
                    pattern = new NameSuffixPattern();
                    ok = ShowNameEditor((NamePattern)pattern);
                    break;
                case RenamerCore.PatternType.Number:
                    pattern = new NumberPattern();
                    ok = ShowNumberEditor((NumberPattern)pattern);
                    break;
                case RenamerCore.PatternType.Letter:
                    pattern = new LetterPattern();
                    ok = ShowLetterEditor((LetterPattern)pattern);
                    break;
                case RenamerCore.PatternType.Time:
                    pattern = new TimePattern();
                    ok = ShowTimeEditor((TimePattern)pattern);
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
            QuickTags.Add(new QuickTagItem { Pattern = pattern });
            ExpressionTextBox.Text = pattern.Pattern;
            ExpressionTextBox.Focus();
            ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
        }

        private void QuickTagItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is QuickTagItem item)
            {
                ExpressionTextBox.Text += item.Expression;
                ExpressionTextBox.Focus();
                ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
            }
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

                var editItem = new System.Windows.Controls.MenuItem { Header = "编辑..." };
                editItem.Click += (s, _) => EditQuickTag(item);
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

        private void EditQuickTag(QuickTagItem item)
        {
            if (item.Pattern == null) return;
            bool ok = item.Pattern.Type switch
            {
                RenamerCore.PatternType.Prefix => ShowNameEditor((NamePattern)item.Pattern),
                RenamerCore.PatternType.Suffix => ShowNameEditor((NamePattern)item.Pattern),
                RenamerCore.PatternType.Number => ShowNumberEditor((NumberPattern)item.Pattern),
                RenamerCore.PatternType.Letter => ShowLetterEditor((LetterPattern)item.Pattern),
                RenamerCore.PatternType.Time => ShowTimeEditor((TimePattern)item.Pattern),
                RenamerCore.PatternType.Separator => true,
                _ => false
            };
            if (!ok) return;

            item.Pattern.Encode();
            item.Refresh();
            ExpressionTextBox.Text = item.Pattern.Pattern;
            ExpressionTextBox.Focus();
            ExpressionTextBox.CaretIndex = ExpressionTextBox.Text.Length;
        }

        private TextBlock FieldLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, 10, 0, 2),
                FontSize = 13,
                Foreground = (System.Windows.Media.Brush)FindResource("OnSurfaceVariantBrush")
            };
        }

        private System.Windows.Controls.TextBox FieldTextBox(string text = "")
        {
            return new System.Windows.Controls.TextBox
            {
                Style = (Style)FindResource("OutlinedTextBox"),
                Text = text,
                Margin = new Thickness(0, 0, 0, 4)
            };
        }

        private System.Windows.Controls.ComboBox FieldComboBox(string[] items, int selectedIndex = 0)
        {
            var cb = new System.Windows.Controls.ComboBox
            {
                Margin = new Thickness(0, 0, 0, 4),
                Height = 36,
                FontSize = 13,
                VerticalContentAlignment = System.Windows.VerticalAlignment.Center
            };
            foreach (var it in items) cb.Items.Add(it);
            cb.SelectedIndex = selectedIndex;
            return cb;
        }

        private bool ShowEditor(string title, double height,
            Action<StackPanel> buildContent, Action applyValues)
        {
            var dlg = new Window
            {
                Title = title,
                Width = 420,
                Height = height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush"),
                FontFamily = (System.Windows.Media.FontFamily)FindResource("AppFont")
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            buildContent(panel);

            var okBtn = new System.Windows.Controls.Button
            {
                Style = (Style)FindResource("FilledButton"),
                Content = "确定",
                Width = 96,
                Margin = new Thickness(0, 16, 8, 0)
            };
            var cancelBtn = new System.Windows.Controls.Button
            {
                Style = (Style)FindResource("OutlinedButton"),
                Content = "取消",
                Width = 96
            };
            var btnPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };
            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            panel.Children.Add(btnPanel);
            dlg.Content = panel;

            bool confirmed = false;
            okBtn.Click += (s, _) => { confirmed = true; dlg.Close(); };
            cancelBtn.Click += (s, _) => dlg.Close();
            dlg.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter) { confirmed = true; dlg.Close(); }
                if (ev.Key == Key.Escape) dlg.Close();
            };

            dlg.ShowDialog();
            if (confirmed) applyValues();
            return confirmed;
        }

        private bool ShowNameEditor(NamePattern p)
        {
            int caseIdx = p.ToUpper ? 1 : (p.ToLower ? 2 : 0);
            int opIdx = p.Opt switch
            {
                OptEnum.Cut => 1,
                OptEnum.Insert => 2,
                OptEnum.Delete => 3,
                OptEnum.Replace1 => 4,
                OptEnum.Replace2 => 5,
                _ => 0
            };
            int dirIdx = p.Dir == DirEnum.Right ? 1 : 0;

            var caseBox = FieldComboBox(new[] { "原样", "转大写", "转小写" }, caseIdx);
            var opBox = FieldComboBox(new[] { "原样", "截取", "插入", "删除", "区间替换", "文字替换" }, opIdx);
            var dirBox = FieldComboBox(new[] { "左起", "右起" }, dirIdx);
            var fromBox = FieldTextBox(p.From > 0 ? p.From.ToString() : "");
            var toBox = FieldTextBox(p.To > 0 ? p.To.ToString() : "");
            var lenBox = FieldTextBox(p.Length > 0 ? p.Length.ToString() : "");
            var srcBox = FieldTextBox(p.SrcText ?? "");
            var dstBox = FieldTextBox(p.DstText ?? "");

            return ShowEditor(p.Title, 720,
                panel =>
                {
                    panel.Children.Add(FieldLabel("大小写（优先级最高，选中后忽略下方操作）"));
                    panel.Children.Add(caseBox);
                    panel.Children.Add(FieldLabel("操作"));
                    panel.Children.Add(opBox);
                    panel.Children.Add(FieldLabel("方向（位置操作用）"));
                    panel.Children.Add(dirBox);
                    panel.Children.Add(FieldLabel("起始位置 From（1 基）"));
                    panel.Children.Add(fromBox);
                    panel.Children.Add(FieldLabel("结束位置 To（截取/区间替换，1 基含端点）"));
                    panel.Children.Add(toBox);
                    panel.Children.Add(FieldLabel("长度 Length（删除）"));
                    panel.Children.Add(lenBox);
                    panel.Children.Add(FieldLabel("源文本 SrcText（文字替换的被替换串）"));
                    panel.Children.Add(srcBox);
                    panel.Children.Add(FieldLabel("目标文本 DstText（插入/替换的目标串）"));
                    panel.Children.Add(dstBox);
                },
                () =>
                {
                    p.ToUpper = caseBox.SelectedIndex == 1;
                    p.ToLower = caseBox.SelectedIndex == 2;
                    p.Opt = opBox.SelectedIndex switch
                    {
                        1 => OptEnum.Cut,
                        2 => OptEnum.Insert,
                        3 => OptEnum.Delete,
                        4 => OptEnum.Replace1,
                        5 => OptEnum.Replace2,
                        _ => OptEnum.None
                    };
                    p.Dir = dirBox.SelectedIndex == 1 ? DirEnum.Right : DirEnum.Left;
                    p.From = ParseInt(fromBox.Text, 1);
                    p.To = ParseInt(toBox.Text, 1);
                    p.Length = ParseInt(lenBox.Text, 1);
                    p.SrcText = srcBox.Text;
                    p.DstText = dstBox.Text;
                });
        }

        private bool ShowNumberEditor(NumberPattern p)
        {
            var lenBox = FieldTextBox(p.Length > 0 ? p.Length.ToString() : "");
            var startBox = FieldTextBox(p.Start.ToString());
            var stepBox = FieldTextBox(p.Step.ToString());

            return ShowEditor(p.Title, 400,
                panel =>
                {
                    panel.Children.Add(FieldLabel("位宽（左侧补 0，0 不补齐）"));
                    panel.Children.Add(lenBox);
                    panel.Children.Add(FieldLabel("起始值（默认 0，支持负数）"));
                    panel.Children.Add(startBox);
                    panel.Children.Add(FieldLabel("步增量（默认 1，支持负数）"));
                    panel.Children.Add(stepBox);
                },
                () =>
                {
                    p.Length = ParseInt(lenBox.Text, 0);
                    p.Start = ParseInt(startBox.Text, 0);
                    p.Step = ParseInt(stepBox.Text, 1);
                });
        }

        private bool ShowLetterEditor(LetterPattern p)
        {
            int setIdx = (p.Letters != null && p.Letters.Length > 0) ? 2 : (p.IsUpper ? 1 : 0);
            var setBox = FieldComboBox(new[] { "小写字母 (a-z)", "大写字母 (A-Z)", "自定义字符集" }, setIdx);
            var customBox = FieldTextBox(p.Letters != null ? new string(p.Letters) : "");
            var lenBox = FieldTextBox(p.Length > 0 ? p.Length.ToString() : "");
            var startBox = FieldTextBox(p.Start.ToString());
            var stepBox = FieldTextBox(p.Step.ToString());

            return ShowEditor(p.Title, 580,
                panel =>
                {
                    panel.Children.Add(FieldLabel("字符集"));
                    panel.Children.Add(setBox);
                    panel.Children.Add(FieldLabel("自定义字符集（选择\"自定义字符集\"时生效）"));
                    panel.Children.Add(customBox);
                    panel.Children.Add(FieldLabel("位宽（左侧补首字符，0 不补齐）"));
                    panel.Children.Add(lenBox);
                    panel.Children.Add(FieldLabel("起始序数（默认 0）"));
                    panel.Children.Add(startBox);
                    panel.Children.Add(FieldLabel("步增量（默认 1，支持负）"));
                    panel.Children.Add(stepBox);
                },
                () =>
                {
                    if (setBox.SelectedIndex == 2)
                    {
                        var cs = customBox.Text ?? "";
                        p.Letters = cs.Length > 0 ? cs.ToCharArray() : null;
                    }
                    else
                    {
                        p.Letters = null;
                        p.IsUpper = setBox.SelectedIndex == 1;
                    }
                    p.Length = ParseInt(lenBox.Text, 0);
                    p.Start = ParseInt(startBox.Text, 0);
                    p.Step = ParseInt(stepBox.Text, 1);
                });
        }

        private bool ShowTimeEditor(TimePattern p)
        {
            int srcIdx = p.Source switch
            {
                SourceEnum.CreateTime => 1,
                SourceEnum.ModifyTime => 2,
                SourceEnum.AccessTime => 3,
                _ => 0
            };
            var srcBox = FieldComboBox(new[] { "当前时间", "创建时间", "修改时间", "访问时间" }, srcIdx);
            var fmtBox = FieldTextBox(p.Format ?? "");

            return ShowEditor(p.Title, 380,
                panel =>
                {
                    panel.Children.Add(FieldLabel("时间来源"));
                    panel.Children.Add(srcBox);
                    panel.Children.Add(FieldLabel("格式（留空 = yyyyMMddHHmmss）"));
                    panel.Children.Add(fmtBox);
                },
                () =>
                {
                    p.Source = srcBox.SelectedIndex switch
                    {
                        1 => SourceEnum.CreateTime,
                        2 => SourceEnum.ModifyTime,
                        3 => SourceEnum.AccessTime,
                        _ => SourceEnum.CurrentTime
                    };
                    p.Format = string.IsNullOrWhiteSpace(fmtBox.Text) ? null : fmtBox.Text;
                });
        }

        private static int ParseInt(string s, int defaultValue)
        {
            return int.TryParse(s?.Trim(), out int v) ? v : defaultValue;
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

        public string Name => _pattern?.Title ?? string.Empty;

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
