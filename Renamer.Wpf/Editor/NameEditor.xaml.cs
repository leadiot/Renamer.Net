using Com.Scm.Renamer.Pattern;
using System.Windows;

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

        // OpBox 下拉项索引：0 不变 / 1 转大写 / 2 转小写 / 3 截取 / 4 插入 / 5 删除 / 6 区间替换 / 7 文字替换
        private const int IdxNone = 0;
        private const int IdxUpper = 1;
        private const int IdxLower = 2;
        private const int IdxCut = 3;
        private const int IdxInsert = 4;
        private const int IdxDelete = 5;
        private const int IdxReplace1 = 6;
        private const int IdxReplace2 = 7;

        /// <summary>从模型加载当前值到输入控件。</summary>
        public void LoadFrom(NamePattern pattern)
        {
            CbOpt.SelectedIndex = pattern.ToUpper ? IdxUpper
                : pattern.ToLower ? IdxLower
                : pattern.Opt switch
                {
                    OptEnum.Cut => IdxCut,
                    OptEnum.Insert => IdxInsert,
                    OptEnum.Delete => IdxDelete,
                    OptEnum.Replace1 => IdxReplace1,
                    OptEnum.Replace2 => IdxReplace2,
                    _ => IdxNone
                };
            CbDir.SelectedIndex = pattern.Dir == DirEnum.Right ? 1 : 0;
            TbFrom.Value = pattern.From;
            TbTo.Value = pattern.To;
            TbLength.Value = pattern.Length;
            TbSrc.Text = pattern.SrcText ?? "";
            TbDst.Text = pattern.DstText ?? "";
        }

        /// <summary>将输入控件值回写到模型。</summary>
        public void ApplyTo(NamePattern pattern)
        {
            // 大小写转换与位置操作互斥：选中转大写/转小写时清空 Opt，选中位置操作时清空大小写标志
            pattern.ToUpper = CbOpt.SelectedIndex == IdxUpper;
            pattern.ToLower = CbOpt.SelectedIndex == IdxLower;
            pattern.Opt = CbOpt.SelectedIndex switch
            {
                IdxCut => OptEnum.Cut,
                IdxInsert => OptEnum.Insert,
                IdxDelete => OptEnum.Delete,
                IdxReplace1 => OptEnum.Replace1,
                IdxReplace2 => OptEnum.Replace2,
                _ => OptEnum.None
            };
            pattern.Dir = CbDir.SelectedIndex == 1 ? DirEnum.Right : DirEnum.Left;
            pattern.From = (int)TbFrom.Value;
            pattern.To = (int)TbTo.Value;
            pattern.Length = (int)TbLength.Value;
            pattern.SrcText = TbSrc.Text;
            pattern.DstText = TbDst.Text;
        }

        private void CbOpt_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // InitializeComponent 添加候选项期间不触发选中变更，这里只需防空引用
            if (LbFrom == null)
            {
                return;
            }

            switch (CbOpt.SelectedIndex)
            {
                case IdxCut:           // 截取：From + To
                    SetFields(dir: true, from: true, to: true);
                    break;
                case IdxInsert:        // 插入：From + Dst
                    SetFields(dir: true, from: true, dst: true);
                    break;
                case IdxDelete:        // 删除：From + To（位置区间）
                    SetFields(dir: true, from: true, to: true);
                    break;
                case IdxReplace1:      // 区间替换：From + To + Dst
                    SetFields(dir: true, from: true, to: true, dst: true);
                    break;
                case IdxReplace2:      // 文字替换：Src + Dst
                    SetFields(src: true, dst: true);
                    break;
                default:
                    // 不变 / 转大写 / 转小写：无参数字段，保持全部隐藏
                    SetFields(dir: false, from: false, to: false, length: false, src: false, dst: false);
                    break;
            }
        }

        private void SetFields(bool dir = false, bool from = false, bool to = false, bool length = false, bool src = false, bool dst = false)
        {
            Visibility v(bool show) => show ? Visibility.Visible : Visibility.Collapsed;
            LbDir.Visibility = CbDir.Visibility = v(dir);
            LbFrom.Visibility = TbFrom.Visibility = v(from);
            LbTo.Visibility = TbTo.Visibility = v(to);
            LbLength.Visibility = TbLength.Visibility = v(length);
            LbSrc.Visibility = TbSrc.Visibility = v(src);
            LbDst.Visibility = TbDst.Visibility = v(dst);
        }
    }
}
