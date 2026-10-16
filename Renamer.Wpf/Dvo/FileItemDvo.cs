namespace Com.Scm.Renamer.Dvo
{
    public class FileItemDvo : ScmDvo, FileItem
    {

        public string Path { get; set; }

        private string _resultStatus = string.Empty;
        public string ResultStatus
        {
            get => _resultStatus;
            set { _resultStatus = value; OnPropertyChanged(nameof(ResultStatus)); }
        }

        private string srcName;
        public string SrcName { get { return srcName; } set { srcName = value; OnPropertyChanged(nameof(SrcName)); } }

        private string dstName;
        public string DstName { get { return dstName; } set { dstName = value; OnPropertyChanged(nameof(DstName)); } }

        public int Index { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public RenameStatus Status { get; set; }

        private string issueMessage = string.Empty;

        public string IssueMessage
        {
            get => issueMessage;
            set { issueMessage = value; OnPropertyChanged(nameof(IssueMessage)); }
        }

        private string tmpName = string.Empty;

        public string TmpName
        {
            get => tmpName;
            set { tmpName = value; OnPropertyChanged(nameof(TmpName)); }
        }

        public FileItem ToItem()
        {
            return this;
        }

        public void ApplyCoreItem(FileItem item)
        {
            Path = item.Path ?? string.Empty;
            SrcName = item.SrcName ?? System.IO.Path.GetFileName(item.Path ?? string.Empty);
            DstName = item.DstName ?? string.Empty;
            IssueMessage = item.IssueMessage ?? string.Empty;
            ResultStatus = item.Status switch
            {
                RenameStatus.Ok => "成功",
                RenameStatus.NoChange => "跳过",
                RenameStatus.Resolved => "已消解",
                RenameStatus.Error => "失败",
                _ => string.Empty
            };
        }
    }
}
