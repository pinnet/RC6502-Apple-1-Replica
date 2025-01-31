﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RC6502_Explorer.Properties;

namespace RC6502_Explorer
{
    public partial class MainForm : Form
    {
     
        public static event Action<string> portStatusChangeEvent;
        
        private Thread _backgroundWorkerThread;
        private string _selectedPort = "COM1";
        private SerialPort _comPort;
        private bool _portConnected = false;
        private bool _downloading = false;
        private string _filename =  "";
        private uint _startAddress = 0;
        private uint _endAddress = 0;
        private string _headder;
        private uint _totalBytes = 0;
        private uint _inBytes = 0;


        public MainForm()
        {
            InitializeComponent();
            ThemeForm_UpdateAppearance();

            ThemeForm.UpdateAppearance += ThemeForm_UpdateAppearance;
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
            DownloadForm.DownloadActionEvent += DownloadForm_DownloadActionEvent;

            richTextBox1.Font = Settings.Default.Font;
            _selectedPort = Settings.Default.LastPort;
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
               cOMPortToolStripMenuItem.DropDownItems.Add(port);
            }
            toolStripStatusLabel1.Text = _selectedPort + ":";
            
            portStatusChangeEvent += MainForm_portStatusChangeEvent;
            if (Settings.Default.AutoConnect)
            {
                autoConnectToolStripMenuItem.Checked = true;
                portChange(_selectedPort);
            }
            else
            {
                autoConnectToolStripMenuItem.Checked = false;
            }
            
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int pcComplete = 0;
            BackgroundWorker worker = sender as BackgroundWorker;
            try
            {
                _backgroundWorkerThread = Thread.CurrentThread;

                do
                {
                    pcComplete = (int)Math.Floor(((double)_inBytes / (double)_totalBytes) * 100);
                    if (pcComplete > 100) pcComplete = 100;
                    worker.ReportProgress(pcComplete);
                    Thread.Sleep(200);
                } while (pcComplete != 100);

            }
            catch (ThreadAbortException)
            {
                // Do your clean up here.
            }
            endSave();
            worker.ReportProgress(0);
        }

        private void ThemeForm_UpdateAppearance()
        {
            richTextBox1.BackColor = Settings.Default.BackgroundColour;
            richTextBox1.ForeColor = Settings.Default.ForegroundColour;
            richTextBox1.SelectAll();
            richTextBox1.Font = Settings.Default.Font;
            richTextBox1.DeselectAll();
        }

        private void DownloadForm_DownloadActionEvent(string file, DownloadType type, uint start, uint end)
        {
            toolStripStatusLabel4.Text = "Saving " + file;
           
            _filename = file;
            _downloading = true;
            _startAddress = start;
            _endAddress = end;
            switch (type)
            {
                case DownloadType.Basic:
                    _headder = "LIST";
                    slowWriteToPort(_headder);
                    slowWriteToPort("\n");
                    _totalBytes = 999999;
                    break;
                case DownloadType.Assembly:
                    _headder = "L\n";
                    slowWriteToPort(_headder);
                    _totalBytes = 999999;
                    break;
                case DownloadType.HexDump:
                    _headder = start.ToString("X4") + "." + end.ToString("X4");
                    slowWriteToPort(_headder);
                    slowWriteToPort("\n");
                    double totalAddresses = (double)_endAddress - (double)_startAddress;
                    uint noLines = (uint)Math.Ceiling(totalAddresses / 8);
                    _totalBytes = noLines * 8;
                    _totalBytes += (uint)totalAddresses * 3; 
                    break;
                default:
                    break;
            }


            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }
        private void endSave()
        {
            toolStripStatusLabel4.Text = "";
            _downloading = false;
            _inBytes = 0;
        }
        private void saveText(string txt)
        {
            if (txt.Contains(_headder)) txt = txt.Replace(_headder, "");
            _inBytes += (uint)txt.Length; 
            var handle =  File.AppendText(_filename);
            handle.Write(txt);
            handle.Close();

            if (txt.Contains("?") || txt.Contains(">")) _inBytes = _totalBytes;
         
        }
        private void MainForm_portStatusChangeEvent(string obj)
        {
            toolStripStatusLabel2.Text = obj;
        }

