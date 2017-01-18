using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using RDEditor;

namespace RDEditor
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }


        // ********************************************************************** LOAD SETTINGS
        private void Settings_Load(object sender, EventArgs e)
        {
            if (RDEditor.Properties.Settings.Default.AutoOpenLastFile == "ON")
            {
                checkBox1.Checked = true;
            }
            if (RDEditor.Properties.Settings.Default.AutoIndention == "ON")
            {
                checkBox2.Checked = true;
            }
            if (RDEditor.Properties.Settings.Default.WordWrap == "ON")
            {
                checkBox4.Checked = true;
            }
            if (RDEditor.Properties.Settings.Default.AutoTextSize == "ON")
            {
                checkBox3.Checked = true;
            }
            if (RDEditor.Properties.Settings.Default.AutoSave == "ON")
            {
                checkBox8.Checked = true;
            }

            textBox2.Text = RDEditor.Properties.Settings.Default.VisualStudioCompiler;
            textBox3.Text = RDEditor.Properties.Settings.Default.EclipseCompiler;
            textBox5.Text = RDEditor.Properties.Settings.Default.CCompiler;
        }

        // ********************************************************************** SETTINGS
        // DEFAULT OPEN LAST FILE
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                RDEditor.Properties.Settings.Default.AutoOpenLastFile = "ON";
                RDEditor.Properties.Settings.Default.Save();
            }
            else
            {
                RDEditor.Properties.Settings.Default.AutoOpenLastFile = "OFF";
                RDEditor.Properties.Settings.Default.Save();
            }
        }
            
        // AUTO INDENTION
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
            {
                RDEditor.Properties.Settings.Default.AutoIndention = "ON";
                RDEditor.Properties.Settings.Default.Save();
            }
            else
            {
                RDEditor.Properties.Settings.Default.AutoIndention = "OFF";
                RDEditor.Properties.Settings.Default.Save();
            }
        }

        // WORD WRAP
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked == true)
            {
                RDEditor.Properties.Settings.Default.WordWrap = "ON";
                RDEditor.Properties.Settings.Default.Save();
            }
            else
            {
                RDEditor.Properties.Settings.Default.WordWrap = "OFF";
                RDEditor.Properties.Settings.Default.Save();
            }
        }

        // ********************************************************************** COMPILER SETTINGS
        // SET VISUAL STUDIO COMPILER
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.Title = "Select Visual Studio compiler..";
            openfile.Filter = "Compilers (*.exe)|*.exe";
            if (openfile.ShowDialog() == DialogResult.OK)
            {
                textBox2.Clear();
                textBox2.Text = openfile.FileName;
                RDEditor.Properties.Settings.Default.VisualStudioCompiler = openfile.FileName;
                RDEditor.Properties.Settings.Default.Save();
            }
        }

        // SET ECLIPSE COMPILER
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.Title = "Select java compiler..";
            openfile.Filter = "Compilers (javac.exe)|javac.exe";
            if (openfile.ShowDialog() == DialogResult.OK)
            {
                textBox3.Clear();
                textBox3.Text = openfile.FileName;
                RDEditor.Properties.Settings.Default.EclipseCompiler = openfile.FileName;
                RDEditor.Properties.Settings.Default.Save();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.Title = "Select C compiler..";
            openfile.Filter = "C compiler (*.exe)|*.exe";
            if (openfile.ShowDialog() == DialogResult.OK)
            {
                textBox5.Clear();
                textBox5.Text = openfile.FileName;
                RDEditor.Properties.Settings.Default.CCompiler = openfile.FileName;
                RDEditor.Properties.Settings.Default.Save();
            }
        }

        // EXECUTE AFTER COMPILE
        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox7.Checked == true)
            {
                RDEditor.Properties.Settings.Default.ExecuteAfterCompile = "ON";
                RDEditor.Properties.Settings.Default.Save();
            }
            else
            {
                RDEditor.Properties.Settings.Default.ExecuteAfterCompile = "OFF";
                RDEditor.Properties.Settings.Default.Save();
            }
        }

        // SAVE TEXT SIZE
        private void checkBox3_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox3.Checked == true)
            {
                RDEditor.Properties.Settings.Default.AutoTextSize = "ON";
                RDEditor.Properties.Settings.Default.Save();
            }
            else
            {
                RDEditor.Properties.Settings.Default.AutoTextSize = "OFF";
                RDEditor.Properties.Settings.Default.Save();
            }
        }

        // AUTO SAVE
        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked == true)
            {
                RDEditor.Properties.Settings.Default.AutoSave = "ON";
                RDEditor.Properties.Settings.Default.Save();
            }
            else
            {
                RDEditor.Properties.Settings.Default.AutoSave = "OFF";
                RDEditor.Properties.Settings.Default.Save();
            }
        }
    }
}
