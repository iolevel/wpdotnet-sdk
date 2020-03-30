using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Peachpied.WordPress.Build.Plugin
{
    public class PluginTask : Task
    {
        public override bool Execute()
        {
            return true;
        }
    }
}
