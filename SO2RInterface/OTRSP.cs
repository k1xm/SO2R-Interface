using JH.CommBase;
using System;
using System.Diagnostics;

namespace SO2RInterface
{
    class Otrsp : CommLine
    {
        /// <summary>
        /// Serial port line settings
        /// </summary>
        private readonly CommLineSettings _settings = new CommLineSettings();

        /// <summary>
        /// Pointer to the business logic and data
        /// </summary>
        private readonly Data _data;

        /// <summary>
        /// Get the OTRSP line settings
        /// </summary>
        /// <returns></returns>
        protected override CommBaseSettings CommSettings()
        {
            Setup(_settings);
            return _settings;
        }

        /// <summary>
        /// True if sending RX events
        /// </summary>
        private bool _erx = false;

        /// <summary>
        /// True if sending TX events
        /// </summary>
        private bool _etx = false;

        /// <summary>
        /// Called when the transmitter changes
        /// </summary>
        public void Tx_Changed()
        {
            if (_etx)
            {
                SendTx();
            }
        }

        /// <summary>
        /// Called when the receiver changes
        /// </summary>
        /// <param name="newRx"></param>
        public void Rx_Changed()
        {
            if (_erx)
            {
                SendRx();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public Otrsp(Data data)
        {
            _data = data;

            _settings.baudRate = 9600;
            _settings.port = _data.OtrspPort;
        }

        /// <summary>
        /// OTRSP-specific code for when the serial port is opened
        /// </summary>
        /// <returns></returns>
        protected override bool AfterOpen()
        {
            _data.Tx_Changed += Tx_Changed;
            _data.Rx_Changed += Rx_Changed;
            return true;
        }

        /// <summary>
        /// OTRSP-specific code for when the serial port is closed
        /// </summary>
        /// <param name="error"></param>
        protected override void BeforeClose(bool error)
        {
            _data.Tx_Changed -= Tx_Changed;
            _data.Rx_Changed -= Rx_Changed;
        }

        /// <summary>
        /// Called when a line of text is received on the serial port
        /// </summary>
        /// <param name="cmd"></param>
        protected override void OnRxLine(string cmd)
        {
            Debug.WriteLine(cmd);

            if (cmd == "")
            {
                return;
            }

            if (cmd[0] == '?')
            {
                switch (cmd)
                {
                    case "?TX":
                        SendTx();
                        break;

                    case "?RX":
                        SendRx();
                        break;

                    case "?NAME":
                        Send(_data.Devicename + '\r');
                        break;

                    case "?FW":
                        Send("FW" + _data.Version + '\r');
                        break;

                    case "?ERX":
                        Send((_erx) ? "ERX1\r" : "ERX0\r");
                        break;

                    case "?ETX":
                        Send((_etx) ? "ETX1\r" : "ETX0\r");
                        break;

                    case "?VLATCH":
                        Send((_data.Latch) ? "VLATCH1\r" : "VLATCH0\r");
                        break;

                    case "?AUX1":
                        Send("AUX1" + _data.Aux1.ToString() + "\r");
                        break;

                    case "?AUX2":
                        Send("AUX2" + _data.Aux2.ToString() + "\r");
                        break;

                    default:
                        Send(cmd + "\r");
                        break;
                }
                return;
            }

            switch (cmd)
            {
                case "TX1":
                    _data.Tx = Data.TX.TX1;
                    break;

                case "TX2":
                    _data.Tx = Data.TX.TX2;
                    break;

                case "RX1":
                    _data.Rx = Data.RX.RX1;
                    break;

                case "RX2":
                    _data.Rx = Data.RX.RX2;
                    break;

                case "RX1S":
                    _data.Rx = Data.RX.RX1S;
                    break;

                case "RX2S":
                    _data.Rx = Data.RX.RX2S;
                    break;

                case "RX1R":
                    _data.Rx = Data.RX.RX1R;
                    break;

                case "RX2R":
                    _data.Rx = Data.RX.RX2R;
                    break;

                case "ERX0":
                    _erx = false;
                    break;

                case "ERX1":
                    _erx = true;
                    SendRx();
                    break;

                case "ETX0":
                    _etx = false;
                    break;

                case "ETX1":
                    _etx = true;
                    SendTx();
                    break;

                case "VLATCH0":
                    _data.Latch = false;
                    break;

                case "VLATCH1":
                    _data.Latch = true;
                    break;

                case "VLATCHT":
                    _data.Latch = !_data.Latch;
                    break;

                default:
                    if (cmd.StartsWith("AUX1") && (cmd.Length > "AUX10".Length))
                    {
                        if (Int32.TryParse(cmd.Substring("AUX1".Length), out int _aux1))
                        {
                            _data.Aux1 = _aux1;
                        }
                    }

                    if (cmd.StartsWith("AUX2") && (cmd.Length > "AUX20".Length))
                    {
                        if (Int32.TryParse(cmd.Substring("AUX2".Length), out int _aux2))
                        {
                            _data.Aux2 = _aux2;
                        }
                    }

                    break;
            }
        }

        protected override void OnStatusChange(ModemStatus mask, ModemStatus state)
        {
            if (mask.cts)
            {
                // CTS is PTT on from OTRSP
                _data.Ptt = state.cts;
            }
        }

        private void SendTx()
        {
            switch (_data.Tx)
            {
                case Data.TX.TX1:
                    Send("TX1\r");
                    break;

                case Data.TX.TX2:
                    Send("TX2\r");
                    break;
            }
        }

        private void SendRx()
        {
            switch (_data.Rx)
            {
                case Data.RX.RX1:
                    Send("RX1\r");
                    break;

                case Data.RX.RX2:
                    Send("RX2\r");
                    break;

                case Data.RX.RX1S:
                    Send("RX1S\r");
                    break;

                case Data.RX.RX2S:
                    Send("RX2S\r");
                    break;

                case Data.RX.RX1R:
                    Send(((_data.Capabilities & 1) == 1) ? "RX1R\r" : "RX1S\r");
                    break;

                case Data.RX.RX2R:
                    Send(((_data.Capabilities & 1) == 1) ? "RX2R\r" : "RX2S\r");
                    break;
            }
        }
    }
}
