using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Popgun
{
    public partial class EnterIPForm : Form
    {
        private IPAddress IP;
        public IPAddress GetIP()
        {
            return IP;
        }

        public EnterIPForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() == "")
            {
                MessageBox.Show("Enter a valid IP");
                return;
            }

            if (!IPAddress.TryParse(textBox1.Text, out IP))
            {
                MessageBox.Show("Enter a valid IP");
                return;
            }

            Close();
        }
    }
}
