using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum.NativeFunctions
{
    internal class NativeFunctionsRegistry
    {
        static List<INativeFunctions> s_registry = new List<INativeFunctions>()
        {
            new RaylibFunctions(),
            new FileFunctions(),
            new MathFunctions()
        };

        public static void RegisterAll(Dictionary<string, Value> globals)
        {
            foreach (INativeFunctions nativeFunctions in s_registry)
                globals[nativeFunctions.Name] = nativeFunctions.Register();
        }
    }
}
