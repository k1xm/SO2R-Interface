using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SO2RInterface
{
    class Data
    {
        // Persistent booleans used in the UI
        private bool _start;
        private bool _minimize;
        private bool _noStereo;
        private bool _latch;
        private bool _manual;

        /// <summary>
        /// True if this program should start running immediately
        /// </summary>
        public bool Start
        {
            get
            {
                return _start;
            }
            set
            {
                _start = value;
                Properties.Settings.Default.Start = _start;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// True if the form should start minimized
        /// </summary>
        public bool Minimize
        {
            get
            {
                return _minimize;
            }
            set
            {
                _minimize = value;
                Properties.Settings.Default.Minimize = _minimize;
                Properties.Settings.Default.Save();
            }
        }

        public bool NoStereo
        {
            get
            {
                return _noStereo;
            }
            set
            {
                _noStereo = value;
                Properties.Settings.Default.NoStereo = _noStereo;
                Properties.Settings.Default.Save();
                ComputeRX();
            }
        }

        public bool Latch
        {
            get
            {
                return _latch;
            }
            set
            {
                _latch = value;
                Properties.Settings.Default.Latch = _latch;
                Properties.Settings.Default.Save();
                Latch_Changed?.Invoke();
            }
        }

        public bool Manual
        {
            get
            {
                return _manual;
            }
            set
            {
                _manual = value;
                Properties.Settings.Default.Manual = _manual;
                Properties.Settings.Default.Save();
            }
        }

        private string _devicePort;
        private string _otrspPort;
        private string _keyerPort;

        /// <summary>
        /// Port name of the SO2R device
        /// </summary>
        public string DevicePort {
        get
            {
                return _devicePort;
            }
            set
            {
                _devicePort = value;
                Properties.Settings.Default.Device = _devicePort;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Port name of the OTRSP virtual port or pipe
        /// </summary>
        public string OtrspPort
        {
            get
            {
                return _otrspPort;
            }
            set
            {
                _otrspPort = value;
                Properties.Settings.Default.Otrsp = _otrspPort;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Port name of the keyer virtual port or pipe
        /// </summary>
        public string KeyerPort
        {
            get
            {
                return _keyerPort;
            }
            set
            {
                _keyerPort = value;
                Properties.Settings.Default.Keyer = _keyerPort;
                Properties.Settings.Default.Save();
            }
        }

        // Current RX and TX radios and states  Various things need to be
        // notified when they change.
        public enum RX
        {
            RX1,
            RX2,
            RX1S,
            RX2S,
            RX1R,
            RX2R
        }

        public enum TX
        {
            TX1,
            TX2
        }

        RX _rx;             // Current receiver
        RX _rxRequested;    // RX as sent from computer
        TX _tx;             // Current transmitter
        bool _ptt;          // OTRSP PTT
        string _devicename; // Device name (SO2R Mini, Midi, Maxi etc)
        int _aux1;         // Aux 1 (Radio 1 antenna)
        int _aux2;         // Aux 2 (Radio 2 antenna)

        public Action Rx_Changed;
        public Action Tx_Changed;
        public Action Ptt_Changed;
        public Action Latch_Changed;
        public Action Devicename_Changed;
        public Action Aux1_Changed;
        public Action Aux2_Changed;
        public Action<byte> KeyerRxChar;
        public Action<byte> KeyerTxChar;

        public RX Rx
        {
            get
            {
                return _rx;
            }
            set
            {
                _rxRequested = value;
                Properties.Settings.Default.RxRadio = (int)_rxRequested;
                Properties.Settings.Default.Save();
                ComputeRX();
            }
        }

        public TX Tx
        {
            get
            {
                return _tx;
            }
            set
            {
                _tx = value;
                Properties.Settings.Default.TxRadio = (int)_tx;
                Properties.Settings.Default.Save();
                Tx_Changed?.Invoke();
            }
        }

        public bool Ptt
        {
            get
            {
                return _ptt;
            }
            set
            {
                _ptt = value;
                Ptt_Changed?.Invoke();
            }
        }

        public string Devicename
        {
            get
            {
                return _devicename;
            }
            set
            {
                _devicename = value;
                Devicename_Changed?.Invoke();
            }
        }

        public int Aux1
        {
            get
            {
                return _aux1;
            }
            set
            {
                _aux1 = value & 0xf;
                Aux1_Changed?.Invoke();
            }
        }

        public int Aux2
        {
            get
            {
                return _aux2;
            }
            set
            {
                _aux2 = value & 0xf;
                Aux2_Changed?.Invoke();
            }
        }

        public string Version
        {
            get
            {
                Version v = new Version(System.Windows.Forms.Application.ProductVersion);
                int _major = v.Major;
                int _minor = Math.Max(v.Minor, 0);
                int _rev = Math.Max(v.Revision, 0);
                return String.Format("{0}.{1}.{1}", _major, _minor, _rev);
            }
        }

        public byte Capabilities = 0;

        public Data()
        {
            _start = Properties.Settings.Default.Start;
            _minimize = Properties.Settings.Default.Minimize;
            _noStereo = Properties.Settings.Default.NoStereo;
            _latch = Properties.Settings.Default.Latch;
            _manual = Properties.Settings.Default.Manual;

            _devicePort = Properties.Settings.Default.Device;
            _otrspPort = Properties.Settings.Default.Otrsp;
            _keyerPort  = Properties.Settings.Default.Keyer;

            _rx = (RX)Properties.Settings.Default.RxRadio;
            _tx = (TX)Properties.Settings.Default.TxRadio;

            _ptt = false;
        }

        /// <summary>
        /// Figure out actual RX based on RX requested and whether stereo is allowed
        /// </summary>
        private void ComputeRX()
        {
            RX _r = _rxRequested;
            RX _rxOld = _rx;

            if (_noStereo)
            {
                if ((_r == RX.RX1S) || (_r == RX.RX1R))
                {
                    _r = RX.RX1;
                }

                if ((_r == RX.RX2S) || (_r == RX.RX2R))
                {
                    _r = RX.RX2;
                }
            }

            if ((_rxOld != _r) && (Rx_Changed != null))
            {
                _rx = _r;
                Rx_Changed();
            }
        }
    }
}
