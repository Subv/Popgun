using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Popgun
{
    public partial class HighScoreSaveForm : Form
    {
        private int Score = 0;
        private int Mode = 0;

        public HighScoreSaveForm(int score, int mode)
        {
            Score = score;
            Mode = mode;
            InitializeComponent();
            label2.Text = Score.ToString();
            label4.Text = Mode.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() == String.Empty)
            {
                MessageBox.Show("Enter your name!");
                return;
            }

            using (FileStream file = File.Open(Menus.HighScoresMenu.HighScoresFile, FileMode.Append))
                using (StreamWriter writer = new StreamWriter(file))
                    writer.WriteLine(textBox1.Text + "@@" + Score.ToString() + "@@" + Mode.ToString() + ";;");

            Close();
        }
    }
}
