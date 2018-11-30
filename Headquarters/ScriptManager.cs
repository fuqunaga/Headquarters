using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Headquarters
{
    class ScriptManager
    {
        List<Script> scripts;

        public Script current => scripts.First();

        public ScriptManager(string dirpath)
        {
            var filepaths = Directory.GetFiles(dirpath, "*.ps1").ToList();

            scripts = filepaths.Select(path => new Script(path)).ToList();
        }
    }
}
