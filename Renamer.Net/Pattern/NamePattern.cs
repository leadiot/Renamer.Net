using Com.Scm.Filexp.Rename.Dvo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Com.Scm.Renamer.Pattern
{
    /// <summary>
    /// 文件名称/扩展名表达式。
    /// 一、旧式单操作语法（body 为 &lt; &gt; 内部文本）：
    /// <code>
    /// * | ?                                  原样输出
    /// |\、|/                                 转大写/小写（唯一形式）
    /// 文本|文本                               全部替换（区分大小写）
    /// \i\zz、/i\zz                            左起/右起第 i 位后插入 zz
    /// \i/l、/i/l                              左起/右起第 i 位后向右删除 l 个字符
    /// \i:j、/i:j                              左起/右起截取 [i,j]
    /// \i:j|zz、/i:j|zz                        左起/右起区间替换
    /// </code>
    /// 二、管道串联语法（P1）：<b>|fn1|fn2:args|…</b>，前一个函数的输出作为后一个的输入：
    /// <code>
    /// upper | lower                           大小写转换
    /// rep:find:repl                           全部替换；repl 可为空，find 不可含 '|'
    /// cut:L:i:j | cut:R:i:j                   左起/右起截取
    /// ins:L:i:text | ins:R:i:text             左起/右起插入（text 为最后一个参数，可含 ':'）
    /// del:L:i:len | del:R:i:len               左起/右起删除
    /// 例：&lt;*|lower|rep:Abc:123|cut:L:1:9&gt;  先小写、再替换、最后截取
    /// </code>
    /// 位置约定：下标 1 基、含端点；方向只决定定位点的计数起点，操作一律向右执行。
    /// 左起定位点 p = i；右起定位点 p = n - i + 1。
    /// 左起截取 [i,j]；右起截取 [n-j+1, n-i+1]，保持原字符顺序。越界一律夹紧（clamp）。
    /// </summary>
    public class NamePattern : RenamerPattern
    {
        /// <summary>
        /// true 表示作用于扩展名（?），false 表示作用于文件名（*）
        /// </summary>
        public bool IsExtension { get; protected set; }

        /// <summary>
        /// 方向（旧式/构造器 Encode 使用）
        /// </summary>
        public DirEnum Dir { get; set; }

        /// <summary>
        /// 操作（旧式/构造器 Encode 使用）
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
        /// 解析得到的操作管道；null 表示该对象由构造器直接构造、未经模板解析（走旧式字段求值）。
        /// </summary>
        private List<IStringTransform> _transforms;

        /// <summary>
        /// 解析表达式内容（不含 &lt; &gt;）。bodyPos 为 body 在整个模板中的起始位置。
        /// </summary>
        public override bool Decode(string body, int bodyPos)
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
                _transforms = new List<IStringTransform>();
                return true;
            }

            // 大小写转换（唯一形式）：|\ 转大写、|/ 转小写
            if (rest == PATTERN_ESCAPE + PATTERN_OPT_UPPER.ToString())
            {
                ToUpper = true;
                _transforms = new List<IStringTransform> { new CaseTransform(true) };
                return true;
            }
            if (rest == PATTERN_ESCAPE + PATTERN_OPT_LOWER.ToString())
            {
                ToLower = true;
                _transforms = new List<IStringTransform> { new CaseTransform(false) };
                return true;
            }

            // 已废弃的旧文档双写形式 \\、//：给出明确迁移提示
            if (rest == PATTERN_OPT_UPPER.ToString() + PATTERN_OPT_UPPER.ToString() ||
                rest == PATTERN_OPT_LOWER.ToString() + PATTERN_OPT_LOWER.ToString())
            {
                char canonical = rest[0] == PATTERN_OPT_UPPER ? PATTERN_OPT_UPPER : PATTERN_OPT_LOWER;
                throw new TemplateParseException(
                    "不再支持双写形式，请使用规范形式 " + PATTERN_START + body.Substring(0, 1)
                    + PATTERN_ESCAPE + canonical + PATTERN_END, bodyPos + 1);
            }

            // 管道串联：| 后跟字母（|\、|/ 已在上面处理）
            if (rest[0] == PATTERN_ESCAPE && rest.Length > 1 && IsAsciiLetter(rest[1]))
            {
                _transforms = DecodePipeline(rest.Substring(1), bodyPos + 1);
                return true;
            }

            // 旧式按位置操作
            if (rest[0] == PATTERN_DIR_LEFT || rest[0] == PATTERN_DIR_RIGHT)
            {
                _transforms = new List<IStringTransform> { DecodePositional(rest, bodyPos + 1) };
                return true;
            }

            // 旧式文字替换 src|dst
            int bar = rest.IndexOf(PATTERN_ESCAPE);
            if (bar > 0)
            {
                Opt = OptEnum.Replace2;
                SrcText = rest.Substring(0, bar);
                DstText = rest.Substring(bar + 1);
                _transforms = new List<IStringTransform> { new ReplaceTransform(SrcText, DstText) };
                return true;
            }

            throw new TemplateParseException("无法识别的名称表达式：" + body, bodyPos);
        }

        /// <summary>
        /// 解析管道段，如 lower|rep:Abc:123|cut:L:1:9
        /// </summary>
        private List<IStringTransform> DecodePipeline(string chain, int chainPos)
        {
            var list = new List<IStringTransform>();
            int segStart = 0;
            while (segStart <= chain.Length)
            {
                int segEnd = chain.IndexOf(PATTERN_ESCAPE, segStart);
                if (segEnd < 0)
                {
                    segEnd = chain.Length;
                }

                string seg = chain.Substring(segStart, segEnd - segStart);
                if (seg.Length == 0)
                {
                    throw new TemplateParseException("管道函数为空", chainPos + segStart);
                }
                list.Add(DecodeFunction(seg, chainPos + segStart));

                if (segEnd == chain.Length)
                {
                    break;
                }
                segStart = segEnd + 1;
            }
            return list;
        }

        private IStringTransform DecodeFunction(string seg, int segPos)
        {
            int colon = seg.IndexOf(PATTERN_SPLIT);
            string name = colon < 0 ? seg : seg.Substring(0, colon);
            string args = colon < 0 ? string.Empty : seg.Substring(colon + 1);
            int argsPos = colon < 0 ? segPos + name.Length : segPos + colon + 1;

            switch (name)
            {
                case "upper":
                    RequireNoArgs(name, args, argsPos);
                    ToUpper = true;
                    return new CaseTransform(true);
                case "lower":
                    RequireNoArgs(name, args, argsPos);
                    ToLower = true;
                    return new CaseTransform(false);
                case "rep":
                    {
                        // rep:find:repl —— repl 为最后一个参数，允许为空、允许含 ':'
                        int sep = args.IndexOf(PATTERN_SPLIT);
                        if (sep <= 0)
                        {
                            throw new TemplateParseException("rep 需要两个参数：rep:查找文本:替换文本", argsPos);
                        }
                        string find = args.Substring(0, sep);
                        string repl = args.Substring(sep + 1);
                        Opt = OptEnum.Replace2;
                        SrcText = find;
                        DstText = repl;
                        return new ReplaceTransform(find, repl);
                    }
                case "cut":
                case "ins":
                case "del":
                    {
                        var (right, from) = ReadDirAndPos(args, argsPos, out int used, name);
                        if (used >= args.Length || args[used] != PATTERN_SPLIT)
                        {
                            throw new TemplateParseException(name + " 缺少第三个参数", argsPos + used);
                        }
                        string tail = args.Substring(used + 1);

                        if (name == "cut")
                        {
                            int to = ParseInt(tail, argsPos + used + 1, "结束位置 j");
                            if (to < 1)
                            {
                                throw new TemplateParseException("位置必须从 1 开始", argsPos + used + 1);
                            }
                            Opt = OptEnum.Cut;
                            Dir = right ? DirEnum.Right : DirEnum.Left;
                            From = from;
                            To = to;
                            return new CutTransform(right, from, to);
                        }
                        if (name == "ins")
                        {
                            Opt = OptEnum.Insert;
                            Dir = right ? DirEnum.Right : DirEnum.Left;
                            From = from;
                            DstText = tail;
                            return new InsertTransform(right, from, tail);
                        }
                        else
                        {
                            int len = ParseInt(tail, argsPos + used + 1, "删除长度 l");
                            if (len < 1)
                            {
                                throw new TemplateParseException("删除长度必须大于 0", argsPos + used + 1);
                            }
                            Opt = OptEnum.Delete;
                            Dir = right ? DirEnum.Right : DirEnum.Left;
                            From = from;
                            Length = len;
                            return new DeleteTransform(right, from, len);
                        }
                    }
                default:
                    throw new TemplateParseException(
                        "未知管道函数：" + name + "（支持 upper/lower/rep/cut/ins/del）", segPos);
            }
        }

        private static void RequireNoArgs(string name, string args, int argsPos)
        {
            if (args.Length > 0)
            {
                throw new TemplateParseException(name + " 不接受参数", argsPos);
            }
        }

        /// <summary>
        /// 读取“方向:位置”前缀，如 L:3、R:2；返回 consumed 到第二个参数前的 ':' 之前。
        /// </summary>
        private static (bool Right, int From) ReadDirAndPos(string args, int argsPos, out int consumed, string fn)
        {
            if (args.Length < 3 || (args[0] != 'L' && args[0] != 'R') || args[1] != PATTERN_SPLIT)
            {
                throw new TemplateParseException(fn + " 的方向应为 L 或 R，如 " + fn + ":L:1:…", argsPos);
            }
            bool right = args[0] == 'R';

            int i = 2;
            int start = i;
            while (i < args.Length && args[i] != PATTERN_SPLIT)
            {
                if (!char.IsDigit(args[i]))
                {
                    throw new TemplateParseException("位置必须为数字", argsPos + i);
                }
                i++;
            }
            int from = ParseInt(args.Substring(start, i - start), argsPos + start, "起始位置 i");
            if (from < 1)
            {
                throw new TemplateParseException("位置必须从 1 开始", argsPos + start);
            }
            consumed = i;
            return (right, from);
        }

        private static int ParseInt(string s, int pos, string what)
        {
            if (s.Length == 0 || !int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            {
                throw new TemplateParseException("缺少或非法的" + what, pos);
            }
            return value;
        }

        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private IStringTransform DecodePositional(string rest, int restPos)
        {
            bool right;
            if (rest[0] == PATTERN_DIR_LEFT)
            {
                right = false;
                Dir = DirEnum.Left;
            }
            else
            {
                right = true;
                Dir = DirEnum.Right;
            }

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
                Opt = OptEnum.Insert;
                DstText = rest.Substring(i + 1);
                return new InsertTransform(right, From, DstText);
            }

            if (op == PATTERN_OPT_DELETE)
            {
                i++;
                long len = TemplateParser.ReadDigits(rest, ref i, restPos, "删除长度 l");
                if (len < 1)
                {
                    throw new TemplateParseException("删除长度必须大于 0", restPos + i);
                }
                Length = (int)len;
                TemplateParser.RequireEnd(rest, i, restPos);
                Opt = OptEnum.Delete;
                return new DeleteTransform(right, From, Length);
            }

            if (op == PATTERN_SPLIT)
            {
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
                    return new CutTransform(right, From, To);
                }
                if (rest[i] == PATTERN_ESCAPE)
                {
                    Opt = OptEnum.Replace1;
                    DstText = rest.Substring(i + 1);
                    return new RangeReplaceTransform(right, From, To, DstText);
                }
            }

            throw new TemplateParseException("无法识别的位置操作：" + rest, restPos);
        }

        public override bool GenName(FileItem item)
        {
            SplitName(item.SrcName ?? string.Empty, out string name, out string ext);
            string src = IsExtension ? ext : name;

            string result;
            if (_transforms != null)
            {
                result = src;
                foreach (IStringTransform transform in _transforms)
                {
                    result = transform.Apply(result);
                }
            }
            else
            {
                // 构造器直接构造、未经模板解析的对象：沿用旧式字段求值
                result = ApplyLegacy(src);
            }

            item.DstName += result;
            return true;
        }

        private string ApplyLegacy(string src)
        {
            if (ToUpper)
            {
                return src.ToUpperInvariant();
            }
            if (ToLower)
            {
                return src.ToLowerInvariant();
            }

            switch (Opt)
            {
                case OptEnum.Insert:
                    return new InsertTransform(Dir == DirEnum.Right, From, DstText).Apply(src);
                case OptEnum.Delete:
                    return new DeleteTransform(Dir == DirEnum.Right, From, Length).Apply(src);
                case OptEnum.Cut:
                    return new CutTransform(Dir == DirEnum.Right, From, To).Apply(src);
                case OptEnum.Replace1:
                    return new RangeReplaceTransform(Dir == DirEnum.Right, From, To, DstText).Apply(src);
                case OptEnum.Replace2:
                    return new ReplaceTransform(SrcText ?? string.Empty, DstText ?? string.Empty).Apply(src);
                default:
                    return src;
            }
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

        internal static int Clamp(int value, int min, int max)
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

    /// <summary>
    /// 名称字符串变换（管道中的一个函数）
    /// </summary>
    internal interface IStringTransform
    {
        string Apply(string src);
    }

    internal sealed class CaseTransform : IStringTransform
    {
        private readonly bool _upper;
        public CaseTransform(bool upper) { _upper = upper; }
        public string Apply(string src)
        {
            return _upper ? src.ToUpperInvariant() : src.ToLowerInvariant();
        }
    }

    internal sealed class ReplaceTransform : IStringTransform
    {
        private readonly string _find;
        private readonly string _repl;
        public ReplaceTransform(string find, string repl) { _find = find ?? string.Empty; _repl = repl ?? string.Empty; }

        public string Apply(string src)
        {
            if (_find.Length == 0)
            {
                return src;
            }

            var sb = new StringBuilder(src.Length);
            int idx = 0;
            int found;
            while ((found = src.IndexOf(_find, idx, StringComparison.Ordinal)) >= 0)
            {
                sb.Append(src, idx, found - idx);
                sb.Append(_repl);
                idx = found + _find.Length;
            }
            sb.Append(src, idx, src.Length - idx);
            return sb.ToString();
        }
    }

    internal sealed class InsertTransform : IStringTransform
    {
        private readonly bool _right;
        private readonly int _from;
        private readonly string _text;
        public InsertTransform(bool right, int from, string text)
        {
            _right = right; _from = from; _text = text ?? string.Empty;
        }

        public string Apply(string src)
        {
            int n = src.Length;
            int p = _right ? n - _from + 1 : _from;
            p = NamePattern.Clamp(p, 0, n);
            return src.Substring(0, p) + _text + src.Substring(p);
        }
    }

    internal sealed class DeleteTransform : IStringTransform
    {
        private readonly bool _right;
        private readonly int _from;
        private readonly int _len;
        public DeleteTransform(bool right, int from, int len)
        {
            _right = right; _from = from; _len = len;
        }

        public string Apply(string src)
        {
            int n = src.Length;
            int p = _right ? n - _from + 1 : _from;
            p = NamePattern.Clamp(p, 0, n);
            int count = Math.Min(_len, n - p);
            if (count <= 0)
            {
                return src;
            }
            return src.Substring(0, p) + src.Substring(p + count);
        }
    }

    internal sealed class CutTransform : IStringTransform
    {
        private readonly bool _right;
        private readonly int _from;
        private readonly int _to;
        public CutTransform(bool right, int from, int to)
        {
            _right = right; _from = from; _to = to;
        }

        public string Apply(string src)
        {
            int n = src.Length;
            int start0, end0;
            if (_right)
            {
                start0 = n - _to;
                end0 = n - _from + 1;
            }
            else
            {
                start0 = _from - 1;
                end0 = _to;
            }
            start0 = NamePattern.Clamp(start0, 0, n);
            end0 = NamePattern.Clamp(end0, start0, n);
            return src.Substring(start0, end0 - start0);
        }
    }

    internal sealed class RangeReplaceTransform : IStringTransform
    {
        private readonly bool _right;
        private readonly int _from;
        private readonly int _to;
        private readonly string _text;
        public RangeReplaceTransform(bool right, int from, int to, string text)
        {
            _right = right; _from = from; _to = to; _text = text ?? string.Empty;
        }

        public string Apply(string src)
        {
            int n = src.Length;
            int start0, end0;
            if (_right)
            {
                start0 = n - _to;
                end0 = n - _from + 1;
            }
            else
            {
                start0 = _from - 1;
                end0 = _to;
            }
            start0 = NamePattern.Clamp(start0, 0, n);
            end0 = NamePattern.Clamp(end0, start0, n);
            return src.Substring(0, start0) + _text + src.Substring(end0);
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
