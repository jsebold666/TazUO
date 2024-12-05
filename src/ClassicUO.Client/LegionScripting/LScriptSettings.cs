using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.LegionScripting
{
    internal class LScriptSettings
    {
        public List<string> GlobalAutoStartScripts { get; set; } = new List<string>();
        public Dictionary<string, List<string>> CharAutoStartScripts { get; set; } = new Dictionary<string, List<string>>();
    }
}
