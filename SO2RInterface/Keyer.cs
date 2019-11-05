using JH.CommBase;

namespace SO2RInterface
{
    class Keyer : CommBase
    {
        private readonly CommBaseSettings _settings = new CommBaseSettings();

        /// <summary>
        /// Pointer to the business logic and data
        /// </summary>
        Data _data;

        /// <summary>
        /// Get the keyer line settings
        /// </summary>
        /// <returns></returns>
        protected override CommBaseSettings CommSettings()
        {
            return _settings;
        }

        /// <summary>
        /// Set up after opening keyer port
        /// </summary>
        /// <returns></returns>
        protected override bool AfterOpen()
        {
            _data.KeyerTxChar += KeyerTx;
            return true;
        }

        /// <summary>
        /// Clean up before closing keyer port
        /// </summary>
        /// <param name="error"></param>
        protected override void BeforeClose(bool error)
        {
            _data.KeyerTxChar -= KeyerTx;

        }

        /// <summary>
        /// Receive a byte from the Winkey port
        /// </summary>
        /// <param name="ch"></param>
        protected override void OnRxChar(byte ch)
        {
            _data.KeyerRxChar?.Invoke(ch);
        }

        /// <summary>
        /// Send a byte to the Winkey port
        /// </summary>
        /// <param name="ch"></param>
        protected void KeyerTx(byte ch)
        {
            Send(ch);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public Keyer(Data data)
        {
            _data = data;

            _settings.baudRate = 9600;
            _settings.port = _data.KeyerPort;
        }
    }
}
