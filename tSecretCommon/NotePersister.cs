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

namespace tSecretCommon
{
    public class NotePersister : IEnumerable<Note>
    {
        public Authenticator Auth { get; set; }

        private const string VERSION = "3.00";
        private static readonly Random rnd = new Random(DateTime.Now.Ticks.GetHashCode());
        private readonly MySecretParameter SecretParam = new MySecretParameter();
        private string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"tSecret.localcache.{VERSION}.{Auth.UserObjectID}.dat");
        private string BlobName => $"MainData.{Auth.UserObjectID}.dat";

        // Data
        private NoteList dat = null;

        private CloudBlobContainer GetContainer(string containerName)
        {
            var account = CloudStorageAccount.Parse(SecretParam.AzureStorageConnectionString);
            var client = account.CreateCloudBlobClient();
            return client.GetContainerReference(containerName.ToLower());
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
            string json = JsonConvert.SerializeObject(dat);
            string sec = RijndaelEncode(json);

            var container = GetContainer("tsecret");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(BlobName);
            await blob.UploadFromStreamAsync(new MemoryStream(Encoding.UTF8.GetBytes(sec)));
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

            foreach (var lnote in dat.Notes)
            {
                if (cloud.Contains(lnote))
                {
                    var cnote = cloud.Get(lnote);
                    foreach (var ckey in cnote.UniversalData.Keys)
                    {
                        var clist = cnote.UniversalData[ckey];
                        var llist = lnote.UniversalData.GetValueOrDefault(ckey, true, k => new List<NoteHistRecord>());
                        foreach (var ch in clist)
                        {
                            var lh = llist.Find(a => Math.Abs((a.DT - ch.DT).TotalSeconds) < 1);
                            if (lh == null)
                            {
                                llist.Add(ch);  // Add cloud only data to local storage
                            }
                            else
                            {
                                if (lh.Value.Equals(ch.Value) == false)
                                {
                                    llist.Add(ch);  // Fail safe 同じ時間なのに値が違うレコードはローカルに追加
                                }
                            }
                        }
                        llist.Sort((a, b) => a.DT.CompareTo(b.DT));
                    }
                }
            }

            // save cloud only data
            foreach (var cnote in cloud.Notes)
            {
                if (dat.Contains(cnote) == false)
                {
                    dat.Add(cnote);
                }
            }

            Save();
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
            CloudBlobContainer container = GetContainer("tsecret");
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blob = container.GetBlockBlobReference(BlobName);
            string sec = await blob.DownloadTextAsync();
            string json = RijndaelDecode(sec);
            NoteList ret = JsonConvert.DeserializeObject<NoteList>(json);
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
                ret[i % nB] = SecretParam.TEXTSET64[(SecretParam.TEXTSET64.IndexOf(ret[i % nB]) + (int)filter[i % nF] + nums[(i + offset) % nums.Length]) % SecretParam.TEXTSET64.Length];
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
            int ivN = 0;
            for (int ivi = 0; ivi < ivN + SecretParam.IVNPP; ivi++)
            {
                iv.Append(SecretParam.TEXTSET64[rnd.Next(SecretParam.TEXTSET64.Length - 1)]);
            }
            using (var ri = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.ASCII.GetBytes(iv.ToString()),
                Key = Encoding.ASCII.GetBytes(FusionString(SecretParam.KEY, Auth.UserObjectID)),
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
                return ($"{SecretParam.TEXTSET64[ivN]}{iv}{Convert.ToBase64String(buf)}");
            }
        }

        /// <summary>
        /// Rijndael Decoding
        /// </summary>
        /// <param name="secText"></param>
        /// <returns></returns>
        private string RijndaelDecode(string secText)
        {
            int ivN = SecretParam.TEXTSET64.IndexOf(secText[0]);
            string iv = secText.Substring(1, ivN + SecretParam.IVNPP);
            string sec = secText.Substring(ivN + iv.Length + 1);

            using (var rijndael = new RijndaelManaged
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.ASCII.GetBytes(iv),
                Key = Encoding.ASCII.GetBytes(FusionString(SecretParam.KEY, Auth.UserObjectID)),
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
        public void Load(bool isForceReload = false)
        {
            if (dat != null && isForceReload == false)
            {
                return;
            }
            dat = null;
            if (File.Exists(FilePath))
            {
                try
                {
                    using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            var sec = sr.ReadToEnd();
                            var json = RijndaelDecode(sec);
                            dat = JsonConvert.DeserializeObject<NoteList>(json);
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

        /// <summary>
        /// Save the all note data
        /// </summary>
        /// <returns></returns>
        public void Save()
        {
            if (dat == null)
            {
                return;
            }
            var json = JsonConvert.SerializeObject(dat);
            var sec = RijndaelEncode(json);
            using (var sw = new StreamWriter(FilePath, false, Encoding.UTF8))
            {
                sw.Write(sec);
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
                Load();
                if (dat.Contains(item))
                {
                    dat.Remove(item);
                }
                dat.Add(item);
                Save();
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
                Load();
                if (dat.Contains(item))
                {
                    dat.Remove(item);
                }
                Save();
            }
        }

        public IEnumerator<Note> GetEnumerator()
        {
            Load();
            return dat.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Load();
            return dat.GetEnumerator();
        }
    }
}
