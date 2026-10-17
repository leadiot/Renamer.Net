using System;
using System.Collections.Generic;

namespace Com.Scm.Renamer
{
    /// <summary>
    /// 托管版自然排序比较器：将字符串拆分为数字段与非数字段，数字段按数值比较
    /// （"2" 排在 "10" 前），行为对齐 Windows 资源管理器；非数字段按字母序（不区分大小写）。
    /// 数值相等时前导零更多者排后（"01" 排在 "1" 后）。
    /// </summary>
    internal sealed class NaturalNameComparer : IComparer<string>
    {
        public static readonly NaturalNameComparer Instance = new NaturalNameComparer();

        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }

            int ix = 0, iy = 0;
            while (ix < x.Length && iy < y.Length)
            {
                char cx = x[ix], cy = y[iy];
                if (char.IsDigit(cx) && char.IsDigit(cy))
                {
                    int sx = ix, sy = iy;
                    while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                    while (iy < y.Length && char.IsDigit(y[iy])) iy++;

                    // 去除前导零后先比位数再比字典序，避免大数值溢出
                    string nx = x.Substring(sx, ix - sx).TrimStart('0');
                    string ny = y.Substring(sy, iy - sy).TrimStart('0');
                    if (nx.Length != ny.Length)
                    {
                        return nx.Length - ny.Length;
                    }
                    int c = string.CompareOrdinal(nx, ny);
                    if (c != 0)
                    {
                        return c;
                    }

                    // 数值相等时前导零更多者排后
                    int zx = (ix - sx) - nx.Length;
                    int zy = (iy - sy) - ny.Length;
                    if (zx != zy)
                    {
                        return zx - zy;
                    }
                }
                else
                {
                    int c = char.ToUpperInvariant(cx).CompareTo(char.ToUpperInvariant(cy));
                    if (c != 0)
                    {
                        return c;
                    }
                    ix++;
                    iy++;
                }
            }
            return (x.Length - ix) - (y.Length - iy);
        }
    }
}
