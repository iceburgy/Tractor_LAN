using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Duan.Xiugang.Tractor
{
    static class Program
    {
        private static Mutex mutex = null;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string nickName = FormSettings.GetSettingString(FormSettings.KeyNickName);
            bool createdNew;
            mutex = new Mutex(true, nickName, out createdNew);

            if (!createdNew)
            {
                //app is already running! Exiting the application  
                MessageBox.Show("Another instance is already running, exiting now!");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}