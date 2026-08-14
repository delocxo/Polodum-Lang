using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum
{
    internal class KindInfo
    {
        public KindInfo(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }

    internal static class ValueKind
    {
        static List<KindInfo> s_types = new List<KindInfo>();
        public static Dictionary<string, int> NameToId = new Dictionary<string, int>();

        // Core
        public static int Number = Register("number");
        public static int String = Register("string");
        public static int Bool = Register("bool");
        public static int Function = Register("function");
        public static int Array = Register("array");

        // Runtime Core
        public static int NativeFunction = Register("native");
        public static int Some = Register("Some");
        public static int None = Register("None");
        public static int Field = Register("Field");

        public static int Register(string name)
        {
            if (NameToId.TryGetValue(name, out int existing))
                return existing;

            int id = s_types.Count;

            s_types.Add(new KindInfo(id, name));
            NameToId[name] = id;

            return id;
        }

        public static KindInfo Get(int id) => s_types[id];
        public static int GetId(string name) => NameToId[name];
        public static string GetName(int id) => Get(id).Name;
    }
}
