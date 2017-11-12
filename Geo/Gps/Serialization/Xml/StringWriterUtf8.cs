using System.IO;
using System.Text;

namespace Geo.Gps.Serialization.Xml
{
    /// <summary>
    /// String writer encoding utf8
    /// </summary>
    public class StringWriterUtf8 : StringWriter
    {
        /// <summary>
        /// UTF8 encoding
        /// </summary>
        public override Encoding Encoding => Encoding.UTF8;
    }
}
