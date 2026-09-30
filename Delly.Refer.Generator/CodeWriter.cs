using System.Text;

namespace Delly.Refer.Generator
{
    /// <summary>
    /// 带缩进的源码文本写入器
    /// </summary>
    internal sealed class CodeWriter
    {
        private const string IndentUnit = "    ";

        private readonly StringBuilder _builder = new StringBuilder();
        private int _indent;

        /// <summary>
        /// 写入一行文本；空串写入空行
        /// </summary>
        /// <param name="text">行内容</param>
        public void Line(string text)
        {
            if (text.Length == 0)
            {
                _builder.Append('\n');
                return;
            }

            for (int i = 0; i < _indent; i++)
            {
                _builder.Append(IndentUnit);
            }

            _builder.Append(text).Append('\n');
        }

        /// <summary>
        /// 写入空行
        /// </summary>
        public void Blank()
        {
            _builder.Append('\n');
        }

        /// <summary>
        /// 写入左花括号并增加缩进
        /// </summary>
        public void OpenBrace()
        {
            Line("{");
            _indent++;
        }

        /// <summary>
        /// 减少缩进并写入右花括号
        /// </summary>
        public void CloseBrace()
        {
            _indent--;
            Line("}");
        }

        /// <summary>
        /// 返回全部已写入文本
        /// </summary>
        /// <returns>源码文本</returns>
        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
