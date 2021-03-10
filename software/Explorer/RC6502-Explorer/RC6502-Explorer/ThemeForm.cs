using RC6502_Explorer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RC6502_Explorer
{
    public partial class ThemeForm : Form
    {
        public static event Action UpdateAppearance;
        public ThemeForm()
        {
            InitializeComponent();
            updateLabels();
        }
        private void updateLabels() {

            label1.BackColor = Settings.Default.BackgroundColour;
            label2.BackColor = Settings.Default.ForegroundColour;
            label3.Text = Settings.Default.Font.Name;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.BackgroundColour = colorDialog1.Color;
                updateLabels();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.Font = fontDialog1.Font;
                updateLabels();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.ForegroundColour = colorDialog1.Color;
                updateLabels();               
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (UpdateAppearance != null) UpdateAppearance.Invoke();
            this.Dispose();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Settings.Default.Font = SystemFonts.DefaultFont;
            Settings.Default.BackgroundColour = SystemColors.Window;
            Settings.Default.ForegroundColour = SystemColors.WindowText;
            updateLabels();
        }
    }
}
