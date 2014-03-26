﻿#region

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

#endregion

namespace SS.Ynote.Classic.UI
{
    public partial class About : GradientForm
    {
        public About()
        {
            InitializeComponent();
            LostFocus += (sender, args) => Close();
            string licensedir = Application.StartupPath + @"\License.txt";
            if (File.Exists(licensedir))
                textBox1.Text = File.ReadAllText(licensedir);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://ynoteclassic.codeplex.com");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://fb.com/sscorpscom");
        }
    }
}