// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
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

namespace tSecretCommon {
    public class NotePersister: IEnumerable<Note> {
        private const string VERSION = "3.00";
        private static readonly Random rnd = new Random(DateTime.Now.Ticks.GetHashCode());
        private readonly MySecretParameter SecretParam = new MySecretParameter();
        private string DataFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"tSecret.localcache.{VERSION}.{(string.IsNullOrEmpty(UserObjectID) ? "null" : UserObjectID)}.dat");
        private string SettingFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"tSecret.localcache.{VERSION}.{(string.IsNullOrEmpty(UserObjectID) ? "null" : UserObjectID)}.dat");
        private string BlobName => $"MainData.{(string.IsNullOrEmpty(UserObjectID) ? "null" : UserObjectID)}.dat";
        private string UserObjectID = "";

        // Data
        private NoteList dat = null;

        private CloudBlobContainer GetContainer(string containerName) {
            CloudStorageAccount account = CloudStorageAccount.Parse(SecretParam.AzureStorageConnectionString);
            CloudBlobClient client = account.CreateCloudBlobClient();
            return client.GetContainerReference(containerName.ToLower());
        }

        /// <summary>
        /// Upload data to Azure Blob Storage
        /// </summary>
        public async Task Upload() {
            if (dat == null) {
                return;
            }
            Debug.Write($"{BlobName} is uploading...");
            string json = JsonConvert.SerializeObject(dat);
            string sec = RijndaelEncode(json);

            CloudBlobContainer container = GetContainer("tsecret");
            _ = await container.CreateIfNotExistsAsync();
            CloudBlockBlob blob = container.GetBlockBlobReference(BlobName);
            await blob.UploadFromStreamAsync(new MemoryStream(Encoding.UTF8.GetBytes(sec)));
            Debug.Write($"{DateTime.Now.ToString(TimeUtil.FormatYMDHMS)} {BlobName} have been uploaded succesfully.");
        }

        /// <summary>
        /// Sync with cloud data
        /// </summary>
        /// <returns></returns>
        public async Task<NoteList> Sync(bool requestToUploadToCloud) {
            await Task.Delay(700);

            NoteList cloud = await Download();
            if (cloud == null || cloud.Notes == null || cloud.Notes.Count == 0) {
                return dat;
            }

            foreach (Note lnote in dat.Notes) {
                if (cloud.Contains(lnote)) {
                    Note cnote = cloud.Get(lnote);
                    foreach (string ckey in cnote.UniversalData.Keys) {
                        List<NoteHistRecord> clist = cnote.UniversalData[ckey];
                        List<NoteHistRecord> llist = lnote.UniversalData.GetValueOrDefault(ckey, true, k => new List<NoteHistRecord>());
                        foreach (NoteHistRecord ch in clist) {
                            NoteHistRecord lh = llist.Find(a => Math.Abs((a.DT - ch.DT).TotalSeconds) < 1);
                            if (lh == null) {
                                llist.Add(ch);  // Add cloud only data to local storage
                            } else {
                                if (lh.Value.Equals(ch.Value) == false) {
                                    llist.Add(ch);  // Fail safe 同じ時間なのに値が違うレコードはローカルに追加
                                }
                            }
                        }
                        llist.Sort((a, b) => a.DT.CompareTo(b.DT));
                    }
                }
            }

            // save cloud only data
            foreach (Note cnote in cloud.Notes) {
                if (dat.Contains(cnote) == false) {
                    dat.Add(cnote);
                }
            }

            SaveFile();

            if (requestToUploadToCloud) {
                await Upload();
            }
            return dat;
        }

        /// <summary>
        /// Download data from Azure Blob Storage
        /// </summary>
        /// <returns></returns>
        public async Task<NoteList> Download() {
            Debug.Write($"{BlobName} is downloading...");
            CloudBlobContainer container = GetContainer("tsecret");
            _ = await container.CreateIfNotExistsAsync();
            CloudBlockBlob blob = container.GetBlockBlobReference(BlobName);
            string sec = await blob.DownloadTextAsync();
            string json = RijndaelDecode(sec);
            NoteList ret = JsonConvert.DeserializeObject<NoteList>(json);
            Debug.Write($"{DateTime.Now.ToString(TimeUtil.FormatYMDHMS)} MainData.dat have been downloaded succesfully.");

            return ret;
        }

        private string FusionString(string basestr, string filter) {
            int[] nums = new[] { 157, 233, 227, 179, 41, 257, 31, 89, 59, 83, 109, 3, 107, 5, 241, 269, 281, 139, 211, 23, 127, 131, 223, 97, 199, 163, 277, 29, 73, 11, 193, 151, 79, 19, 7, 229, 167, 47, 197, 149, 103, 37, 239, 13, 113, 2, 53, 61, 137, 263, };
            StringBuilder ret = new StringBuilder(basestr);
            int nB = basestr.Length;
            int nF = filter.Length;
            int offset = 0;
            for (int i = Math.Max(nB, nF) - 1; i >= 0; i--) {
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
        private string RijndaelEncode(string planeText) {
            StringBuilder iv = new StringBuilder();
            int ivN = 0;
            for (int ivi = 0; ivi < ivN + SecretParam.IVNPP; ivi++) {
                _ = iv.Append(SecretParam.TEXTSET64[rnd.Next(SecretParam.TEXTSET64.Length - 1)]);
            }
            using (RijndaelManaged ri = new RijndaelManaged {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.ASCII.GetBytes(iv.ToString()),
                Key = Encoding.ASCII.GetBytes(FusionString(SecretParam.KEY, UserObjectID)),
            }) {
                ICryptoTransform enc = ri.CreateEncryptor(ri.Key, ri.IV);
                byte[] buf;
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, enc, CryptoStreamMode.Write)) {
                        using (StreamWriter sw = new StreamWriter(cs)) {
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
        private string RijndaelDecode(string secText) {
            int ivN = SecretParam.TEXTSET64.IndexOf(secText[0]);
            string iv = secText.Substring(1, ivN + SecretParam.IVNPP);
            string sec = secText.Substring(ivN + iv.Length + 1);

            using (RijndaelManaged rijndael = new RijndaelManaged {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.ASCII.GetBytes(iv),
                Key = Encoding.ASCII.GetBytes(FusionString(SecretParam.KEY, UserObjectID)),
            }) {
                ICryptoTransform de = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(sec))) {
                    using (CryptoStream cs = new CryptoStream(ms, de, CryptoStreamMode.Read)) {
                        using (StreamReader sr = new StreamReader(cs)) {
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
        public void LoadFile(string userObjectID, bool isForceReload = false) {
            if (string.IsNullOrEmpty(userObjectID)) {
                return;
            }
            if (userObjectID.Equals(UserObjectID) == false) {
                isForceReload = true;
            }
            UserObjectID = userObjectID;

            if (dat != null && isForceReload == false) {
                return;
            }
            dat = null;
            if (File.Exists(DataFilePath)) {
                try {
                    using (FileStream fs = new FileStream(DataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        using (StreamReader sr = new StreamReader(fs, Encoding.UTF8)) {
                            string sec = sr.ReadToEnd();
                            string json = RijndaelDecode(sec);
                            dat = JsonConvert.DeserializeObject<NoteList>(json);
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"!! NotePersister Exception : {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    dat = null;
                }
            }
            if (dat == null) {
                dat = new NoteList {
                    Version = VERSION,
                    Notes = new List<Note>(),
                };
            }
        }

        public void SaveFile() {
            if (dat == null) {
                return;
            }
            string json = JsonConvert.SerializeObject(dat);
            string sec = RijndaelEncode(json);
            using (StreamWriter sw = new StreamWriter(DataFilePath, false, Encoding.UTF8)) {
                sw.Write(sec);
                sw.Close();
            }
        }

        /// <summary>
        /// Add new item and save.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Add(Note item) {
            if (item != null) {
                LoadFile(UserObjectID);
                if (dat.Contains(item)) {
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
        public void Remove(Note item) {
            if (item != null) {
                LoadFile(UserObjectID);
                if (dat.Contains(item)) {
                    dat.Remove(item);
                }
                SaveFile();
            }
        }

        public IEnumerator<Note> GetEnumerator() {
            LoadFile(UserObjectID);
            return dat.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            LoadFile(UserObjectID);
            return dat.GetEnumerator();
        }
    }
}
