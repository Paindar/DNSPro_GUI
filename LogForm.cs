using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DNSPro_GUI
{
    public partial class LogForm : Form
    {
        private static readonly Object log_Object = new object();
        private Queue queue = new Queue();
        private System.Windows.Forms.Timer
            timer = new System.Windows.Forms.Timer
            {
                Interval = 100
            };
        long lastOffset;
        string filename= Logging.LogFilePath;
        const int BACK_OFFSET = 65536;
        //private event EventHandler NewLogMessageEvent;
        public LogForm()
        {
            InitializeComponent();
            timer.Tick += UpdateContent;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;           //取消关闭操作 表现为不关闭窗体  
                this.Hide();               //隐藏窗体  
            }
        }
        public new virtual void Hide()
        {
            timer.Stop();
            base.Hide();
        }
        public new virtual void Show()
        {
            timer.Start();
            base.Show();
        }
        private void UpdateContent(object sender, EventArgs e)
        {
            try
            {
                using (StreamReader reader = new StreamReader(new FileStream(filename,
                         FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    reader.BaseStream.Seek(lastOffset, SeekOrigin.Begin);

                    string line = "";
                    bool changed = false;
                    StringBuilder appendText = new StringBuilder(128);
                    while ((line = reader.ReadLine()) != null)
                    {
                        changed = true;
                        appendText.Append(line + Environment.NewLine);
                    }

                    if (changed)
                    {
                        textBox1.AppendText(appendText.ToString());
                        textBox1.ScrollToCaret();
                    }

                    lastOffset = reader.BaseStream.Position;
                }
            }
            catch (FileNotFoundException)
            {
            }
            
        }
        private void LogForm_Load(object sender, EventArgs e)
        {
        }
    }
}
