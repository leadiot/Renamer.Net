namespace Com.Scm.Renamer
{
    /// <summary>
    /// 交互式批量重命名示例：
    ///   1) 输入目录与重命名表达式
    ///   2) 编译表达式（Compile）
    ///   3) 只读预览（Preview）并展示 原名 -> 新名、问题与自动消解警告
    ///   4) 确认后提交磁盘改名（Commit）
    /// 仅处理目录下的直接文件（不递归子目录）。
    /// 也支持命令行参数：Renamer.Cmd &lt;目录&gt; &lt;表达式&gt; [冲突策略 fail|auto]
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("==============================================================");
            Console.WriteLine("           文件批量重命名工具（表达式驱动）");
            Console.WriteLine("==============================================================");
            Console.WriteLine("表达式示例：IMAGE<|*4>-<*>.<?>" +
                              "   <*|lower|rep:abc:XYZ>.<?>" +
                              "   <|\"c:yyyyMMdd>_<*>.<?>");
            Console.WriteLine("             <*><.><?>（无扩展名文件不产生尾随点）");
            Console.WriteLine("输入 q 可随时退出。");
            Console.WriteLine();

            // 命令行参数：目录、表达式、冲突策略；未提供的项进入交互输入
            string? dir = args.Length > 0 ? args[0] : null;
            string? template = args.Length > 1 ? args[1] : null;
            ConflictMode mode = ConflictMode.Fail;
            if (args.Length > 2)
            {
                mode = args[2].Trim().Equals("auto", StringComparison.OrdinalIgnoreCase)
                    ? ConflictMode.AutoSuffix
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

            // 2) 表达式（编译失败可重新输入）
            Renamer2? renamer = null;
            while (renamer == null)
            {
                if (string.IsNullOrWhiteSpace(template))
                {
                    Console.Write("请输入重命名表达式：");
                    template = Console.ReadLine();
                    if (IsQuit(template))
                    {
                        return 0;
                    }
                }

                try
                {
                    renamer = Renamer2.Compile(template!);
                }
                catch (TemplateParseException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("表达式错误（位置 {0}）：{1}", ex.Position, ex.Message);
                    Console.ResetColor();
                    template = null; // 允许重新输入
                }
            }

            // 3) 冲突策略
            if (args.Length <= 2)
            {
                mode = PromptConflictMode();
            }

            // 4) 收集目录下的直接文件
            List<FileItem> files = Directory
                .EnumerateFiles(dir)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Select(p => new FileItem { Path = p, SrcName = Path.GetFileName(p) })
                .ToList();

            if (files.Count == 0)
            {
                Console.WriteLine("目录下没有文件：" + dir);
                return 0;
            }

            // 5) 预览（只读，不触碰磁盘）
            RenamePlan plan = renamer.Preview(files, mode);
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
            string? answer = Console.ReadLine();
            if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("已取消，未做任何修改。");
                return 0;
            }

            RenamePlan result = renamer.Commit(files, mode);
            PrintCommitResult(result);
            return result.Issues.Count == 0 ? 0 : 3;
        }

        private static string? PromptDirectory()
        {
            while (true)
            {
                Console.Write("请输入目录路径：");
                string? input = Console.ReadLine();
                if (IsQuit(input))
                {
                    return null;
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

        private static ConflictMode PromptConflictMode()
        {
            Console.WriteLine("冲突处理策略：");
            Console.WriteLine("  1) Fail       发现重名/占用即中止（默认）");
            Console.WriteLine("  2) AutoSuffix 自动追加“ (n)”后缀消解");
            while (true)
            {
                Console.Write("请选择 [1/2]（回车=1）：");
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input) || input == "1")
                {
                    return ConflictMode.Fail;
                }
                if (input == "2")
                {
                    return ConflictMode.AutoSuffix;
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
                        Console.WriteLine("  {0}  ->  {1}", src, dst);
                        break;
                    case RenameStatus.NoChange:
                        Console.WriteLine("  {0}  ==  （无变化，跳过）", src);
                        break;
                    case RenameStatus.Resolved:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("  {0}  ->  {1}  [已自动消解冲突]", src, dst);
                        Console.ResetColor();
                        break;
                    case RenameStatus.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  {0}  ->  [错误] {1}", src, item.IssueMessage);
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

        private static bool IsQuit(string? input)
        {
            return string.Equals(input?.Trim(), "q", StringComparison.OrdinalIgnoreCase);
        }
    }
}
