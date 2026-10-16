using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 一条非阻断性提示（如冲突已被 AutoSuffix 自动消解）
    /// </summary>
    public class RenameWarning
    {
        public RenameWarning(FileItem item, string message)
        {
            Item = item;
            Message = message;
        }

        public FileItem Item { get; }

        public string Message { get; }

        public override string ToString()
        {
            return Message;
        }
    }
}
