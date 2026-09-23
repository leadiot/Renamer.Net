using Com.Scm.Filexp.Rename.Dvo;
using System;
using System.Text;

namespace Com.Scm.Renamer.Pattern
{
    /// <summary>
    /// 文件名称/扩展名表达式。
    /// 语法（body 为 &lt; &gt; 内部文本）：
    /// <code>
    /// body := '*' | '?' | case | positional | textReplace
    /// case := '|\' | '|/' | '\\' | '//'              （规范形式为 |\、|/，双写形式兼容旧文档）
    /// positional := dir digits ins | dir digits del | dir digits cut | dir digits cut '|' text
    /// dir  := '\' 左起 | '/' 右起
    /// ins  := '\' text                                定位点后插入
    /// del  := '/' digits                              定位点后向右删除
    /// cut  := ':' digits [ '|' text ]                 截取区间；带 text 时为区间替换
    /// textReplace := text '|' text                    全部替换，区分大小写
    /// </code>
    /// 位置约定：下标 1 基、含端点；方向只决定定位点的计数起点，操作一律向右执行。
    /// 左起定位点 p = i（第 i 个字符之后）；右起定位点 p = n - i + 1（倒数第 i 个字符之后）。
    /// 左起截取区间 [i,j]；右起截取区间 [n-j+1, n-i+1]，保持原字符顺序。
    /// 越界一律夹紧（clamp）到有效范围，不抛异常。
    /// </summary>
    internal class NamePattern : RenamerPattern
    {
        /// <summary>
        /// true 表示作用于扩展名（?），false 表示作用于文件名（*）
        /// </summary>
        public bool IsExtension { get; protected set; }

        /// <summary>
        /// 方向
        /// </summary>
        public DirEnum Dir { get; set; }

        /// <summary>
        /// 操作
        /// </summary>
        public OptEnum Opt { get; set; }

        /// <summary>
        /// 转小写
        /// </summary>
        public bool ToLower { get; set; }
        /// <summary>
        /// 转大写
        /// </summary>
        public bool ToUpper { get; set; }

        /// <summary>
        /// 起始位置（1 基）
        /// </summary>
        public int From { get; set; }
        /// <summary>
        /// 结束位置（1 基，含端点）
        /// </summary>
        public int To { get; set; }

        /// <summary>
        /// 字符长度
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 来源文本（文字替换的被替换串）
        /// </summary>
        public string SrcText { get; set; }
        /// <summary>
        /// 目标文本（插入/替换文本）
        /// </summary>
        public string DstText { get; set; }

        /// <summary>
        /// 解析表达式内容（不含 &lt; &gt;）。bodyPos 为 body 在整个模板中的起始位置。
        /// </summary>
        internal void DecodeBody(string body, int bodyPos)
        {
            if (string.IsNullOrEmpty(body))
            {
                throw new TemplateParseException("表达式内容为空", bodyPos);
            }

            char tag = body[0];
            if (tag != PATTERN_PREFIX[0] && tag != PATTERN_SUFFIX[0])
            {
                throw new TemplateParseException("名称表达式必须以 * 或 ? 开头", bodyPos);
            }
            IsExtension = tag == PATTERN_SUFFIX[0];

            string rest = body.Substring(1);
            if (rest.Length == 0)
            {
                return;
            }

            // 大小写转换：规范形式 |\、|/；兼容旧文档的双写 \\、//
            if (rest == PATTERN_ESCAPE + PATTERN_OPT_UPPER.ToString() ||
                rest == PATTERN_OPT_UPPER.ToString() + PATTERN_OPT_UPPER.ToString())
            {
                ToUpper = true;
                return;
            }
            if (rest == PATTERN_ESCAPE + PATTERN_OPT_LOWER.ToString() ||
                rest == PATTERN_OPT_LOWER.ToString() + PATTERN_OPT_LOWER.ToString())
            {
                ToLower = true;
                return;
            }

            // 按位置的操作
            if (rest[0] == PATTERN_DIR_LEFT || rest[0] == PATTERN_DIR_RIGHT)
            {
                DecodePositional(rest, bodyPos + 1);
                return;
            }

            // 文字替换 src|dst
            int bar = rest.IndexOf(PATTERN_ESCAPE);
            if (bar > 0)
            {
                Opt = OptEnum.Replace2;
                SrcText = rest.Substring(0, bar);
                DstText = rest.Substring(bar + 1);
                return;
            }

            throw new TemplateParseException("无法识别的名称表达式：" + body, bodyPos);
        }

