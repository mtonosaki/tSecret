// (c) 2019 Manabu Tonosaki
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace tSecretCommon
{
    public class StoryNode
    {
        public StreamWriter Message { get; set; }
        public CancellationTokenSource CTS { get; set; }
        public Func<StoryNode, Task<bool>> Task { get; set; }
        public string TaskName { get; set; }
        public StoryNode Success { get; set; }
        public StoryNode Error { get; set; }

        public override string ToString()
        {
            return $"{TaskName ?? base.ToString()}";
        }
    }
}
