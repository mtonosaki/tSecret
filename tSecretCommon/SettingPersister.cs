// (c) 2020 Manabu Tonosaki
// Licensed under the MIT license.

using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace tSecretCommon
{
    public class SettingPersister : PersistSetting
    {
        private string SettingFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"tSecret.setting.dat");

        public void SaveFile()
        {
            var json = JsonConvert.SerializeObject((PersistSetting)this);
            using (var sw = new StreamWriter(SettingFilePath, false, Encoding.UTF8))
            {
                sw.Write(json);
                sw.Close();
            }
        }

        public void LoadFile()
        {
            if (File.Exists(SettingFilePath))
            {
                try
                {
                    using (var fs = new FileStream(SettingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            var json = sr.ReadToEnd();
                            var dat = JsonConvert.DeserializeObject<PersistSetting>(json);
                            CopyFrom(dat);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"!! Setting Persister Exception : {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
    public class PersistSetting
    {
        public string LastUserObjectID { get; set; }
        public string LastDisplayName { get; set; }
        public DateTime LastLoginTimeUtc { get; set; }

        public void CopyFrom(PersistSetting dat)
        {
            LastUserObjectID = dat.LastUserObjectID;
            LastDisplayName = dat.LastDisplayName;
            LastLoginTimeUtc = dat.LastLoginTimeUtc;
        }
    }
}
