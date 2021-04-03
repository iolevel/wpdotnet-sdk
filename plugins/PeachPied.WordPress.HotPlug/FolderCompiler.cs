using System;
using System.Collections.Generic;
using System.Text;

namespace PeachPied.WordPress.HotPlug
{
    class FolderCompiler : IDisposable
    {
        public string RootPath { get; }

        public string SubPath { get; }

        public FolderCompiler(string rootPath, string subPath, string outputAssemblyName)
        {
            this.RootPath = rootPath;
            this.SubPath = subPath;
        }

        public FolderCompiler Build(bool watch)
        {
            return this;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
