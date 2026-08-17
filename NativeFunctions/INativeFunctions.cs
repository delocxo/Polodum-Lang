using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum.NativeFunctions
{
    internal interface INativeFunctions
    {
        public string Name { get; }
        public Value Register();
    }
}
