using System;
using System.Threading;
using System.Windows.Forms;

namespace SO2RInterface
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Mutex _mutex = new System.Threading.Mutex(false, "SO2RInterface");
            try
            {
                if (_mutex.WaitOne(0, false))
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form());
                }
                else
                {
                    //MessageBox.Show("An instance of the application is already running.");
                }
            }
            finally
            {
                if (_mutex != null)
                {
                    _mutex.Close();
                }
            }
        }
    }
}
