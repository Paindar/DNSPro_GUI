using System;
using System.Windows.Forms;

namespace DNSPro_GUI
{
    public partial class ToolBarForm : Form
    {
        LogForm mainForm;
        public ToolBarForm(LogForm form)
        {
            InitializeComponent();
            mainForm = form;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!mainForm.Visible)
                mainForm.Show();
            else
                mainForm.Hide();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if(!mainForm.Visible)
            { 
                mainForm.Show();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if(mainForm.Visible)
            { 
                mainForm.Hide();
            }
        }
    }
}
