using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Tono;

namespace tSecretCommon.Models
{
    /// <summary>
    /// Model data : Record of Note
    /// </summary>
    public class Note : INotifyPropertyChanged
    {
        /// <summary>
        /// Time Span to separate create from update action
        /// </summary>
        private static readonly TimeSpan TIME_NEW_RECORD = TimeSpan.FromMinutes(60);

        /// <summary>
        /// ID
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// Persist data format (considering no migration even if format change)
        /// </summary>
        public Dictionary<string/*key*/, List<NoteHistRecord>> UniversalData = new Dictionary<string, List<NoteHistRecord>>();

        //=== NOTE: belows are NOT seriarized. Set [IgnoreDataMember] attribute ==============
        public event PropertyChangedEventHandler PropertyChanged;

        #region General override
        public override string ToString()
        {
            return $"ID={ID} / Caption={Caption} / AccountID={AccountID}";
        }
        public override bool Equals(object obj)
        {
            if (obj is Note tar)
            {
                return tar.ID.Equals(ID);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        #endregion

        [IgnoreDataMember]
        private bool isHidePassword = true;

        [IgnoreDataMember]
        public bool IsHidePassword
        {
            get => isHidePassword;
            set
            {
                isHidePassword = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsHidePassword"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PasswordImageName"));
            }
        }
        [IgnoreDataMember]
        public string PasswordImageName => $"cc_btn_Eye{(IsHidePassword ? 0 : 1)}.png";

        [IgnoreDataMember]
        public bool IsDirty { get; set; }

        public event EventHandler<EventArgs> ValueChanged;

        /// <summary>
        /// Get Latest history of record
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [IgnoreDataMember]
        public string this[string key, string def = ""]
        {
            get
            {
                List<NoteHistRecord> list = UniversalData.GetValueOrDefault(key, true, k => new List<NoteHistRecord>());
                if (list.Count > 0)
                {
                    return list[list.Count - 1].Value;
                }
                else
                {
                    return def;
                }
            }
            set
            {
                List<NoteHistRecord> list = UniversalData.GetValueOrDefault(key, true, k => new List<NoteHistRecord>());
                if (list.Count == 0)
                {
                    list.Add(NoteHistRecord.FromNow(value));
                    IsDirty = true;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    NoteHistRecord last = list[list.Count - 1];
                    if (last.Value?.Equals(value) ?? false)
                    {
                        return; // do nothing
                    }
                    else
                    {
                        if ((DateTime.Now - last.DT) > TIME_NEW_RECORD)
                        {
                            list.Add(NoteHistRecord.FromNow(value));
                        }
                        else
                        {
                            list[list.Count - 1] = NoteHistRecord.FromNow(value);
                        }
                        IsDirty = true;
                        ValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        [IgnoreDataMember]
        public string IDString => $"ID={ID}";

        [IgnoreDataMember]
        public string Caption
        {
            get => this["Caption"];
            set
            {
                this["Caption"] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Caption"));
            }
        }

        /// <summary>
        /// CaptionHistory
        /// </summary>
        [IgnoreDataMember]
        public IEnumerable<NoteHistRecord> CaptionHistory => UniversalData.GetValueOrDefault("Caption", true, k => new List<NoteHistRecord>()).OrderByDescending(a => a.DT);

        [IgnoreDataMember]
        public string CaptionRubi1 => Japanese.Getあかさたな(this["CaptionRubi"].Trim().Substring(0, 1).ToUpper());

        [IgnoreDataMember]
        public string CaptionRubi
        {
            get => this["CaptionRubi"];
            set
            {
                this["CaptionRubi"] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CaptionRubi"));
            }
        }

        /// <summary>
        /// CaptionRubiHistory
        /// </summary>
        [IgnoreDataMember]
        public IEnumerable<NoteHistRecord> CaptionRubiHistory => UniversalData.GetValueOrDefault("CaptionRubi", true, k => new List<NoteHistRecord>()).OrderByDescending(a => a.DT);

        [IgnoreDataMember]
        public string AccountID
        {
            get => this["AccountID"];
            set
            {
                this["AccountID"] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccountID"));
            }
        }
        [IgnoreDataMember]
        public IEnumerable<NoteHistRecord> AccountIDHistory => UniversalData.GetValueOrDefault("AccountID", true, k => new List<NoteHistRecord>()).OrderByDescending(a => a.DT);

        [IgnoreDataMember]
        public string Password
        {
            get => this["Password"];
            set
            {
                this["Password"] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Password"));
            }
        }
        [IgnoreDataMember]
        public string Email
        {
            get => this["Email"];
            set
            {
                this["Email"] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Email"));
            }
        }

        [IgnoreDataMember]
        public bool IsDeleted
        {
            get => DbUtil.ToBoolean(this["IsDeleted"]);
            set
            {
                this["IsDeleted"] = value.ToString();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsDeleted"));
            }
        }

        [IgnoreDataMember]
        public string Memo
        {
            get => this["Memo"];
            set
            {
                this["Memo"] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Memo"));
            }
        }

        [IgnoreDataMember]
        public IEnumerable<NoteHistRecord> PasswordHistory => UniversalData.GetValueOrDefault("Password", true, k => new List<NoteHistRecord>()).OrderByDescending(a => a.DT);

        [IgnoreDataMember]
        public IEnumerable<NoteHistRecord> EmailHistory => UniversalData.GetValueOrDefault("Email", true, k => new List<NoteHistRecord>()).OrderByDescending(a => a.DT);

        [IgnoreDataMember]
        public DateTime CreatedDateTime
        {
            get => DbUtil.ToDateTime(this["CreatedDateTime", DateTime.Now.ToString(TimeUtil.FormatYMDHMS)]);
            set
            {
                this["CreatedDateTime"] = value.ToString(TimeUtil.FormatYMDHMS);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CreatedDateTime"));
            }
        }
    }
}
