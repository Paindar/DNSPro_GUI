using System.IO;
using System.IO.Compression;
using System.Text;

namespace DNSPro_GUI
{
    class Utils
    {
        public static string UnGzip(byte[] buf)
        {
            byte[] buffer = new byte[1024];
            int n;
            using (MemoryStream sb = new MemoryStream())
            {
                using (GZipStream input = new GZipStream(new MemoryStream(buf),
                                                         CompressionMode.Decompress,
                                                         false))
                {
                    while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sb.Write(buffer, 0, n);
                    }
                }
                return Encoding.UTF8.GetString(sb.ToArray());
            }
        }
    }
}
