using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Tono;

namespace tSecretCommon.Models
{
    public class NoteList
    {
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<Note> Notes { get; set; } = new List<Note>();

        //=== NOTE: belows are NOT seriarized. Set [IgnoreDataMember] attribute ==============
        public string MakeInstanceCode()
        {
            var ret = new StringBuilder();
            _ = ret.Append($"TSECRET:DATA:VERSION:{Version}");
            _ = ret.Append('\n');
            _ = ret.Append("%%%%----SEGMENT-ATTRIBUTES----%%%%");
            _ = ret.Append('\n');
            _ = ret.Append($"{Attributes.Count}");
            _ = ret.Append('\n');
            foreach (var kv in Attributes)
            {
                _ = ret.Append($"{kv.Key}={System.Web.HttpUtility.UrlEncode(kv.Value)}");
                _ = ret.Append('\n');
            }
            _ = ret.Append("%%%%----SEGMENT-NOTES----%%%%");
            _ = ret.Append('\n');
            _ = ret.Append($"{Notes.Count}");
            _ = ret.Append('\n');
            foreach (var note in Notes)
            {
                _ = ret.Append(note.MakeObjectString());
            }
            _ = ret.Append($"TSECRET:DATA:END");
            return ret.ToString();
        }

        public static NoteList MakeObjectFrom(string str, string expectedVersion = null)
        {
            try
            {
                var ret = new NoteList();
                var lines = str.Split('\n');
                var step = 0;
                var line = lines[step];

                var startTag = "TSECRET:DATA:VERSION:";
                if (!line.StartsWith(startTag))
                {
                    throw new FormatException("Start tag is not expected one.");
                }

                if (expectedVersion != null)
                {
                    var version = line.Substring(startTag.Length);
                    if (version != expectedVersion)
                    {
                        throw new FormatException("Version number is not same.");
                    }
                }

                line = lines[++step];
                if (line != "%%%%----SEGMENT-ATTRIBUTES----%%%%")
                {
                    throw new FormatException("Segment error 1");
                }

                ret.Attributes.Clear();
                line = lines[++step];
                var listCount = int.Parse(line);
                if (listCount > 99999 || listCount < 0)
                {
                    throw new FormatException("Attribute count over flow");
                }

                foreach (var index in Enumerable.Range(0, listCount))
                {
                    line = lines[++step];
                    var pos = line.IndexOf('=');
                    var key = line.Substring(0, pos);
                    var val = line.Substring(pos + 1);
                    ret.Attributes[key] = System.Web.HttpUtility.UrlDecode(val);
                }

                line = lines[++step];
                if (line != "%%%%----SEGMENT-NOTES----%%%%")
                {
                    throw new FormatException("Segment error 2");
                }

                line = lines[++step];
                listCount = int.Parse(line);
                if (listCount > 99999 || listCount < 0)
                {
                    throw new FormatException("Note count over flow");
                }

                ret.Notes.Clear();

                foreach (var index in Enumerable.Range(0, listCount))
                {
                    var note = Note.MakeObject(lines, ref step);
                    ret.Notes.Add(note);
                }

                return ret;
            }
            catch (Exception ex)
            {
                if (ex is FormatException)
                {
                    throw ex;
                }
                else
                {
                    throw new FormatException(ex.Message);
                }
            }
        }


        [IgnoreDataMember]
        public string Version
        {
            get => Attributes.GetValueOrDefault("Version");
            set => Attributes["Version"] = value;
        }

        public void Add(Note item)
        {
            Notes.Add(item);
        }

        public bool Contains(Note item)
        {
            return Notes.Contains(item);
        }

        public void Remove(Note item)
        {
            _ = Notes.Remove(item);
        }

        public Note Get(Note tar)
        {
            return Notes.Find(a => a.ID.Equals(tar.ID));
        }

        public IEnumerator<Note> GetEnumerator()
        {
            return Notes.GetEnumerator();
        }
    }
}
