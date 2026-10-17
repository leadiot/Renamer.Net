namespace Com.Scm.Renamer
{
    /// <summary>
    /// 交互式批量更名示例：
    ///   1) 输入目录与更名表达式
    ///   2) 编译表达式（Compile）
    ///   3) 只读预览（Preview）并展示 原名 -> 新名、问题与自动消解警告
    ///   4) 确认后提交磁盘改名（Commit）
    /// 仅处理目录下的直接文件（不递归子目录）。
    /// 也支持命令行参数：Renamer.Cmd &lt;目录&gt; &lt;表达式&gt; [冲突策略 fail|auto] [发号选项 none|name|mtime|ctime|atime[,asc|desc][,reset]]
    /// 首参数为 help / --help / -h / /? 时输出表达式使用说明并退出；
    /// 交互输入 help / ? / ？亦可随时查看。
    /// </summary>
    public static class Program
    {
        public const int MAJOR = 1;

        public const int MINOR = 0;

        public const int PATCH = 0;

        public const int BUILD = 1;

        public static readonly string VersionCode = $"{MAJOR}.{MINOR}.{PATCH}.{BUILD}";
        public static readonly string VersionDate = "20261006";

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 首参数为帮助指令时，输出表达式使用说明后退出
            if (args.Length > 0 && IsHelp(args[0]))
            {
                HelpText.Print();
                return 0;
            }

            Console.WriteLine("==============================================================");
            Console.WriteLine("           文件批量更名工具（表达式驱动）");
            Console.WriteLine("==============================================================");
            Console.WriteLine("表达式示例：");
            Console.WriteLine("    IMAGE<|*4>-<*>.<?>");
            Console.WriteLine("    <*|lower|rep:abc:XYZ>.<?>");
            Console.WriteLine("    <|\"c:yyyyMMdd>_<*>.<?>");
            Console.WriteLine("    <*><.><?>（无扩展名文件不产生尾随点）");
            Console.WriteLine("输入 help 查看表达式使用说明，输入 q 可随时退出。");
            Console.WriteLine();

            // 命令行参数：目录、表达式、冲突策略；未提供的项进入交互输入
            string dir = args.Length > 0 ? args[0] : null;
            string template = args.Length > 1 ? args[1] : null;
            ConflictMode mode = ConflictMode.Fail;
            if (args.Length > 2)
            {
                mode = args[2].Trim().Equals("auto", StringComparison.OrdinalIgnoreCase)
                    ? ConflictMode.Auto
                    : ConflictMode.Fail;
            }

            // 1) 目录
            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = PromptDirectory();
                if (dir == null)
                {
                    return 0;
                }
            }
            else if (!Directory.Exists(dir))
            {
                Console.Error.WriteLine("目录不存在：" + dir);
                return 1;
            }

            // 2) 模板列表（从 template.json 读取）或直接输入表达式
            List<TemplateItem> templates = LoadTemplates();
            if (string.IsNullOrWhiteSpace(template))
            {
                template = PromptTemplateOrExpression(templates);
                if (template == null)
                {
                    return 0;
                }
            }

            // 3) 编译表达式（失败可重新输入）
            Renamer renamer = null;
            while (renamer == null)
            {
                try
                {
                    renamer = Renamer.Compile(template!);
                }
                catch (TemplateParseException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("表达式错误（位置 {0}）：{1}", ex.Position, ex.Message);
                    Console.ResetColor();
                    template = PromptTemplateOrExpression(templates);
                    if (template == null)
                    {
                        return 0;
                    }
                }
            }

            // 4) 冲突策略
            if (args.Length <= 2)
            {
                mode = PromptConflictMode();
            }

            // 5) 流水发号选项（命令行第 4 参数，或交互输入）
            NumberingOptions numbering = null;
            if (args.Length > 3)
            {
                numbering = ParseNumbering(args[3]);
            }
            else if (args.Length <= 2)
            {
                numbering = PromptNumbering();
            }

            // 收集目录下的直接文件
            var files = Directory
                .EnumerateFiles(dir)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
            List<FileItem> fileItems = new List<FileItem>();
            foreach (var file in files)
            {
                fileItems.Add(new FileItemDvo()
                {
                    Path = file,
                    SrcName = Path.GetFileName(file)
                });
            }

            if (files.Count == 0)
            {
                Console.WriteLine("目录下没有文件：" + dir);
                return 0;
            }

            if (numbering != null)
            {
                renamer.Numbering = numbering;
            }

            // 5) 预览（只读，不触碰磁盘）
            RenamePlan plan = renamer.Preview(fileItems, mode);
            PrintPreview(plan);

            if (!plan.CanCommit)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("存在 {0} 个阻断性问题，未执行任何改名。请修正表达式或调整文件后重试。",
                    plan.Issues.Count);
                Console.ResetColor();
                return 2;
            }

            int changeCount = plan.Items.Count(i => i.Status != RenameStatus.NoChange);
            if (changeCount == 0)
            {
                Console.WriteLine("没有需要改名的文件（目标名与原名全部相同）。");
                return 0;
            }

            // 6) 确认后提交
            Console.Write("确认对以上 {0} 个文件执行改名？(y/N)：", changeCount);
            string answer = Console.ReadLine();
            if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("已取消，未做任何修改。");
                return 0;
            }

            RenamePlan result = renamer.Commit(fileItems, mode);
            PrintCommitResult(result);
            return result.Issues.Count == 0 ? 0 : 3;
        }

        private static string PromptDirectory()
        {
            while (true)
            {
                Console.Write("请输入目录路径：");
                string input = Console.ReadLine();
                if (input == null || IsQuit(input))
                {
                    return null;
                }
                if (IsHelp(input))
                {
                    HelpText.Print();
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(input) && Directory.Exists(input))
                {
                    return input.Trim();
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("目录不存在，请重新输入。");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// 从程序目录下的 template.json 读取更名模板列表；文件缺失或解析失败时返回空列表。
        /// </summary>
        private static List<TemplateItem> LoadTemplates()
        {
            var templates = new List<TemplateItem>();
            string path = Path.Combine(AppContext.BaseDirectory, "template.json");
            if (!File.Exists(path))
            {
                return templates;
            }
            try
            {
                string json = File.ReadAllText(path);
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return System.Text.Json.JsonSerializer.Deserialize<List<TemplateItem>>(json, options) ?? templates;
            }
            catch
            {
                return templates;
            }
        }

        /// <summary>
        /// 展示模板列表并等待用户选择；也可直接输入表达式（非数字内容视为表达式）。
        /// </summary>
        private static string PromptTemplateOrExpression(List<TemplateItem> templates)
        {
            Console.WriteLine();
            if (templates.Count > 0)
            {
                Console.WriteLine("请选择更名模板（输入序号），或直接输入表达式：");
                for (int i = 0; i < templates.Count; i++)
                {
                    Console.WriteLine("  {0}) {1}    {2}", i + 1, templates[i].Name, templates[i].Pattern);
                }
            }
            else
            {
                Console.WriteLine("未找到模板文件 template.json，请直接输入表达式。");
            }

            while (true)
            {
                Console.Write("输入模板序号或表达式：");
                string input = Console.ReadLine();
                if (input == null || IsQuit(input))
                {
                    return null;
                }
                if (IsHelp(input))
                {
                    HelpText.Print();
                    continue;
                }
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }
                input = input.Trim();
                if (int.TryParse(input, out int index) && index >= 1 && index <= templates.Count)
                {
                    var selected = templates[index - 1];
                    Console.WriteLine("已选择模板：{0}", selected.Name);
                    return selected.Pattern;
                }
                return input; // 视为表达式
            }
        }

        private static ConflictMode PromptConflictMode()
        {
            Console.WriteLine("冲突处理策略：");
            Console.WriteLine("  1) Fail       发现重名/占用即中止（默认）");
            Console.WriteLine("  2) AutoSuffix 自动追加“ (n)”后缀消解");
            while (true)
            {
                Console.Write("请选择 [1/2]（回车=1）：");
                string input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input) || input == "1")
                {
                    return ConflictMode.Fail;
                }
                if (input == "2")
                {
                    return ConflictMode.Auto;
                }
            }
        }

        private static void PrintPreview(RenamePlan plan)
        {
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("预览结果（共 {0} 个文件）", plan.Items.Count);
            Console.WriteLine("--------------------------------------------------------------");

            foreach (FileItem item in plan.Items)
            {
                string src = item.SrcName;
                string dst = item.DstName;
                switch (item.Status)
                {
                    case RenameStatus.Ok:
                        Console.WriteLine("  " + src);
                        Console.WriteLine("    ->  " + dst);
                        break;
                    case RenameStatus.NoChange:
                        Console.WriteLine("  " + src);
                        Console.WriteLine("    ==  （无变化，跳过）");
                        break;
                    case RenameStatus.Resolved:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("  " + src);
                        Console.WriteLine("    ->  {0}  [已自动消解冲突]", dst);
                        Console.ResetColor();
                        break;
                    case RenameStatus.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  " + src);
                        Console.WriteLine("    ->  [错误] {0}", item.IssueMessage);
                        Console.ResetColor();
                        break;
                }
            }

            if (plan.Warnings.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (RenameWarning warning in plan.Warnings)
                {
                    Console.WriteLine("提示：" + warning.Message);
                }
                Console.ResetColor();
            }

            if (plan.Issues.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (RenameIssue issue in plan.Issues)
                {
                    Console.WriteLine("问题[{0}] {1}：{2}",
                        issue.Kind, issue.Item.SrcName, issue.Message);
                }
                Console.ResetColor();
            }

            Console.WriteLine("--------------------------------------------------------------");
        }

        private static void PrintCommitResult(RenamePlan plan)
        {
            int ok = plan.Items.Count(i => i.Status == RenameStatus.Ok);
            int skipped = plan.Items.Count(i => i.Status == RenameStatus.NoChange);
            int failed = plan.Issues.Count(i => i.Kind == IssueKind.IoError);

            Console.WriteLine();
            Console.WriteLine("改名完成：成功 {0}，跳过 {1}，失败 {2}。", ok, skipped, failed);
            if (failed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (RenameIssue issue in plan.Issues.Where(i => i.Kind == IssueKind.IoError))
                {
                    Console.WriteLine("失败：{0}：{1}", issue.Item.SrcName, issue.Message);
                }
                Console.ResetColor();
            }
        }

        private static bool IsQuit(string input)
        {
            return string.Equals(input?.Trim(), "q", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断输入是否为帮助指令：help / --help / -h / /? / ? / ？，不区分大小写。
        /// </summary>
        private static bool IsHelp(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string text = input.Trim();
            return text.Equals("help", StringComparison.OrdinalIgnoreCase)
                || text.Equals("--help", StringComparison.OrdinalIgnoreCase)
                || text.Equals("-h", StringComparison.OrdinalIgnoreCase)
                || text.Equals("/?", StringComparison.OrdinalIgnoreCase)
                || text == "?"
                || text == "？";
        }

        /// <summary>
        /// 交互方式让用户选择流水发号选项。
        /// </summary>
        private static NumberingOptions PromptNumbering()
        {
            Console.WriteLine();
            Console.WriteLine("流水发号顺序：");
            Console.WriteLine("  1) 按列表顺序（默认）");
            Console.WriteLine("  2) 按名称（自然排序：数字段按数值比较）");
            Console.WriteLine("  3) 按修改时间");
            Console.WriteLine("  4) 按创建时间");
            Console.WriteLine("  5) 按访问时间");
            while (true)
            {
                Console.Write("请选择 [1-5]（回车=1）：");
                string input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    input = "1";
                }
                if (!int.TryParse(input, out int opt) || opt < 1 || opt > 5)
                {
                    continue;
                }

                var numbering = new NumberingOptions();
                switch (opt)
                {
                    case 2: numbering.OrderBy = NumberingOrderBy.Name; break;
                    case 3: numbering.OrderBy = NumberingOrderBy.LastWriteTime; break;
                    case 4: numbering.OrderBy = NumberingOrderBy.CreationTime; break;
                    case 5: numbering.OrderBy = NumberingOrderBy.LastAccessTime; break;
                    default: numbering.OrderBy = NumberingOrderBy.None; break;
                }

                if (numbering.OrderBy != NumberingOrderBy.None)
                {
                    Console.Write("方向 [1 升序/2 降序]（回车=1）：");
                    string dir = Console.ReadLine()?.Trim();
                    numbering.Ascending = dir != "2";
                }

                Console.Write("流水号按目录复位（多目录批次各自从起始值发号）？(y/N)：");
                string reset = Console.ReadLine()?.Trim();
                numbering.ResetPerDirectory = string.Equals(reset, "y", StringComparison.OrdinalIgnoreCase);
                return numbering;
            }
        }

        /// <summary>
        /// 解析命令行第 4 个参数（发号选项）。
        /// 格式：none|name|mtime|ctime|atime[,asc|desc][,reset]
        /// </summary>
        private static NumberingOptions ParseNumbering(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return null;
            }

            var opt = new NumberingOptions();
            string order = parts[0].Trim().ToLowerInvariant();
            switch (order)
            {
                case "none": opt.OrderBy = NumberingOrderBy.None; break;
                case "name": opt.OrderBy = NumberingOrderBy.Name; break;
                case "mtime": opt.OrderBy = NumberingOrderBy.LastWriteTime; break;
                case "ctime": opt.OrderBy = NumberingOrderBy.CreationTime; break;
                case "atime": opt.OrderBy = NumberingOrderBy.LastAccessTime; break;
                default:
                    Console.WriteLine("警告：无效的发号排序 '{0}'，已忽略。有效值：none|name|mtime|ctime|atime", order);
                    return null;
            }

            if (parts.Length > 1)
            {
                string dir = parts[1].Trim().ToLowerInvariant();
                if (dir == "desc")
                {
                    opt.Ascending = false;
                }
            }

            if (parts.Length > 2)
            {
                string reset = parts[2].Trim().ToLowerInvariant();
                if (reset == "reset")
                {
                    opt.ResetPerDirectory = true;
                }
            }

            return opt;
        }

        /// <summary>
        /// 更名模板项（template.json 中的一条记录）。
        /// </summary>
        private class TemplateItem
        {
            public string Name { get; set; } = string.Empty;
            public string Pattern { get; set; } = string.Empty;
        }
    }
}
