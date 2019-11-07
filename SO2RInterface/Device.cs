// Copyright 2019 Paul Young
// See the file LICENSE for license information and restrictions.

using JH.CommBase;
using System.Diagnostics;

namespace SO2RInterface
{
    class Device : CommBase
    {
        /// <summary>
        /// SO2R protocol commands
        /// </summary>
        private enum Messages : byte
        {
            CLOSE = 0x80,
            OPEN = 0x81,
            PTTOFF = 0x82,
            PTTON = 0x83,
            LATCHOFF = 0x84,
            LATCHON = 0x85,
            TXRX = 0x90,
            AUX1 = 0xA0,
            AUX2 = 0xB0
        }

        /// <summary>
        /// Winkey commands
        /// </summary>
        private enum Winkey
        {
            ADMIN = 0,
            SIDETONE,
            SET_WPM_SPEED,
            SET_WEIGHTING,
            PTT_LEAD_TAIL,
            SET_SPEED_POT,
            PAUSE_STATE,
            GET_SPEED_POT,
            BACKSPACE,
            SET_PINCONFIG,
            CLEAR_BUFFER,
            KEY_IMMEDIATE,
            SET_HSCW,
            SET_FARNS_WPM,
            SET_WINKEYER2_MODE,
            LOAD_DEFAULTS,
            SET_FIRST_EXTENSION,
            SET_KEY_COMP,
            SET_PADDLE_SWITCHPOINT,
            NULL_COMMAND,
            SOFTWARE_PADDLE,
            REQUEST_STATUS,
            POINTER_COMMAND,
            SET_RATIO,
            PTT_ON_OFF,
            KEY_BUFFERED,
            WAIT,
            MERGE_LETTERS,
            BUFFERED_SPEED_CHANGE,
            HSCW_SPEED_CHANGE,
            CANCEL_BUFFERED_SPEED_CHANGE,
            NOP
        }
            
        /// <summary>
        /// Winkey admin commands
        /// </summary>
        private enum Admin
        {
            CALIBRATE = 0,
            RESET,
            HOST_OPEN,
            HOST_CLOSE,
            ECHO_TEST,
            PADDLE_A2D,
            SPEED_A2D,
            GET_VALUES,
            RESERVED08,
            GET_CAL,
            SET_WK1_MODE,
            SET_WK2_MODE,
            DUMP_EEPROM,
            LOAD_EEPROM,
            STANDALONE,
            LOAD_XMODE,
            RESERVED16,
            SET_HIGH_BAUD,
            SET_LOW_BAUD,
            RESERVED19,
            RESERVED20,
            GET_SO2R_INFO = 0xF0,
            NO_OP = 0xFF
        }

        /// <summary>
        /// Device state
        /// </summary>
        private enum State
        {
            UNKNOWN,
            INFO_REQUESTED,
            CLOSED,
            OPEN
        }

        private State _state = State.UNKNOWN;

        private readonly CommBaseSettings _settings = new CommBaseSettings();

        private readonly int[] _deviceVersion = new int[3];
        private readonly int[] _protocolVersion = new int[2];
        private string _deviceName;
        private string _deviceVerString;
        private int _deviceByte = 0;

        private int _wkIgnore = 0;
        private bool _wkAdmin = false;

        private bool _txrxPending = false;
        private bool _latchPending = false;
        private bool _pttPending = false;
        private bool _aux1Pending = false;
        private bool _aux2Pending = false;

        private readonly object LockObject = new object();

        /// <summary>
        /// Pointer to the business logic and data
        /// </summary>
        private readonly Data _data;

        /// <summary>
        /// Get the keyer line settings
        /// </summary>
        /// <returns></returns>
        protected override CommBaseSettings CommSettings()
        {
            return _settings;
        }

        protected override bool AfterOpen()
        {
            Send((byte)Winkey.ADMIN);
            Send((byte)Admin.HOST_CLOSE);
            Send((byte)Winkey.ADMIN);
            Send((byte)Admin.GET_SO2R_INFO);
            _state = State.INFO_REQUESTED;
            _deviceByte = 0;

            _data.Tx_Changed += RxTX_Changed;
            _data.Rx_Changed += RxTX_Changed;
            _data.KeyerRxChar += KeyerRx;
            _data.Ptt_Changed += Ptt_Changed;
            _data.Latch_Changed += Latch_Changed;
            _data.Aux1_Changed += Aux1_Changed;
            _data.Aux2_Changed += Aux2_Changed;
            return true;
        }