        private void DecodePositional(string rest, int restPos)
        {
            Dir = rest[0] == PATTERN_DIR_LEFT ? DirEnum.Left : DirEnum.Right;

            int i = 1;
            long from = TemplateParser.ReadDigits(rest, ref i, restPos, "起始位置 i");
            if (from < 1)
            {
                throw new TemplateParseException("位置必须从 1 开始", restPos + 1);
            }
            From = (int)from;

            if (i >= rest.Length)
            {
                throw new TemplateParseException("缺少操作类型（\\ 插入、/ 删除、: 截取）", restPos + i);
            }

            char op = rest[i];
            if (op == PATTERN_OPT_INSERT)
            {
                // \i\zz 插入
                Opt = OptEnum.Insert;
                DstText = rest.Substring(i + 1);
                return;
            }

            if (op == PATTERN_OPT_DELETE)
            {
                // \i/l 删除
                i++;
                long len = TemplateParser.ReadDigits(rest, ref i, restPos, "删除长度 l");
                if (len < 1)
                {
                    throw new TemplateParseException("删除长度必须大于 0", restPos + i);
                }
                Length = (int)len;
                TemplateParser.RequireEnd(rest, i, restPos);
                Opt = OptEnum.Delete;
                return;
            }

            if (op == PATTERN_SPLIT)
            {
                // \i:j 截取 / \i:j|zz 区间替换
                i++;
                long to = TemplateParser.ReadDigits(rest, ref i, restPos, "结束位置 j");
                if (to < 1)
                {
                    throw new TemplateParseException("位置必须从 1 开始", restPos + i);
                }
                To = (int)to;

                if (i >= rest.Length)
                {
                    Opt = OptEnum.Cut;
                    return;
                }
                if (rest[i] == PATTERN_ESCAPE)
                {
                    Opt = OptEnum.Replace1;
                    DstText = rest.Substring(i + 1);
                    return;
                }
            }

            throw new TemplateParseException("无法识别的位置操作：" + rest, restPos);
        }

        public override bool GenName(FileItem item)
        {
            SplitName(item.SrcName ?? string.Empty, out string name, out string ext);
            string src = IsExtension ? ext : name;

            string result;
            if (ToUpper)
            {
                result = src.ToUpperInvariant();
            }
            else if (ToLower)
            {
                result = src.ToLowerInvariant();
            }
            else
            {
                result = Apply(src);
            }

            item.DstName += result;
            return true;
        }

        private string Apply(string src)
        {
            int n = src.Length;

            switch (Opt)
            {
                case OptEnum.Insert:
                    {
                        int p = Dir == DirEnum.Right ? n - From + 1 : From;
                        p = Clamp(p, 0, n);
                        return src.Substring(0, p) + (DstText ?? string.Empty) + src.Substring(p);
                    }
                case OptEnum.Delete:
                    {
                        int p = Dir == DirEnum.Right ? n - From + 1 : From;
                        p = Clamp(p, 0, n);
                        int count = Math.Min(Length, n - p);
                        if (count <= 0)
                        {
                            return src;
                        }
                        return src.Substring(0, p) + src.Substring(p + count);
                    }
                case OptEnum.Cut:
                case OptEnum.Replace1:
                    {
                        int start0;
                        int end0;
                        if (Dir == DirEnum.Right)
                        {
                            // 右起区间 [n-j+1, n-i+1]（0 基半开）
                            start0 = n - To;
                            end0 = n - From + 1;
                        }
                        else
                        {
                            start0 = From - 1;
                            end0 = To;
                        }
                        start0 = Clamp(start0, 0, n);
                        end0 = Clamp(end0, start0, n);

                        if (Opt == OptEnum.Cut)
                        {
                            return src.Substring(start0, end0 - start0);
                        }
                        return src.Substring(0, start0) + (DstText ?? string.Empty) + src.Substring(end0);
                    }
                case OptEnum.Replace2:
                    return ReplaceAll(src, SrcText ?? string.Empty, DstText ?? string.Empty);
                default:
                    return src;
            }
        }

