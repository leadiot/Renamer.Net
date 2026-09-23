using Com.Scm.Filexp.Rename.Dvo;
using System.Collections.Generic;

namespace Com.Scm.Renamer
{
    public class Renamer2
    {
        private List<RenamerPattern> patterns;

        public void Add(RenamerPattern pattern)
        {
            this.patterns.Add(pattern);
        }

        public bool Rename(List<FileItem> fileList)
        {
            foreach (FileItem item in fileList)
            {
                if (!GenName(item))
                {
                    return false;
                }
            }
            return true;
        }

        private bool GenName(FileItem item)
        {
            item.DstName = "";
            foreach (var pattern in this.patterns)
            {
                if (!pattern.GenName(item))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
