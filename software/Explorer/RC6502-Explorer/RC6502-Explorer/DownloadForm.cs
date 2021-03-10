using RC6502_Explorer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RC6502_Explorer
{


    public partial class DownloadForm : Form
    {
        public static event Action<string, DownloadType, int, int> DownloadActionEvent;
        private DownloadType _type;
        private string _extention;
        private int _start;
        private int _end;
        private bool _cancel = false;

        public DownloadForm()
        {
            InitializeComponent();
            filenameTextBox.Text = Settings.Default.DownloadFilename;
            _extention = "txt";
            _start = 0;
            _end = 0;
            _type = DownloadType.Text;
            startTextBox.Text = Settings.Default.StartHEX;
            startTextBox.Text = Settings.Default.EndHEX;

        }
        private bool validateFullPath(string fname)
        {
            string file = fname.Substring(fname.LastIndexOf('\\') + 2);
            if (file.Contains(":")) return false;
            if (file.Length == 0) return false;
            if (file.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) return false;

            string path = Path.GetDirectoryName(fname);
            if (path.Length == 0) return false;
            if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;

            return true;
        }
        private bool handleHexValidation(int code)
        {
            return true;
        }

        private void filenameTextBox_Validating(object sender, CancelEventArgs e)
        {
            if (!validateFullPath(filenameTextBox.Text)) errorLabel.Text = "!!ERROR!! Invalid File Name";
            else errorLabel.Text = "";
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _cancel = true;
            this.Dispose();
        }

        private void typeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (typeComboBox.SelectedIndex)
            {
                case 0:
                    Console.WriteLine(typeComboBox.Text);
                    _extention = "bas";
                    _type = DownloadType.Basic;
                    break;
                case 1:
                    Console.WriteLine(typeComboBox.Text);
                    _extention = "asm";
                    _type = DownloadType.Assembly;
                    break;
                case 2:
                    Console.WriteLine(typeComboBox.Text);
                    _extention = "bin";
                    _type = DownloadType.Binary;
                    break;
                case 3:
                    Console.WriteLine(typeComboBox.Text);
                    _extention = "hex";
                    _type = DownloadType.HexDump;
                    break;
                default:
                    Console.WriteLine(typeComboBox.Text);
                    _extention = "txt";
                    _type = DownloadType.Text;
                    break;
            }
            filenameTextBox.Enabled = true;
            saveDialogButton.Enabled = true;
            startTextBox.Enabled = true;
            endTextBox.Enabled = true;

            saveFileDialog1.FileName = filenameTextBox.Text;
            if (saveFileDialog1.CheckPathExists)
            {
                okButton.Enabled = true;
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (!validateFullPath(filenameTextBox.Text)) return;
            else errorLabel.Text = "";
            if (!filenameTextBox.Text.EndsWith(_extention))
            {
                var name = Path.GetFileNameWithoutExtension(filenameTextBox.Text);
                var path = Path.GetDirectoryName(filenameTextBox.Text);
                filenameTextBox.Text = Path.Combine(path, name + "." + _extention);
            }
            Settings.Default.DownloadFilename = filenameTextBox.Text;
            Settings.Default.Save();
            if (DownloadActionEvent != null) DownloadActionEvent.Invoke(filenameTextBox.Text, _type, 0, 0);
            this.Dispose();
        }

        private void saveDialogButton_Click(object sender, EventArgs e)
        {
            saveFileDialog1.DefaultExt = _extention;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filenameTextBox.Text = saveFileDialog1.FileName;
            }
            if (saveFileDialog1.CheckPathExists)
            {
                okButton.Enabled = true;
            }
            errorLabel.Text = "";
        }

        private void startTextBox_Validated(object sender, EventArgs e)
        {
            Settings.Default.StartHEX = startTextBox.Text;
        }

        private void endTextBox_Validated(object sender, EventArgs e)
        {
            Settings.Default.EndHEX = endTextBox.Text;
        }
    }
    public enum DownloadType
    {
        Basic,
        Assembly,
        Binary,
        HexDump,
        Text
    }
}
