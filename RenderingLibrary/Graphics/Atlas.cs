using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public class Atlas : IDisposable
    {
        public bool IsDisposed
        {
            get;
            private set;
        }

        private readonly IDictionary<string, AtlasedTexture> atlasDictionary;

        public Atlas()
        {
            atlasDictionary = new Dictionary<string, AtlasedTexture>();
        }

        public void Add(string name, AtlasedTexture sprite)
        {
            atlasDictionary.Add(name, sprite);
        }

        public bool Contains(string name)
        {
            return atlasDictionary.ContainsKey(name);
        }

        public void Add(Atlas otherSheet)
        {
            foreach (var sprite in otherSheet.atlasDictionary)
            {
                atlasDictionary.Add(sprite);
            }
        }

        public AtlasedTexture Get(string sprite)
        {
            return this.atlasDictionary[sprite];
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
