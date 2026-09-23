using Com.Scm.Filexp.Rename.Dvo;
using System;
using System.Collections.Generic;
using System.IO;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 批量重命名执行器：模板编译一次，按文件顺序逐一生成 DstName。
    /// 数字/字母流水依据 FileItem.Index（本批次中的 0 基序号）计算。
    /// </summary>
    public class Renamer2
    {
        private readonly List<RenamerPattern> patterns = new List<RenamerPattern>();

        /// <summary>
        /// 编译模板并返回执行器；模板非法时抛出 <see cref="TemplateParseException"/>。
        /// </summary>
        public static Renamer2 Compile(string template)
        {
            var renamer = new Renamer2();
            foreach (RenamerPattern pattern in TemplateParser.Parse(template))
            {
                renamer.Add(pattern);
            }
            return renamer;
        }

        public void Add(RenamerPattern pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            this.patterns.Add(pattern);
        }

        /// <summary>
        /// 为批次内每个文件生成 DstName。任一文件失败立即中止，
        /// 失败原因写入该 FileItem 的 ErrorCode/ErrorMessage。
        /// </summary>
        public bool Rename(List<FileItem> fileList)
        {
            if (fileList == null)
            {
                throw new ArgumentNullException(nameof(fileList));
            }

            for (int k = 0; k < fileList.Count; k++)
            {
                FileItem item = fileList[k];
                item.Index = k;
                item.ErrorCode = 0;
                item.ErrorMessage = null;

                LoadTimes(item);

                try
                {
                    item.DstName = string.Empty;
                    foreach (RenamerPattern pattern in this.patterns)
                    {
                        if (!pattern.GenName(item))
                        {
                            item.ErrorCode = 1;
                            item.ErrorMessage = "表达式求值失败：" + pattern.Pattern;
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    item.ErrorCode = 1;
                    item.ErrorMessage = ex.Message;
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
    }
}
