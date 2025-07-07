using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UITGBot.Core.UI
{
    internal class UIScreenItem
    {
        public delegate void Runner();
        public string Title { get; set; } = "~ ~ ~";
        public Runner? ExecAfter { get; set; }
    }
}
