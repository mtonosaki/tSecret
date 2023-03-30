using System;
using System.Text;

namespace tSecretCommon.Models
{
    /// <summary>
    /// Record of History
    /// </summary>
    public class NoteHistRecord
    {
        public static NoteHistRecord FromNow(string value)
        {
            return new NoteHistRecord
            {
                DT = DateTime.Now,
                Value = value,
            };
        }

        public DateTime DT { get; set; }
        public string Value { get; set; } = "";

        public string MakeObjectString()
        {
            var ret = new StringBuilder();
            _ = ret.Append("HIST:");
            _ = ret.Append(DT.ToString("yyyy/MM/dd HH:mm:ss.fff"));
            _ = ret.Append("=");
            _ = ret.Append(System.Web.HttpUtility.UrlEncode(Value));
            return ret.ToString();
        }

        public static NoteHistRecord MakeObject(string[] lines, ref int step)
        {
            var ret = new NoteHistRecord();
            var line = lines[++step];
            if (!line.StartsWith("HIST:"))
            {
                throw new FormatException("Unexpected history format");
            }

            var pos = line.IndexOf('=');
            var dtstr = line.Substring(5, pos - 5);
            ret.DT = DateTime.Parse(dtstr);
            var val = line.Substring(pos + 1);
            ret.Value = System.Web.HttpUtility.UrlDecode(val);
            return ret;
        }


        public override string ToString()
        {
            return $"{Value} ({DT})";
        }
    }
}
