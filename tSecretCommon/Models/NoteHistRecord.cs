using System;

namespace tSecretCommon.Models {
    /// <summary>
    /// Record of History
    /// </summary>
    public class NoteHistRecord {
        public static NoteHistRecord FromNow(string value) {
            return new NoteHistRecord {
                DT = DateTime.Now,
                Value = value,
            };
        }

        public DateTime DT { get; set; }
        public string Value { get; set; } = "";
        public override string ToString() {
            return $"{Value} ({DT})";
        }
    }
}