        /// <summary>
        /// 区分大小写的全部替换（序号比较）
        /// </summary>
        private static string ReplaceAll(string src, string find, string repl)
        {
            if (find.Length == 0)
            {
                return src;
            }

            var sb = new StringBuilder(src.Length);
            int idx = 0;
            int found;
            while ((found = src.IndexOf(find, idx, StringComparison.Ordinal)) >= 0)
            {
                sb.Append(src, idx, found - idx);
                sb.Append(repl);
                idx = found + find.Length;
            }
            sb.Append(src, idx, src.Length - idx);
            return sb.ToString();
        }

        /// <summary>
        /// 拆分文件名：取最后一个点之后为扩展名；点开头文件（.gitignore）与无点文件视为无扩展名。
        /// </summary>
        internal static void SplitName(string full, out string name, out string extension)
        {
            int dot = full.LastIndexOf('.');
            if (dot <= 0)
            {
                name = full;
                extension = string.Empty;
            }
            else
            {
                name = full.Substring(0, dot);
                extension = full.Substring(dot + 1);
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        protected string GenPattern(string tag)
        {
            var pattern = tag;
            if (ToUpper)
            {
                return pattern + PATTERN_ESCAPE + PATTERN_OPT_UPPER;
            }
            if (ToLower)
            {
                return pattern + PATTERN_ESCAPE + PATTERN_OPT_LOWER;
            }

            if (Opt == OptEnum.Insert)
            {
                pattern += GetDir();
                pattern += From;
                pattern += PATTERN_OPT_INSERT + DstText;
                return pattern;
            }

            if (Opt == OptEnum.Delete)
            {
                pattern += GetDir();
                pattern += From;
                pattern += PATTERN_OPT_DELETE + Length;
                return pattern;
            }

            if (Opt == OptEnum.Cut)
            {
                pattern += GetDir();
                pattern += From;
                pattern += PATTERN_SPLIT + To;
                return pattern;
            }

            if (Opt == OptEnum.Replace1)
            {
                pattern += GetDir();
                pattern += From;
                pattern += PATTERN_SPLIT + To;
                pattern += PATTERN_ESCAPE + DstText;
                return pattern;
            }

            if (Opt == OptEnum.Replace2)
            {
                pattern += SrcText;
                pattern += PATTERN_ESCAPE + DstText;
                return pattern;
            }
            return tag;
        }

        protected char GetDir()
        {
            if (Dir == DirEnum.Left)
            {
                return PATTERN_DIR_LEFT;
            }
            if (Dir == DirEnum.Right)
            {
                return PATTERN_DIR_RIGHT;
            }
            return ' ';
        }
    }

    public enum DirEnum
    {
        None,
        Left,
        Right,
    }

    public enum OptEnum
    {
        /// <summary>
        /// 原样输出
        /// </summary>
        None,
        /// <summary>
        /// 截取
        /// </summary>
        Cut,
        /// <summary>
        /// 追加
        /// </summary>
        Insert,
        /// <summary>
        /// 删除
        /// </summary>
        Delete,
        /// <summary>
        /// 按区间替换
        /// </summary>
        Replace1,
        /// <summary>
        /// 按文字替换
        /// </summary>
        Replace2
    }
}
