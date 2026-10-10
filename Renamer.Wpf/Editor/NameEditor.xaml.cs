using Com.Scm.Renamer.Pattern;

namespace Com.Scm.Renamer.Editor
{
    /// <summary>
    /// 名称字段编辑面板：大小写、截取/插入/删除/替换等操作配置，
    /// 供 PrefixEditor / SurfixEditor 等编辑窗体复用。
    /// </summary>
    public partial class NameEditor : System.Windows.Controls.UserControl
    {
        public NameEditor()
        {
            InitializeComponent();
        }

        /// <summary>从模型加载当前值到输入控件。</summary>
        public void LoadFrom(NamePattern pattern)
        {
            CaseBox.SelectedIndex = pattern.ToUpper ? 1 : (pattern.ToLower ? 2 : 0);
            OpBox.SelectedIndex = pattern.Opt switch
            {
                OptEnum.Cut => 1,
                OptEnum.Insert => 2,
                OptEnum.Delete => 3,
                OptEnum.Replace1 => 4,
                OptEnum.Replace2 => 5,
                _ => 0
            };
            DirBox.SelectedIndex = pattern.Dir == DirEnum.Right ? 1 : 0;
            FromBox.Text = pattern.From > 0 ? pattern.From.ToString() : "";
            ToBox.Text = pattern.To > 0 ? pattern.To.ToString() : "";
            LenBox.Text = pattern.Length > 0 ? pattern.Length.ToString() : "";
            SrcBox.Text = pattern.SrcText ?? "";
            DstBox.Text = pattern.DstText ?? "";
        }

        /// <summary>将输入控件值回写到模型。</summary>
        public void ApplyTo(NamePattern pattern)
        {
            pattern.ToUpper = CaseBox.SelectedIndex == 1;
            pattern.ToLower = CaseBox.SelectedIndex == 2;
            pattern.Opt = OpBox.SelectedIndex switch
            {
                1 => OptEnum.Cut,
                2 => OptEnum.Insert,
                3 => OptEnum.Delete,
                4 => OptEnum.Replace1,
                5 => OptEnum.Replace2,
                _ => OptEnum.None
            };
            pattern.Dir = DirBox.SelectedIndex == 1 ? DirEnum.Right : DirEnum.Left;
            pattern.From = ParseInt(FromBox.Text, 1);
            pattern.To = ParseInt(ToBox.Text, 1);
            pattern.Length = ParseInt(LenBox.Text, 1);
            pattern.SrcText = SrcBox.Text;
            pattern.DstText = DstBox.Text;
        }

        private static int ParseInt(string s, int defaultValue)
        {
            return int.TryParse(s?.Trim(), out int v) ? v : defaultValue;
        }
    }
}
