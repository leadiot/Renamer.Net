namespace Com.Scm.Renamer
{
    public class FileItemDvo : FileItem
    {
        public string Path { get; set; }
        public string SrcName { get; set; }
        public int Index { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public string DstName { get; set; }
        public RenameStatus Status { get; set; }
        public string IssueMessage { get; set; }
    }
}
