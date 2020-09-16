using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using vnebo.mobi.bot.Libs;

namespace vnebo.mobi.bot
{
    internal class IniFiles
    {
        #region DLL IMPORT
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);
        #endregion

        #region ПЕРЕМЕННЫЕ
        private readonly string Path;
        #endregion

        public IniFiles(string IniPath)
        {
            Path = new FileInfo(IniPath).FullName.ToString();
        }

        private string Read(string Section, string Key)
        {
            StringBuilder RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", RetVal, 255, Path);

            return RetVal.ToString();
        }

        public string ReadString(string Section, string Key)
        {
            return Read(Section, Key);
        }

        public int ReadInt(string Section, string Key)
        {
            if (KeyExists(Section, Key))
            {
                return Convert.ToInt32(Read(Section, Key));
            }

            return 1;
        }

        public bool ReadBool(string Section, string Key)
        {
            if (KeyExists(Section, Key))
            {
                return HelpMethod.ToBoolean(Read(Section, Key));
            }

            return false;
        }

        public void Write(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, Path);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Section, Key, null);
        }

        public void DeleteSection(string Section = null)
        {
            Write(Section, null, null);
        }

        public bool KeyExists(string Section, string Key)
        {
            return Read(Section, Key).Length > 0;
        }
    }
}
