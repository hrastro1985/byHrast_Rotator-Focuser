using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ASCOM.byHrast.Rotator
{
    [ComVisible(false)] // Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        TraceLogger tl; // Holder for a reference to the driver's trace logger
        bool rev;
        bool hold;

        public SetupDialogForm(TraceLogger tlDriver)
        {            
            InitializeComponent();

            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;
            rev = Rotator._Rev;
            hold = Rotator._Hold;

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();


        }

        private void CmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here and update the state variables with results from the dialogue

            tl.Enabled = chkTrace.Checked;
            Rotator._Rev = Rev.Checked;
            Rotator._Hold = Hold.Checked;

            // Update the COM port variable if one has been selected
            if (comboBoxComPort.SelectedItem is null) // No COM port selected
            {
                tl.LogMessage("Setup OK", $"New configuration values - Trace: {chkTrace.Checked}, COM Port: Not selected");
            }
            else // A COM port has been selected
            {
                Rotator.SerialPortName = (string)comboBoxComPort.SelectedItem;
                tl.LogMessage("Setup OK", $"New configuration values - Trace: {chkTrace.Checked}, COM Port: {comboBoxComPort.SelectedItem}");
            }

        }

        private void CmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("https://ascom-standards.org/");
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {
            
            // Set the trace checkbox
            chkTrace.Checked = tl.Enabled;
            Rev.Checked = rev;
            Hold.Checked = hold;

            // set the list of COM ports to those that are currently available
            comboBoxComPort.Items.Clear();
            comboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());

			// select the current port if possible
			if (comboBoxComPort.Items.Contains( Rotator.SerialPortName ))
			{
				comboBoxComPort.SelectedItem = Rotator.SerialPortName;
			}

            tl.LogMessage("InitUI", $"Set UI controls to Trace: {chkTrace.Checked}, COM Port: {comboBoxComPort.SelectedItem}");


            if (Hold.Checked)
            {
                Hold.Text = "Hold on";
            }
            else
            {
                Hold.Text = "Hold off";
            }
            if (Rev.Checked)
            {
                Rev.Text = "Reverse on";
            }
            else
            {
                Rev.Text = "Reverse off";
            }
            if (chkTrace.Checked)
            {
                chkTrace.Text = "Trace on";
            }
            else
            {
                chkTrace.Text = "Trace off";
            }

        }

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {
            // Bring the setup dialogue to the front of the screen
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            else
            {
                TopMost = true;
                Focus();
                BringToFront();
                TopMost = false;
            }
        }

        private void chkTrace_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTrace.Checked)
            {
                chkTrace.Text = "Trace on";
            }
            else
            {
                chkTrace.Text = "Trace off";
            }
        }

        private void Rev_CheckedChanged(object sender, EventArgs e)
        {
            if (Rev.Checked)
            {
                Rev.Text = "Reverse on";
            }
            else
            {
                Rev.Text = "Reverse off";
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Hold_CheckedChanged(object sender, EventArgs e)
        {
            if (Hold.Checked)
            {
                Hold.Text = "Hold on";
            }
            else
            {
                Hold.Text = "Hold off";
            }
        }
    }
}