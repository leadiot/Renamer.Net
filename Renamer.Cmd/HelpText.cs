namespace Com.Scm.Renamer
{
    /// <summary>
    /// 表达式使用说明的输出（Readme.txt 表达式规则的精简版）。
    /// 触发方式：命令行首参数 help / --help / -h / /?，或交互输入 help / ? / ？。
    /// </summary>
    internal static class HelpText
    {
        public static void Print()
        {
            Console.WriteLine();
            Console.WriteLine("表达式使用说明");
            Console.WriteLine("==============================================================");

            Console.WriteLine("一、总体规则");
            Console.WriteLine("  1. 模板由【字面量文本】和【表达式】交替组成，表达式用尖括号 <...> 定界，");
            Console.WriteLine("     不允许嵌套；求值结果按位置与字面量拼接，得到目标文件名（含扩展名）。");
            Console.WriteLine("  2. 模板特殊字符 < > * ? \" : | \\ / 均为 Windows 文件名非法字符，");
            Console.WriteLine("     只可能出现在模板里，不会从真实文件名中混入，词法扫描无歧义。");
            Console.WriteLine();

            Console.WriteLine("二、名称 / 扩展名表达式");
            Console.WriteLine("  <*>                    输出原文件名（最后一个点之前的部分）");
            Console.WriteLine("  <?>                    输出原扩展名（不含点；无扩展名时输出空串）");
            Console.WriteLine("  <*|\\>  <*|/>           文件名转大写 / 转小写；");
            Console.WriteLine("  <?|\\>  <?|/>           扩展名转大写 / 转小写；");
            Console.WriteLine("  管道串联：<*|函数1|函数2|...>，从左到右依次执行：");
            Console.WriteLine("    upper                    转大写");
            Console.WriteLine("    lower                    转小写");
            Console.WriteLine("    rep:查找文本:替换文本     全部替换（区分大小写；替换文本可为空）");
            Console.WriteLine("    cut:L:i:j                左起截取第 i~j 个字符，含端点（cut:R 为右起）");
            Console.WriteLine("    ins:L:i:文本             左起第 i 个字符之后插入文本（ins:R 为右起）");
            Console.WriteLine("    del:L:i:长度             左起第 i 个字符之后向右删除若干字符（del:R 为右起）");
            Console.WriteLine("  示例：<*|lower|rep:abc:XYZ|cut:L:1:4>");
            Console.WriteLine("        Abcabcdef.jpg -> XYZX.jpg");
            Console.WriteLine();

            Console.WriteLine("三、数字序列  <|*[位宽][:起始值[:步增量]]>");
            Console.WriteLine("  <|*>                   1,2,3,...（不补零）");
            Console.WriteLine("  <|*4>                  0001,0002,...（最小 4 位，不足补 0，超出不截断）");
            Console.WriteLine("  <|*4:10:2>             起始 10、步长 2 -> 0010,0012,0014,...");
            Console.WriteLine("  序列值 = 起始值 + 步增量 × 文件序号（按文件列表顺序发号）");
            Console.WriteLine();

            Console.WriteLine("四、字母 / 自定义字符序列（字符集必须显式指定）");
            Console.WriteLine("  <|?/[位宽][:起始[:步增量]]>     小写字母（a-z，不足左侧补 a）");
            Console.WriteLine("  <|?\\[位宽][:起始[:步增量]]>     大写字母（A-Z，不足左侧补 A）");
            Console.WriteLine("  <|?字符集[:[位宽]:起始:步增量]   自定义字符集，如 <|?01:2> -> 00,01,10,11");
            Console.WriteLine("  双射进制计数：z 的下一个是 ba，与 Excel 列名同理");
            Console.WriteLine();

            Console.WriteLine("五、时间信息  <|\"[内容]>");
            Console.WriteLine("  <|\">                   当前时间，缺省格式 yyyyMMddHHmmss");
            Console.WriteLine("  <|\"c>  <|\"m>  <|\"a>    文件创建 / 修改 / 访问时间，缺省格式");
            Console.WriteLine("  <|\":格式>              当前时间 + 自定义格式，如 <|\":yyyy-MM-dd>");
            Console.WriteLine("  <|\"c:格式>             指定来源 + 自定义格式，如 <|\"c:yyyyMMdd_HHmmss>");
            Console.WriteLine();

            Console.WriteLine("六、条件分隔符");
            Console.WriteLine("  <.>                    原文件有扩展名时输出点，否则输出空串");
            Console.WriteLine("  典型用法：<*><.><?>（无扩展名文件不产生尾随点）");
            Console.WriteLine();

            Console.WriteLine("七、冲突策略");
            Console.WriteLine("  fail                   发现重名/占用即中止（默认）");
            Console.WriteLine("  auto                   自动追加\" (n)\"后缀消解，如 FIXED (1).txt");
            Console.WriteLine();

            Console.WriteLine("提示：完整规则与 EBNF 文法见 Renamer.Net\\Readme.txt。");
            Console.WriteLine();
        }
    }
}
