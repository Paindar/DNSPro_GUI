using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DNSPro_GUI
{
    class Logging
    {
        public static string LogFilePath;

        private static FileStream _fs;
        private static StreamWriterWithTimestamp _sw;

        public static bool OpenLogFile()
        {
            try
            {
                LogFilePath = "DnsPro.log";
                if (File.Exists(LogFilePath))
                {
                    using (FileStream originalFileStream = new FileStream(LogFilePath, FileMode.Open))
                    { 
                          using (FileStream compressedFileStream = File.Create(DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + ".gz"))
                           {
                                using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                                   CompressionMode.Compress))
                                {
                                    originalFileStream.CopyTo(compressionStream);
                                }
                            }
                     }
                }
                _fs = new FileStream(LogFilePath, FileMode.Create);
                _sw = new StreamWriterWithTimestamp(_fs);
                _sw.AutoFlush = true;
                Console.SetOut(_sw);
                Console.SetError(_sw);

                return true;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        private static void WriteToLogFile(object o)
        {
            try
            {
                Console.WriteLine(o);
            }
            catch (ObjectDisposedException)
            {
            }
        }
        public static void Warn(object o)
        {
            WriteToLogFile("[W] " + o);
        }

        public static void Error(object o)
        {
            WriteToLogFile("[E] " + o);
        }

        public static void Info(object o)
        {
            WriteToLogFile(o);
        }

        public static void Clear()
        {
            _sw.Close();
            _sw.Dispose();
            _fs.Close();
            _fs.Dispose();
            File.Delete(LogFilePath);
            OpenLogFile();
        }
        public static void LogUsefulException(Exception e)
        {
            // just log useful exceptions, not all of them
            if (e is SocketException)
            {
                SocketException se = (SocketException)e;
                if (se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // closed by browser when sending
                    // normally happens when download is canceled or a tab is closed before page is loaded
                }
                else if (se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // received rst
                }
                else if (se.SocketErrorCode == SocketError.NotConnected)
                {
                    // The application tried to send or receive data, and the System.Net.Sockets.Socket is not connected.
                }
                else if (se.SocketErrorCode == SocketError.HostUnreachable)
                {
                    // There is no network route to the specified host.
                }
                else if (se.SocketErrorCode == SocketError.TimedOut)
                {
                    // The connection attempt timed out, or the connected host has failed to respond.
                }
                else
                {
                    Info(e);
                }
            }
            else if (e is ObjectDisposedException)
            {
            }
            else if (e is Win32Exception)
            {
                var ex = (Win32Exception)e;

                // Win32Exception (0x80004005): A 32 bit processes cannot access modules of a 64 bit process.
                if ((uint)ex.ErrorCode != 0x80004005)
                {
                    Info(e);
                }
            }
            else
            {
                Info(e);
            }
        }

    }

    public class StreamWriterWithTimestamp : StreamWriter
    {
        public StreamWriterWithTimestamp(Stream stream) : base(stream)
        {
        }

        private string GetTimestamp()
        {
            return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(GetTimestamp() + value);
        }

        public override void Write(string value)
        {
            base.Write(GetTimestamp() + value);
        }
    }
}
