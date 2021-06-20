using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;


namespace Duan.Xiugang.Tractor
{
    /// <summary>
    /// 提供声音播放的方法
    /// </summary>
    public class MciSoundPlayer
    {

        [DllImport("winmm.dll", EntryPoint = "mciSendString", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int mciSendString(string lpstrCommand, [MarshalAs(UnmanagedType.LPTStr)]string lpstrReturnString, int uReturnLength, int hwndCallback);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName([MarshalAs(UnmanagedType.LPTStr)]string path, [MarshalAs(UnmanagedType.LPTStr)]StringBuilder shortPath, int shortPathLength);

        public string FileName;
        public string Alias;

        public MciSoundPlayer(string fn, string al)
        {
            this.FileName = fn;
            this.Alias = al;
        }

        public void LoadMediaFiles()
        {
            Stop();
            Close();

            StringBuilder shortPathTemp = new StringBuilder(255);
            int result = GetShortPathName(FileName, shortPathTemp, shortPathTemp.Capacity);
            string ShortPath = shortPathTemp.ToString();

            mciSendString(string.Format("open {0} alias {1}", ShortPath, Alias), "", 0, 0);
        }

        public void Play(bool enableSound)
        {
            if (!enableSound) return;
            mciSendString(string.Format("play {0} from 0", Alias), "", 0, 0);
        }

        public void Stop()
        {
            mciSendString(string.Format("stop {0}", Alias), "", 0, 0);
        }

        public void Pause()
        {
            mciSendString(string.Format("pause {0}", Alias), "", 0, 0);
        }

        public void Close()
        {
            mciSendString(string.Format("close {0}", Alias), "", 0, 0);
        }

        public void CloseAll()
        {
            mciSendString("close all", "", 0, 0);
        }

        public bool IsPlaying()
        {
            string durLength = "";
            durLength = durLength.PadLeft(128, Convert.ToChar(" "));
            mciSendString(string.Format("status {0} mode", Alias), durLength, 128, 0);

            return durLength.Substring(0, 7).ToLower() == "playing".ToLower();
        }
    }
}
