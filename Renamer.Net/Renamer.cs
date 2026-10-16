using Com.Scm.Renamer.Pattern;
using System;
using System.Collections.Generic;
using System.IO;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 批量重命名执行器：模板编译一次，按文件顺序逐一生成 DstName。
    /// 数字/字母流水依据 FileItem.Index（本批次中的 0 基序号）计算。
    /// 标准流程：Compile → Preview（冲突检测）→ Commit（提交磁盘改名）。
    /// </summary>
    public class Renamer
    {
        public const int MAJOR = 1;

        public const int MINOR = 0;

        public const int PATCH = 0;

        public const int BUILD = 1;

        public static readonly string VersionCode = $"{MAJOR}.{MINOR}.{PATCH}.{BUILD}";
        public static readonly string VersionDate = "20261006";

        /// <summary>
        /// 临时名前缀，用于两阶段改名中标识中间状态文件。
        /// </summary>
        private static readonly string TempPrefix = ".__renamer_tmp_";

        private readonly List<RenamePattern> patterns = new List<RenamePattern>();

        /// <summary>
        /// 编译模板并返回执行器；模板非法时抛出 <see cref="TemplateParseException"/>。
        /// </summary>
        public static Renamer Compile(string template)
        {
            var renamer = new Renamer();
            foreach (RenamePattern pattern in TemplateParser.Parse(template))
            {
                renamer.Add(pattern);
            }
            return renamer;
        }

        public void Add(RenamePattern pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            this.patterns.Add(pattern);
        }

        /// <summary>
        /// 预览：对全部文件独立求值并做冲突检测，不触碰磁盘。
        /// 检测项：求值异常、空文件名、非法文件名字符、批次内重名（大小写不敏感）、
        /// 目标已被同目录现有文件占用（若占用者本身在批次内且将被改名移走，则视为可用）。
        /// AutoSuffix 模式下冲突会自动以“ (n)”消解。
        /// </summary>
        public RenamePlan Preview(List<FileItem> fileList, ConflictMode mode = ConflictMode.Fail)
        {
            if (fileList == null)
            {
                throw new ArgumentNullException(nameof(fileList));
            }

            var plan = new RenamePlan(fileList);
            var existingCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var claimed = new Dictionary<string, Dictionary<string, FileItem>>(StringComparer.OrdinalIgnoreCase);

            // Pass 1：对全部文件求值（单项失败不影响其余）
            for (int k = 0; k < fileList.Count; k++)
            {
                FileItem item = fileList[k];
                item.Index = k;
                item.IssueMessage = null;
                item.Status = RenameStatus.Ok;

                LoadTimes(item);

                try
                {
                    if (!Evaluate(item))
                    {
                        throw new InvalidOperationException("表达式求值返回失败");
                    }
                }
                catch (Exception ex)
                {
                    item.Status = RenameStatus.Error;
                    item.IssueMessage = ex.Message;
                    plan.Issues.Add(new RenameIssue(item, IssueKind.EvalError, "表达式求值失败：" + ex.Message));
                }
            }

            // 构建“将被改名移走”的映射：(目录|原名) -> 对应 FileItem。
            // 用于占用检测时跳过批次内会被移走的源文件，从而支持循环/链式改名。
            var willMove = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in fileList)
            {
                if (item.Status == RenameStatus.Error)
                {
                    continue;
                }
                string dir = GetDirectory(item);
                string originalName = System.IO.Path.GetFileName(item.Path);
                string candidate = item.DstName ?? string.Empty;
                if (candidate.Length == 0)
                {
                    continue;
                }
                if (!string.Equals(candidate, originalName, StringComparison.OrdinalIgnoreCase))
                {
                    willMove[BuildMoveKey(dir, originalName)] = item;
                }
            }

            // Pass 2：文件名校验与冲突检测
            foreach (FileItem item in fileList)
            {
                if (item.Status == RenameStatus.Error)
                {
                    continue;
                }

                string candidate = item.DstName ?? string.Empty;
                if (candidate.Length == 0)
                {
                    AddError(plan, item, IssueKind.EmptyName, "目标文件名为空");
                    continue;
                }
                char invalid = IndexOfInvalidChar(candidate);
                if (invalid != '\0')
                {
                    AddError(plan, item, IssueKind.InvalidChar, "目标文件名含非法字符 '" + invalid + "'：" + candidate);
                    continue;
                }

                string dir = GetDirectory(item);

                bool isNoChange = IsSameAsSource(item, candidate);
                string resolved = candidate;
                bool duplicated = IsClaimed(claimed, dir, resolved, item, out FileItem clashItem);
                bool occupied = false;
                string existingPath = null;
                if (!duplicated)
                {
                    occupied = IsExistingOccupied(existingCache, willMove, dir, item, resolved, out existingPath);
                }

                if ((duplicated || occupied) && mode == ConflictMode.Auto)
                {
                    resolved = ResolveUnique(existingCache, claimed, willMove, dir, item, candidate);
                    item.DstName = resolved;
                    item.Status = RenameStatus.Resolved;
                    item.IssueMessage = "原名冲突，已自动调整为：" + resolved;
                    plan.Warnings.Add(new RenameWarning(item, (item.SrcName ?? item.Path) + " 与其他文件冲突，已调整为 " + resolved));
                }
                else if (duplicated)
                {
                    AddError(plan, item, IssueKind.DuplicateTarget, "目标名与批次内另一文件冲突：" + resolved, clashItem != null ? clashItem.Path : null);
                    continue;
                }
                else if (occupied)
                {
                    AddError(plan, item, IssueKind.TargetExists, "目标文件已存在：" + existingPath, existingPath);
                    continue;
                }

                if (item.Status == RenameStatus.Ok && isNoChange)
                {
                    item.Status = RenameStatus.NoChange;
                    item.IssueMessage = "目标名与原名相同，将跳过";
                }

                Claim(claimed, dir, resolved, item);
            }

            return plan;
        }

        private static string BuildMoveKey(string dir, string name)
        {
            return (dir ?? "\0virtual") + "|" + name;
        }

        /// <summary>
        /// 预览通过后提交磁盘改名。采用两阶段策略：
        /// Phase 1：每个文件在其所在目录内改名为唯一临时名（消解循环改名链 a↔b）；
        /// Phase 2：再从临时名改到最终名。
        /// Phase 1 任一失败则整体回滚；Phase 2 失败尽力回退到原名并上报。
        /// 临时名暂存在 <see cref="FileItem.TmpName"/>，完成后清空。
        /// </summary>
        public RenamePlan Commit(List<FileItem> fileList, ConflictMode mode = ConflictMode.Fail)
        {
            RenamePlan plan = Preview(fileList, mode);
            if (!plan.CanCommit)
            {
                return plan;
            }

            var workItems = new List<FileItem>();
            foreach (FileItem item in plan.Items)
            {
                if (item.Status == RenameStatus.NoChange || item.Status == RenameStatus.Error)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(item.Path))
                {
                    AddError(plan, item, IssueKind.IoError, "缺少文件完整路径，无法执行改名");
                    continue;
                }

                string dir = Path.GetDirectoryName(item.Path);
                string finalPath = Path.Combine(dir ?? string.Empty, item.DstName);
                if (string.Equals(item.Path, finalPath, StringComparison.Ordinal))
                {
                    item.Status = RenameStatus.NoChange;
                    continue;
                }

                item.TmpName = GenerateTempName(dir, item.Path);
                workItems.Add(item);
            }

            if (workItems.Count == 0)
            {
                return plan;
            }

            // Phase 1：原 → 临时名
            foreach (var item in workItems)
            {
                string dir = Path.GetDirectoryName(item.Path);
                string tempPath = Path.Combine(dir, item.TmpName);
                try
                {
                    File.Move(item.Path, tempPath);
                }
                catch (Exception ex)
                {
                    RollbackPhase1(workItems);
                    AddError(plan, item, IssueKind.IoError, "改名失败（阶段1）：" + ex.Message, tempPath);
                    return plan;
                }
            }

            // Phase 2：临时名 → 最终名
            foreach (var item in workItems)
            {
                string dir = Path.GetDirectoryName(item.Path);
                string tempPath = Path.Combine(dir, item.TmpName);
                string finalPath = Path.Combine(dir, item.DstName);
                try
                {
                    File.Move(tempPath, finalPath);
                    item.Path = finalPath;
                    item.Status = RenameStatus.Ok;
                    item.TmpName = null;
                }
                catch (Exception ex)
                {
                    AddError(plan, item, IssueKind.IoError,
                        "改名失败（阶段2）：" + ex.Message + "，文件当前位于：" + tempPath,
                        finalPath);
                    TryRestoreToOriginal(item);
                }
            }

            return plan;
        }

        /// <summary>
        /// 生成目录内唯一的临时文件名（保留扩展名，便于识别）。
        /// </summary>
        private static string GenerateTempName(string dir, string originalPath)
        {
            string ext = Path.GetExtension(originalPath);
            string tempName;
            string tempPath;
            do
            {
                tempName = TempPrefix + Guid.NewGuid().ToString("N") + ext;
                tempPath = Path.Combine(dir, tempName);
            } while (File.Exists(tempPath));
            return tempName;
        }

        /// <summary>
        /// Phase 1 失败回滚：把所有已改名到临时名的文件还原为原名。
        /// 此时所有原名均未被占用，回滚安全。
        /// </summary>
        private static void RollbackPhase1(List<FileItem> workItems)
        {
            foreach (var item in workItems)
            {
                if (string.IsNullOrEmpty(item.TmpName))
                {
                    continue;
                }
                string dir = Path.GetDirectoryName(item.Path);
                string tempPath = Path.Combine(dir, item.TmpName);
                if (File.Exists(tempPath) && !File.Exists(item.Path))
                {
                    try
                    {
                        File.Move(tempPath, item.Path);
                    }
                    catch
                    {
                        // 回滚失败不抛出，保留临时名由用户手动处理
                    }
                }
            }
        }

        /// <summary>
        /// Phase 2 失败后尽力把文件从临时名还原为原名；若原名已被占用则放弃，保留临时名。
        /// </summary>
        private static void TryRestoreToOriginal(FileItem item)
        {
            if (string.IsNullOrEmpty(item.TmpName))
            {
                return;
            }
            string dir = Path.GetDirectoryName(item.Path);
            string tempPath = Path.Combine(dir, item.TmpName);
            if (!File.Exists(tempPath))
            {
                return;
            }
            if (File.Exists(item.Path))
            {
                return; // 原名已被其他改名结果占用，无法回退
            }
            try
            {
                File.Move(tempPath, item.Path);
            }
            catch
            {
                // 保留在临时名，由用户处理
            }
        }

        private bool Evaluate(FileItem item)
        {
            item.DstName = string.Empty;
            foreach (RenamePattern pattern in this.patterns)
            {
                if (!pattern.GenName(item))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 路径可读时用磁盘时间填充文件时间；读取失败保留默认（当前时间）。
        /// </summary>
        private static void LoadTimes(FileItem item)
        {
            if (string.IsNullOrEmpty(item.Path) || !File.Exists(item.Path))
            {
                return;
            }

            try
            {
                item.CreationTime = File.GetCreationTime(item.Path);
                item.LastWriteTime = File.GetLastWriteTime(item.Path);
                item.LastAccessTime = File.GetLastAccessTime(item.Path);
            }
            catch
            {
                // 时间不可读时不阻断重命名，保留默认时间
            }
        }

        private static void AddError(RenamePlan plan, FileItem item, IssueKind kind, string message, string otherPath = null)
        {
            item.Status = RenameStatus.Error;
            item.IssueMessage = message;
            plan.Issues.Add(new RenameIssue(item, kind, message, otherPath));
        }

        private static char IndexOfInvalidChar(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in name)
            {
                if (Array.IndexOf(invalid, c) >= 0)
                {
                    return c;
                }
            }
            return '\0';
        }

        private static string GetDirectory(FileItem item)
        {
            if (string.IsNullOrEmpty(item.Path))
            {
                return null;
            }
            string dir = Path.GetDirectoryName(item.Path);
            return string.IsNullOrEmpty(dir) ? null : dir;
        }

        /// <summary>
        /// 大小写完全匹配
        /// </summary>
        /// <param name="item"></param>
        /// <param name="candidate"></param>
        /// <returns></returns>
        private static bool IsSameAsSource(FileItem item, string candidate)
        {
            if (!string.IsNullOrEmpty(item.Path))
            {
                return string.Equals(Path.GetFileName(item.Path), candidate, StringComparison.Ordinal);
            }
            return string.Equals(item.SrcName, candidate, StringComparison.Ordinal);
        }

        private static Dictionary<string, FileItem> GetDirMap(
            Dictionary<string, Dictionary<string, FileItem>> maps, string dir)
        {
            string key = dir ?? "\0virtual";
            if (!maps.TryGetValue(key, out Dictionary<string, FileItem> map))
            {
                map = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);
                maps[key] = map;
            }
            return map;
        }

        private static bool IsClaimed(
            Dictionary<string, Dictionary<string, FileItem>> claimed,
            string dir, string name, FileItem self, out FileItem other)
        {
            Dictionary<string, FileItem> map = GetDirMap(claimed, dir);
            if (map.TryGetValue(name, out other) && !ReferenceEquals(other, self))
            {
                return true;
            }
            other = null;
            return false;
        }

        private static void Claim(
            Dictionary<string, Dictionary<string, FileItem>> claimed,
            string dir, string name, FileItem item)
        {
            GetDirMap(claimed, dir)[name] = item;
        }

        private static HashSet<string> GetExisting(
            Dictionary<string, HashSet<string>> cache, string dir)
        {
            string key = dir ?? "\0virtual";
            if (cache.TryGetValue(key, out HashSet<string> set))
            {
                return set;
            }

            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            cache[key] = set;
            if (dir != null)
            {
                try
                {
                    foreach (string file in Directory.EnumerateFiles(dir))
                    {
                        set.Add(Path.GetFileName(file));
                    }
                }
                catch
                {
                    // 目录不可读时按“无占用信息”处理，提交阶段仍会由系统拦截
                }
            }
            return set;
        }

        private static bool IsExistingOccupied(
            Dictionary<string, HashSet<string>> cache,
            Dictionary<string, FileItem> willMove,
            string dir, FileItem item, string name, out string existingPath)
        {
            existingPath = null;
            if (dir == null)
            {
                return false;
            }

            HashSet<string> set = GetExisting(cache, dir);
            if (!set.Contains(name))
            {
                return false;
            }

            string full = Path.Combine(dir, name);
            if (string.Equals(full, item.Path, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 若占用者是本批次内的文件且将被改名移走，则视为可用（支持循环/链式改名）
            if (willMove.TryGetValue(BuildMoveKey(dir, name), out FileItem mover))
            {
                if (!string.Equals(mover.DstName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            existingPath = full;
            return true;
        }

        /// <summary>
        /// 生成不与批次内已占用名、磁盘现有文件冲突的“原名 (n).ext”形式名称。
        /// </summary>
        private static string ResolveUnique(
            Dictionary<string, HashSet<string>> cache,
            Dictionary<string, Dictionary<string, FileItem>> claimed,
            Dictionary<string, FileItem> willMove,
            string dir, FileItem item, string candidate)
        {
            NamePattern.SplitName(candidate, out string baseName, out string ext);
            string suffixExt = ext.Length > 0 ? "." + ext : string.Empty;

            for (int n = 1; n < int.MaxValue; n++)
            {
                string tryName = baseName + " (" + n.ToString() + ")" + suffixExt;
                if (!IsClaimed(claimed, dir, tryName, item, out _) &&
                    !IsExistingOccupied(cache, willMove, dir, item, tryName, out _))
                {
                    return tryName;
                }
            }
            // 理论不可达
            throw new InvalidOperationException("无法为冲突文件生成唯一名称：" + candidate);
        }
    }
}
