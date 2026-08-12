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
        static Dictionary<string, int> s_nameToId = new Dictionary<string, int>();

        public static int Number = Register("number");
        public static int String = Register("string");
        public static int Bool = Register("bool");
        public static int Function = Register("function");

        public static int Register(string name)
        {
            if (s_nameToId.TryGetValue(name, out int existing))
                return existing;

            int id = s_types.Count;

            s_types.Add(new KindInfo(id, name));
            s_nameToId[name] = id;

            return id;
        }

        public static KindInfo Get(int id) => s_types[id];
        public static int GetId(string name) => s_nameToId[name];
        public static string GetName(int id) => Get(id).Name;
    }
}
