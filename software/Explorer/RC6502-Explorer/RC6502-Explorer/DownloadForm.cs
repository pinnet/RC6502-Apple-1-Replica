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
        public static event Action<string, DownloadType, uint, uint> DownloadActionEvent;
        private DownloadType _type;
        private string _extention;
  
        public DownloadForm()
        {
            InitializeComponent();
            filenameTextBox.Text = Settings.Default.DownloadFilename;
            _extention = "txt";
            _type = DownloadType.Text;
            startTextBox.Text = Settings.Default.StartHEX;
            endTextBox.Text = Settings.Default.EndHEX;
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
        private bool handleHexValidation(char key)
        {
            int k = (int)Encoding.Default.GetBytes(key.ToString())[0];
            Console.WriteLine(k);
            if (k >= 97) k -= 32;
            if (k >= 48 && k <= 57 || k >= 65 && k <= 70 || k == 8) return true;
            return false;
        }

        private void filenameTextBox_Validating(object sender, CancelEventArgs e)
        {
            if (!validateFullPath(filenameTextBox.Text)) errorLabel.Text = "!!ERROR!! Invalid File Name";
            else errorLabel.Text = "";
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
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
            
            if (DownloadActionEvent != null) DownloadActionEvent.Invoke(filenameTextBox.Text, _type, getHexValue(startTextBox.Text),getHexValue(endTextBox.Text));
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

        private uint getHexValue(string hex)
        {
            return uint.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier);
        }
        
        private void startTextBox_Validated(object sender, EventArgs e)
        {
            startTextBox.Text = getHexValue(startTextBox.Text).ToString("X4");
            Settings.Default.StartHEX = startTextBox.Text;
        }

        private void endTextBox_Validated(object sender, EventArgs e)
        {
            endTextBox.Text = getHexValue(endTextBox.Text).ToString("X4");
            Settings.Default.EndHEX = endTextBox.Text;
        }

        private void startTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {

            e.Handled = true;
            if (handleHexValidation(e.KeyChar))
            {
                if (e.KeyChar != '\b' && startTextBox.Text.Length < 4) startTextBox.AppendText(e.KeyChar.ToString().ToUpper());
                else if (e.KeyChar == '\b' && startTextBox.Text.Length > 0)
                {
                    startTextBox.Text = startTextBox.Text.Substring(0, startTextBox.Text.Length - 1);
                    startTextBox.SelectionStart = startTextBox.Text.Length;
                    startTextBox.SelectionLength = 0;
                }
            }
        }

        private void endTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {

            e.Handled = true;
            if (handleHexValidation(e.KeyChar))
            {
                if (e.KeyChar != '\b' && endTextBox.Text.Length < 4) endTextBox.AppendText(e.KeyChar.ToString().ToUpper());
                else if (e.KeyChar == '\b' && endTextBox.Text.Length > 0)
                {
                    endTextBox.Text = endTextBox.Text.Substring(0, endTextBox.Text.Length - 1);
                    endTextBox.SelectionStart = endTextBox.Text.Length;
                    endTextBox.SelectionLength = 0;
                }
            }
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