        protected override void BeforeClose(bool error)
        {
            Send((byte)Messages.CLOSE);
            Send((byte)Winkey.ADMIN);
            Send((byte)Admin.HOST_CLOSE);

            _data.Tx_Changed -= RxTX_Changed;
            _data.Rx_Changed -= RxTX_Changed;
            _data.KeyerRxChar -= KeyerRx;
            _data.Ptt_Changed -= Ptt_Changed;
            _data.Latch_Changed -= Latch_Changed;
            _data.Aux1_Changed -= Aux1_Changed;
            _data.Aux2_Changed -= Aux2_Changed;
        }

        protected override void OnRxChar(byte ch)
        {
            switch (_state)
            {
                case State.UNKNOWN:

                    break;

                case State.OPEN:
                    // Winkey response - pass it along
                    _data.KeyerTxChar(ch);
                    break;

                case State.INFO_REQUESTED:
                    switch(_deviceByte)
                    {
                        case 0:     // First header byte
                            _deviceByte = (ch == 0xAA) ? 1 : 0;
                            break;

                        case 1:     // Second header byte
                            _deviceByte = (ch == 0x55) ? 2 : 0;
                            break;

                        case 2:     // Third header byte
                            _deviceByte = (ch == 0xCC) ? 3 : 0;
                            break;

                        case 3:     // Fourth header byte
                            _deviceByte = (ch == 0x33) ? 4 : 0;
                            break;

                        case 4:     // SO2R device major version
                            _deviceVersion[0] = ch;
                            _deviceByte = 5;
                            break;

                        case 5:     // SO2R device minor version
                            _deviceVersion[1] = ch;
                            _deviceByte = 6;
                            break;

                        case 6:     // SO2R device patch version
                            _deviceVersion[2] = ch;
                            _deviceByte = 7;
                            break;

                        case 7:     // SO2R protocol major version
                            _protocolVersion[0] = ch;
                            _deviceByte = 8;
                            break;

                        case 8:     // SO2R protocol minor version
                            _protocolVersion[1] = ch;
                            _deviceByte = 9;
                            break;

                        case 9:     // Device capabilities
                            _data.Capabilities = ch;
                            _deviceByte = 10;
                            break;

                        case 10:    // Device name
                            if (ch == 0)
                            {
                                _data.Devicename = _deviceName;
                                _deviceByte = 11;
                            }
                            else
                            {
                                _deviceName += (char)ch;
                            }
                            break;

                        case 11:    // Device version string
                            if (ch == 0)
                            {
                                Send((byte)Messages.OPEN);
                                SendTxRx();
                                SendLatch();
                                SendPtt();
                                SendAux1();
                                SendAux2();
                                _state = State.OPEN;
                                _deviceByte = 0;
                            }
                            else
                            {
                                _deviceVerString += (char)ch;
                            }
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Additional bytes needed for Winkey commands
        /// </summary>
        static readonly int[] CmdLen = { 0, 1, 1, 1, 2, 3, 1, 0, 0, 1, 0, 1, 1, 1, 1, 15, 1, 1, 1, 0, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 0, 0 };

        /// <summary>
        /// Additional bytes needed for Winkey admin commands
        /// </summary>
        static readonly int[] AdminLen = { 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0 };

        /// <summary>
        /// Handle a Winkey command character
        /// </summary>
        /// <param name="ch"></param>
        protected void KeyerRx(byte ch)
        {
            lock (LockObject)
            {
                if (_wkIgnore != 0)
                {
                    Send(ch);
                    Debug.WriteLine(ch.ToString("X"));
                    _wkIgnore--;
                    if (_wkIgnore == 0)
                    {
                        UpdatePending();
                    }
                    return;
                }

                if (_wkAdmin)
                {
                    // Don't send speed changes
                    if ((ch == (byte)Admin.SET_HIGH_BAUD) || (ch == (byte)Admin.SET_LOW_BAUD))
                    {
                        ch = (byte)Admin.NO_OP;
                    }

                    Send(ch);
                    Debug.WriteLine(ch.ToString("X"));

                    if (AdminLen.Length > ch)
                    {
                        _wkIgnore = AdminLen[ch];
                    }

                    _wkAdmin = false;

                    if (_wkIgnore == 0)
                    {
                        UpdatePending();
                    }
                    return;
                }

                if (ch < 32)
                {
                    Send(ch);
                    Debug.WriteLine(ch.ToString("X"));
                    if (ch == 0)
                    {
                        _wkAdmin = true;
                    }
                    else
                    {
                        _wkIgnore = CmdLen[ch];
                    }
                    return;
                }

                Send(ch);
                Debug.WriteLine(ch.ToString("X"));
            }
        }

        /// <summary>
        /// Send the SO2R TX and RX message
        /// </summary>
        private void SendTxRx()
        {
            byte tx;
            byte rx = 0;
            switch (_data.Rx)
            {
                case Data.RX.RX1:
                    rx = 0;
                    break;

                case Data.RX.RX2:
                    rx = 1;
                    break;

                case Data.RX.RX1S:
                case Data.RX.RX2S:
                    rx = 2;
                    break;

                case Data.RX.RX1R:
                case Data.RX.RX2R:
                    rx = 3;
                    break;
            }

            if (_data.Tx == Data.TX.TX1)
            {
                tx = 0;
            }
            else
            {
                tx = 4;
            }

            Send((byte)((byte)Messages.TXRX | tx | rx));
            Debug.WriteLine(((byte)Messages.TXRX | tx | rx).ToString("X"));
            _txrxPending = false;
        }

        /// <summary>
        /// Send the SO2R latch message
        /// </summary>
        private void SendLatch()
        {
            Send((byte)((_data.Latch) ? Messages.LATCHON : Messages.LATCHOFF));
            _latchPending = false;
        }

        /// <summary>
        /// Send the SO2R PTT message
        /// </summary>
        private void SendPtt()
        {
            Send((byte)((_data.Ptt) ? Messages.PTTON : Messages.PTTOFF));
            _pttPending = false;
        }

        /// <summary>
        /// Send the SO2R AUX1 message
        /// </summary>
        private void SendAux1()
        {
            Send((byte)((byte)Messages.AUX1 | _data.Aux1));
            _aux1Pending = false;
        }

        /// <summary>
        /// Send the SO2R AUX2 message
        /// </summary>
        private void SendAux2()
        {
            Send((byte)((byte)Messages.AUX2 | _data.Aux2));
            _aux2Pending = false;
        }

        /// <summary>
        /// Called when transmitter or receiver changes
        /// </summary>
        public void RxTX_Changed()
        {
            lock (LockObject)
            {
                if ((_wkIgnore != 0) || _wkAdmin)
                {
                    _txrxPending = true;
                }
                else
                {
                    SendTxRx();
                }
            }
        }

        /// <summary>
        /// Called when the latch mode changes
        /// </summary>
        public void Latch_Changed()
        {
            lock (LockObject)
            {
                if ((_wkIgnore != 0) || _wkAdmin)
                {
                    _latchPending = true;
                }
                else
                {
                    SendLatch();
                }
            }
        }

        /// <summary>
        /// Called when PTT changes
        /// </summary>
        public void Ptt_Changed()
        {
            lock (LockObject)
            {
                if ((_wkIgnore != 0) || _wkAdmin)
                {
                    _pttPending = true;
                }
                else
                {
                    SendPtt();
                }
            }
        }

        /// <summary>
        /// Called when AUX 1 changes
        /// </summary>
        public void Aux1_Changed()
        {
            lock (LockObject)
            {
                if ((_wkIgnore != 0) || _wkAdmin)
                {
                    _aux1Pending = true;
                }
                else
                {
                    SendAux1();
                }
            }
        }

        /// <summary>
        /// Called when AUX 2 changes
        /// </summary>
        public void Aux2_Changed()
        {
            lock (LockObject)
            {
                if ((_wkIgnore != 0) || _wkAdmin)
                {
                    _aux2Pending = true;
                }
                else
                {
                    SendAux2();
                }
            }
        }

        /// <summary>
        /// Called when a multi-byte Winkey command is completed
        /// </summary>
        private void UpdatePending()
        {
            if (_txrxPending)
            {
                Debug.WriteLine("TxRx was Pending");
                SendTxRx();
            }

            if (_pttPending)
            {
                Debug.WriteLine("PTT was Pending");
                SendPtt();
            }

            if (_latchPending)
            {
                Debug.WriteLine("Latch was Pending");
                SendLatch();
            }

            if (_aux1Pending)
            {
                Debug.WriteLine("AUX1 was Pending");
                SendAux1();
            }

            if (_aux2Pending)
            {
                Debug.WriteLine("AUX2 was Pending");
                SendAux2();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public Device(Data data)
        {
            _data = data;

            _settings.baudRate = 9600;
            _settings.port = _data.DevicePort;
        }
    }

}
