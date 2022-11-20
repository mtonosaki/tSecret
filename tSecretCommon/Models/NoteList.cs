using System.Collections.Generic;
using System.Runtime.Serialization;
using Tono;

namespace tSecretCommon.Models {
    public class NoteList {
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<Note> Notes { get; set; } = new List<Note>();

        //=== NOTE: belows are NOT seriarized. Set [IgnoreDataMember] attribute ==============
        [IgnoreDataMember]
        public string Version {
            get => Attributes.GetValueOrDefault("Version");
            set => Attributes["Version"] = value;
        }

        public void Add(Note item) {
            Notes.Add(item);
        }

        public bool Contains(Note item) {
            return Notes.Contains(item);
        }

        public void Remove(Note item) {
            _ = Notes.Remove(item);
        }

        public Note Get(Note tar) {
            return Notes.Find(a => a.ID.Equals(tar.ID));
        }

        public IEnumerator<Note> GetEnumerator() {
            return Notes.GetEnumerator();
        }
    }
}