        private void portChange(string obj)
        {
            _selectedPort = obj;
            richTextBox1.Clear();
            toolStripStatusLabel1.Text = obj + ":";
            if (connect(obj))
            { 
                Settings.Default.LastPort = obj; 
            } 
            else 
            { 
                toolStripStatusLabel2.Text = "ERROR"; 
            }
        }

        private void disconnect()
        {
            if (_portConnected)
            {
                _comPort.DataReceived -= _comPort_DataReceived;
                _comPort.Dispose();
                _portConnected = false;
            }
            if (portStatusChangeEvent != null) portStatusChangeEvent.Invoke("Disconnected");
        }
        private bool connect(string port)
        {
            if (_portConnected == true) disconnect();
            try
            {
                _comPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                _comPort.DtrEnable = true;
                _comPort.DataReceived += _comPort_DataReceived;
                _comPort.Open();
                _portConnected = true;
                if (portStatusChangeEvent != null) portStatusChangeEvent.Invoke("Connected");
            }
            catch(Exception e)
            {
                return false;
            }
            return true;
        }
        private void updateText(string txt) 
        {

            richTextBox1.AppendText(txt.Replace('\r', ' '));
            if (_downloading)
            {
                saveText(txt.Replace('\r', ' '));
            }  
        }
        private void _comPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            richTextBox1.Invoke((Action)delegate { updateText(_comPort.ReadExisting()); } );
        }
        
        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
        
        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_downloading) return;
            DownloadForm d = new DownloadForm();
            d.ShowDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cOMPortToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            portChange(e.ClickedItem.Text);
        }

        private void cOMPortToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem i = (ToolStripMenuItem)sender;
            foreach(ToolStripMenuItem mi in i.DropDownItems)
            {
                mi.Checked = false;
                if (mi.Text == _selectedPort) mi.Checked = true;
            }
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            if (_portConnected) { disconnect(); }
            else { connect(_selectedPort); }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        { 
            ThemeForm.UpdateAppearance -= ThemeForm_UpdateAppearance;
            backgroundWorker1.DoWork -= BackgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged -= BackgroundWorker1_ProgressChanged;
            DownloadForm.DownloadActionEvent -= DownloadForm_DownloadActionEvent;
            portStatusChangeEvent -= MainForm_portStatusChangeEvent;
            backgroundWorker1.CancelAsync();
            Settings.Default.Save();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBox1.SelectedText);
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        { 
            slowWriteToPort(Clipboard.GetText());
        }
        private void slowWriteToPort(string text) 
        {
            char[] data = text.ToCharArray();
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '\n')
                {
                    _comPort.WriteLine('\r'.ToString());
                    Thread.Sleep(100);
                }
                else
                {
                    _comPort.Write(data[i].ToString());
                    Thread.Sleep(60);
                }
            }
        }
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            portChange(_selectedPort);
        }

        private void startIntigerBasicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            slowWriteToPort("E000R\n");
        }

        private void autoConnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AutoConnect = !Settings.Default.AutoConnect;
            if (Settings.Default.AutoConnect)
            {
                autoConnectToolStripMenuItem.Checked = true;
                portChange(_selectedPort);
            }
            else
            {
                autoConnectToolStripMenuItem.Checked = false;
                disconnect();
            }
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ThemeForm tf = new ThemeForm();
            tf.ShowDialog();
        }

        private void startKrusaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            slowWriteToPort("F000R\n");
        }
        
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About a = new About();
            a.ShowDialog();
        }

        private void richTextBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            KeysConverter kc = new KeysConverter();
            switch (e.KeyData)
            {
                case Keys.Tab:
                    _comPort.Write(' '.ToString());
                    break;
                default:       
                    break;
            }
        }

        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!_downloading)
            {
                switch (e.KeyChar)
                {
                    default:
                        slowWriteToPort(e.KeyChar.ToString());
                        break;
                }
            }
            else
            {
                _inBytes = _totalBytes;
            }
            e.Handled = true;
        }
    }
}
