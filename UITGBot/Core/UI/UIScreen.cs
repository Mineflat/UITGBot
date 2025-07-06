using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UITGBot.Core.UI
{
    internal class UIScreen
    {
        public Table currentScreen {  get; set; } = new Table();
        public List<UIScreenItem> Items { get; set; } = new List<UIScreenItem>();

    }
}
