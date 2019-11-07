// Copyright 2019 Paul Young
// See the file LICENSE for license information and restrictions.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SO2RInterface
{
    public partial class Form : System.Windows.Forms.Form
    {
        Device _device = null;
        Otrsp _otrsp = null;
        Keyer _keyer = null;

        readonly Data _data = new Data();

        public Form()
        {
            InitializeComponent();

            // OTRSP and Keyer can be set to "none" if the operator
            // does not want those features
            cOtrsp.Items.Add("");
            cKeyer.Items.Add("");

            // Add some COM ports.  This could use only the ports that exist
            // on the computer but then something would need to be done if the
            // port doesn't currently exist, for example if the SO2R device is
            // not plugged in.
            for (int _i = 1; _i <= 40; _i++)
            {
                string _port = "COM" + _i.ToString();
                cOtrsp.Items.Add(_port);
                cKeyer.Items.Add(_port);
                cSo2rDevice.Items.Add(_port);
            }

            // Get the persistent values.  If there is no persistent value for a
            // ComboBox use the first value in the list.
            cStart.Checked = _data.Start;
            cMinimize.Checked = _data.Minimize;
            cNoStereo.Checked = _data.NoStereo;
            cLatch.Checked = _data.Latch;
            cManual.Checked = _data.Manual;

            UpdateManual();
            UpdateTx();
            UpdateRx();

            rTX1.Enabled = false;
            rTX2.Enabled = false;
            rRX1.Enabled = false;
            rRX2.Enabled = false;
            rStereo.Enabled = false;
            gTX.Enabled = false;
            gRX.Enabled = false;

            int _index = cSo2rDevice.FindStringExact(_data.DevicePort);
            cSo2rDevice.SelectedIndex = Math.Max(_index, 0);

            _index = cOtrsp.FindStringExact(_data.OtrspPort);
            cOtrsp.SelectedIndex = Math.Max(_index, 0);

            _index = cKeyer.FindStringExact(_data.KeyerPort);
            cKeyer.SelectedIndex = Math.Max(_index, 0);

            _data.Latch_Changed += Latch_Changed;
            _data.Devicename_Changed += Devicename_Changed;

            if (_data.Minimize)
            {
                this.WindowState = FormWindowState.Minimized;
            }

            if (_data.Start)
            {
                Start();
            }
        }

        private void BStartStop_Click(object sender, EventArgs e)
        {
            if (bStartStop.Text == "Start")
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        private void CStart_CheckedChanged(object sender, EventArgs e)
        {
            _data.Start = cStart.Checked;
        }

        private void CMinimize_CheckedChanged(object sender, EventArgs e)
        {
            _data.Minimize = cMinimize.Checked;
        }

        private void CNoStereo_CheckedChanged(object sender, EventArgs e)
        {
            _data.NoStereo = cNoStereo.Checked;
        }

        private void CLatch_CheckedChanged(object sender, EventArgs e)
        {
            _data.Latch = cLatch.Checked;
        }

        private void CManual_CheckedChanged(object sender, EventArgs e)
        {
            UpdateManual();
        }

        private void CSo2rDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            _data.DevicePort = cSo2rDevice.Text;
        }

        private void COtrsp_SelectedIndexChanged(object sender, EventArgs e)
        {
            _data.OtrspPort = cOtrsp.Text;
        }

        private void CKeyer_SelectedIndexChanged(object sender, EventArgs e)
        {
            _data.KeyerPort = cKeyer.Text;
        }

        private void RTx_CheckedChanged(object sender, EventArgs e)
        {
            if (rTX1.Checked)
            {
                _data.Tx = Data.TX.TX1;
            }
            else
            {
                _data.Tx = Data.TX.TX2;
            }
        }

        private void RRx_CheckedChanged(object sender, EventArgs e)
        {
            if (rRX1.Checked)
            {
                _data.Rx = Data.RX.RX1;
            }
            else
            {
                if (rRX2.Checked)
                {
                    _data.Rx = Data.RX.RX2;
                }
                else
                {
                    _data.Rx = Data.RX.RX1S;
                }
            }
        }

        void ErrorStop(string s, string port, Exception e)
        {
            string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            MessageBox.Show(s + " Port " + port + Environment.NewLine + errorMessage, e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Stop();
        }

        /// <summary>
        /// Start handling OTRSP and Winkey commands
        /// </summary>
        private void Start()
        {
            bStartStop.Text = "Stop";

            try
            {
                _device = new Device(_data);
                _device.Open();
            }
            catch (Exception e)
            {
                ErrorStop("Device", _data.DevicePort, e);
                return;
            }

            if (!_data.Manual)
            {
                try
                {
                    _otrsp = new Otrsp(_data);
                    _otrsp.Open();
                }
                catch (Exception e)
                {
                    ErrorStop("OTRSP", _data.OtrspPort, e);
                    return;
                }
            }

            try
            {
                _keyer = new Keyer(_data);
                _keyer.Open();
            }
            catch (Exception e)
            {
                ErrorStop("Keyer", _data.KeyerPort, e);
                return;
            }

            cSo2rDevice.Enabled = false;
            cKeyer.Enabled = false;
            cManual.Enabled = false;

            if (_data.Manual)
            {
                rTX1.Enabled = true;
                rTX2.Enabled = true;
                rRX1.Enabled = true;
                rRX2.Enabled = true;
                rStereo.Enabled = true;
                gTX.Enabled = true;
                gRX.Enabled = true;
            }
            else
            {
                cOtrsp.Enabled = false;
                _data.Tx_Changed += UpdateTx;
                _data.Rx_Changed += UpdateRx;
            }
        }

        /// <summary>
        /// Stop handling OTRSP and Winkey commands
        /// </summary>
        private void Stop()
        {
            bStartStop.Text = "Start";

            if (_device != null)
            {
                _device.Close();
                _device = null;
            }

            if (_otrsp != null)
            {
                _otrsp.Close();
                _otrsp = null;
            }

            if (_keyer != null)
            {
                _keyer.Close();
                _keyer = null;
            }

            cSo2rDevice.Enabled = true;
            cKeyer.Enabled = true;
            cManual.Enabled = true;

            if (!_data.Manual)
            {
                cOtrsp.Enabled = true;
                _data.Tx_Changed -= UpdateTx;
                _data.Rx_Changed -= UpdateRx;
            }

            rTX1.Enabled = false;
            rTX2.Enabled = false;
            rRX1.Enabled = false;
            rRX2.Enabled = false;
            rStereo.Enabled = false;
            gTX.Enabled = false;
            gRX.Enabled = false;

            Text = "SO2R Interface";
        }

        ~Form()
        {
            Stop();
        }

        /// <summary>
        /// Update the form for the current manual box check
        /// </summary>
        public void UpdateManual()
        {
            _data.Manual = cManual.Checked;
            cOtrsp.Enabled = !_data.Manual;
        }

        public void UpdateTx()
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
               {
                   UpdateTx();
               });
            }
            else
            {
                if (_data.Tx == Data.TX.TX1)
                {
                    rTX1.Checked = true;
                    rTX2.Checked = false;
                }
                else
                {
                    rTX1.Checked = false;
                    rTX2.Checked = true;
                }
            }
        }

        public void UpdateRx()
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
               {
                   UpdateRx();
               });
            }
            else
            {
                switch ((Data.RX)_data.Rx)
                {
                    case Data.RX.RX1:
                        rRX1.Checked = true;
                        rRX2.Checked = false;
                        rStereo.Checked = false;
                        break;

                    case Data.RX.RX2:
                        rRX1.Checked = false;
                        rRX2.Checked = true;
                        rStereo.Checked = false;
                        break;

                    case Data.RX.RX1S:
                    case Data.RX.RX1R:
                    case Data.RX.RX2S:
                    case Data.RX.RX2R:
                        rRX1.Checked = false;
                        rRX2.Checked = false;
                        rStereo.Checked = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Called when the latch mode changes
        /// </summary>
        public void Latch_Changed()
        {
            if (cLatch.Checked != _data.Latch)
            {
                _data.Latch = cLatch.Checked;
            }

        }

        public void Devicename_Changed()
        {
            Invoke((MethodInvoker) delegate
            {
                Text = _data.Devicename;
            });
        }
    }

}