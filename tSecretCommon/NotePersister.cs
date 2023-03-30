// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using Azure.Storage.Blobs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tono;
using tSecretCommon.Models;

namespace tSecretCommon
{
    public class NotePersister : IEnumerable<Note>
    {
        private const string VERSION = "4.00";
        private static readonly Random rnd = new Random(DateTime.Now.Ticks.GetHashCode());
        private readonly MySecretParameter SecretParam = new MySecretParameter();
        private string DataFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"tSecret.localcache.{VERSION}.{(string.IsNullOrEmpty(UserObjectID) ? "null" : UserObjectID)}.dat");
        private string BlobName => $"MainData.{(string.IsNullOrEmpty(UserObjectID) ? "null" : UserObjectID)}.dat";
        private string UserObjectID = "";

        // Data
        private NoteList dat = null;

        private BlobContainerClient GetContainer(string containerName)
        {
            var container = new BlobContainerClient(SecretParam.AzureStorageConnectionString, containerName.ToLower());
            return container;
        }

        /// <summary>
        /// Upload data to Azure Blob Storage
        /// </summary>
        public async Task Upload()
        {
            if (dat == null)
            {
                return;
            }
            Debug.Write($"{BlobName} is uploading...");
            var planeCode = dat.MakeInstanceCode();
            var secureCode = RijndaelEncode(planeCode);
            var container = GetContainer("tsecret");
            _ = await container.CreateIfNotExistsAsync();
            var blob = container.GetBlobClient(BlobName);
            var secureData = Encoding.UTF8.GetBytes(secureCode);
            _ = await blob.UploadAsync(new MemoryStream(secureData), true);
            Debug.Write($"{DateTime.Now.ToString(TimeUtil.FormatYMDHMS)} {BlobName} have been uploaded succesfully.");
        }

        /// <summary>
        /// Sync with cloud data
        /// </summary>
        /// <returns></returns>
        public async Task<NoteList> Sync()
        {
            await Task.Delay(700);

            var cloud = await Download();
            if (cloud == null || cloud.Notes == null || cloud.Notes.Count == 0)
            {
                return dat;
            }

            foreach (var noteLocal in dat.Notes)
            {
                if (cloud.Contains(noteLocal))
                {
                    var noteCloud = cloud.Get(noteLocal);
                    foreach (var keyCloud in noteCloud.UniversalData.Keys)
                    {
                        var historyListCloud = noteCloud.UniversalData[keyCloud];
                        var historyListLocal = noteLocal.UniversalData.GetValueOrDefault(keyCloud, true, k => new List<NoteHistRecord>());
                        foreach (var historyCloud in historyListCloud)
                        {
                            var historyLocal = historyListLocal.Find(a => Math.Abs((a.DT - historyCloud.DT).TotalSeconds) < 1);
                            if (historyLocal == null)
                            {
                                historyListLocal.Add(historyCloud);  // Add cloud only data to local storage
                            }
                            else
                            {
                                if (historyLocal.Value.Equals(historyCloud.Value) == false)
                                {
                                    historyListLocal.Add(historyCloud);  // Fail safe 同じ時間なのに値が違うレコードはローカルに追加
                                }
                            }
                        }
                        historyListLocal.Sort((a, b) => a.DT.CompareTo(b.DT));
                    }
                }
            }

            // save cloud only data
            foreach (var noteCloud in cloud.Notes)
            {
                if (dat.Contains(noteCloud) == false)
                {
                    dat.Add(noteCloud);
                }
            }

            SaveFile();
            await Upload();

            return dat;
        }

        /// <summary>
        /// Download data from Azure Blob Storage
        /// </summary>
        /// <returns></returns>
        public async Task<NoteList> Download()
        {
            Debug.Write($"{BlobName} is downloading...");
            var container = GetContainer("tsecret");
            var blob = container.GetBlobClient(BlobName);
            _ = await container.CreateIfNotExistsAsync();
            var stream = new MemoryStream();
            _ = blob.DownloadTo(stream);
            stream.Flush();
            var secureText = Encoding.ASCII.GetString(stream.ToArray());
            var planeCode = RijndaelDecode(secureText);
            var ret = NoteList.MakeObjectFrom(planeCode); // JsonConvert.DeserializeObject<NoteList>(planeCode);
            Debug.Write($"{DateTime.Now.ToString(TimeUtil.FormatYMDHMS)} MainData.dat have been downloaded succesfully.");

            return ret;
        }

