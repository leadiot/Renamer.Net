using Com.Scm.Renamer;

namespace Com.Scm.Renamer.Dvo
{
    public class FileItemDvo : ScmDvo
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

        public FileItem ToCoreItem()
        {
            return new FileItem
            {
                Path = Path,
                SrcName = OriginalName
            };
        }

        public void ApplyCoreItem(FileItem item)
        {
            Path = item.Path ?? string.Empty;
            OriginalName = item.SrcName ?? System.IO.Path.GetFileName(item.Path ?? string.Empty);
            ResultName = item.DstName ?? string.Empty;
            ExceptionMessage = item.IssueMessage ?? string.Empty;
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