        private string FusionString(string basestr, string filter)
        {
            var nums = new[] { 157, 233, 227, 179, 41, 257, 31, 89, 59, 83, 109, 3, 107, 5, 241, 269, 281, 139, 211, 23, 127, 131, 223, 97, 199, 163, 277, 29, 73, 11, 193, 151, 79, 19, 7, 229, 167, 47, 197, 149, 103, 37, 239, 13, 113, 2, 53, 61, 137, 263, };
            var ret = new StringBuilder(basestr);
            var nB = basestr.Length;
            var nF = filter.Length;
            var offset = 0;
            for (var i = Math.Max(nB, nF) - 1; i >= 0; i--)
            {
                ret[i % nB] = SecretParam.TEXTSET64[(SecretParam.TEXTSET64.IndexOf(ret[i % nB]) + filter[i % nF] + nums[(i + offset) % nums.Length]) % SecretParam.TEXTSET64.Length];
                offset++;
            }
            return ret.ToString();
        }

        /// <summary>
        /// RIjndael Encoding
        /// </summary>
        /// <param name="planeText"></param>
        /// <returns></returns>
        private string RijndaelEncode(string planeText)
        {
            var iv = new StringBuilder();
            var ivN = 0;
            for (var ivi = 0; ivi < ivN + SecretParam.IVNPP; ivi++)
            {
                _ = iv.Append(SecretParam.TEXTSET64[rnd.Next(SecretParam.TEXTSET64.Length - 1)]);
            }
            using (var ri = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.ASCII.GetBytes(iv.ToString()),
                Key = Encoding.ASCII.GetBytes(FusionString(SecretParam.KEY, UserObjectID)),
            })
            {
                var enc = ri.CreateEncryptor(ri.Key, ri.IV);
                byte[] buf;
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(planeText);
                        }
                        buf = ms.ToArray();
                    }
                }
                return $"{SecretParam.TEXTSET64[ivN]}{iv}{Convert.ToBase64String(buf)}";
            }
        }

        /// <summary>
        /// Rijndael Decoding
        /// </summary>
        /// <param name="secText"></param>
        /// <returns></returns>
        private string RijndaelDecode(string secText)
        {
            var ivN = SecretParam.TEXTSET64.IndexOf(secText[0]);
            var iv = secText.Substring(1, ivN + SecretParam.IVNPP);
            var sec = secText.Substring(ivN + iv.Length + 1);

            using (var rijndael = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.ASCII.GetBytes(iv),
                Key = Encoding.ASCII.GetBytes(FusionString(SecretParam.KEY, UserObjectID)),
            })
            {
                var de = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
                using (var ms = new MemoryStream(Convert.FromBase64String(sec)))
                {
                    using (var cs = new CryptoStream(ms, de, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load for preparing
        /// </summary>
        /// <param name="isForceReload">true=load from disk forcely. / false=use memory cache</param>
        /// <returns></returns>
        public void LoadFile(string userObjectID, bool isForceReload = false)
        {
            if (string.IsNullOrEmpty(userObjectID))
            {
                return;
            }
            if (userObjectID.Equals(UserObjectID) == false)
            {
                isForceReload = true;
            }
            UserObjectID = userObjectID;

            if (dat != null && isForceReload == false)
            {
                return;
            }
            dat = null;
            if (File.Exists(DataFilePath))
            {
                try
                {
                    using (var fs = new FileStream(DataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            var sec = sr.ReadToEnd();
                            var planeText = RijndaelDecode(sec);
                            dat = NoteList.MakeObjectFrom(planeText, VERSION);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"!! NotePersister Exception : {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    dat = null;
                }
            }
            if (dat == null)
            {
                dat = new NoteList
                {
                    Version = VERSION,
                    Notes = new List<Note>(),
                };
            }
        }

        public void SaveFile()
        {
            if (dat == null)
            {
                return;
            }
            var planeCode = dat.MakeInstanceCode();
            var secureText = RijndaelEncode(planeCode);
            using (var sw = new StreamWriter(DataFilePath, false, Encoding.UTF8))
            {
                sw.Write(secureText);
                sw.Close();
            }
        }

        /// <summary>
        /// Add new item and save.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Add(Note item)
        {
            if (item != null)
            {
                LoadFile(UserObjectID);
                if (dat.Contains(item))
                {
                    dat.Remove(item);
                }
                dat.Add(item);
                SaveFile();
            }
        }

        /// <summary>
        /// Physical delete the specified instance
        /// </summary>
        /// <param name="item"></param>
        public void Remove(Note item)
        {
            if (item != null)
            {
                LoadFile(UserObjectID);
                if (dat.Contains(item))
                {
                    dat.Remove(item);
                }
                SaveFile();
            }
        }

        public IEnumerator<Note> GetEnumerator()
        {
            LoadFile(UserObjectID);
            return dat.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            LoadFile(UserObjectID);
            return dat.GetEnumerator();
        }
    }
}
