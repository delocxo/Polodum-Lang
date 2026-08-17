using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum.NativeFunctions
{
    internal class FileFunctions : INativeFunctions
    {
        public string Name { get; } = "file";

        public Value Register()
        {
            Namespace file = new Namespace("file");
            Value fileValue = new Value(file);

            file.SetNative(Value.FromNativeExpected("exists", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                return new Value(File.Exists(path));
            }));

            file.SetNative(Value.FromNativeExpected("read", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    return new Value(File.ReadAllText(path));
                }
                catch
                {
                    throw new Error($"Error reading file: '{path}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeExpected("tryRead", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                try
                {
                    return Vm.MakeSome(new Value(File.ReadAllText(path)));
                }
                catch
                {
                    return Vm.MakeNone();
                }
            }));

            file.SetNative(Value.FromNativeMinimum("write", ["path"], "contents", fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                string[] contents = new string[args.Count - 1];
                for (int i = 1; i < args.Count; i++)
                    contents[i - 1] = args[i].ToString();
                try
                {
                    File.WriteAllText(path, string.Join("", contents));
                    return Vm.MakeNone();
                }
                catch
                {
                    throw new Error($"Error writing to file: '{path}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeMinimum("tryWrite", ["path"], "contents", fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                string[] contents = new string[args.Count - 1];
                for (int i = 1; i < args.Count; i++)
                    contents[i - 1] = args[i].ToString();
                try
                {
                    File.WriteAllText(path, string.Join("", contents));
                    return Value.True;
                }
                catch
                {
                    return Value.False;
                }
            }));

            file.SetNative(Value.FromNativeMinimum("append", ["path"], "contents", fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                string[] contents = new string[args.Count - 1];
                for (int i = 1; i < args.Count; i++)
                    contents[i - 1] = args[i].ToString();
                try
                {
                    File.AppendAllText(path, string.Join("", contents));
                    return Vm.MakeNone();
                }
                catch
                {
                    throw new Error($"Error appending to file: '{path}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeMinimum("tryAppend", ["path"], "contents", fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                string[] contents = new string[args.Count - 1];
                for (int i = 1; i < args.Count; i++)
                    contents[i - 1] = args[i].ToString();
                try
                {
                    File.AppendAllText(path, string.Join("", contents));
                    return Value.True;
                }
                catch
                {
                    return Value.False;
                }
            }));

            file.SetNative(Value.FromNativeExpected("delete", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    File.Delete(path);
                    return Vm.MakeNone();
                }
                catch
                {
                    throw new Error($"Error deleting file: '{path}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeExpected("tryDelete", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                try
                {
                    if (!File.Exists(path))
                        return Value.False;
                     File.Delete(path);
                    return Value.True;
                }
                catch
                {
                    return Value.False;
                }
            }));

            file.SetNative(Value.FromNativeExpected("copy", ["path", "destination"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                string dest = args[1].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    File.Copy(path, dest);
                    return Vm.MakeNone();
                }
                catch
                {
                    throw new Error($"Error copying from '{path}' to '{dest}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeExpected("tryCopy", ["path", "destination"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                string dest = args[1].ExpectKind(ValueKind.String, "Expected string", pos).String;
                try
                {
                    File.Copy(path, dest);
                    return Value.True;
                }
                catch
                {
                    return Value.False;
                }
            }));

            file.SetNative(Value.FromNativeExpected("move", ["path", "destination"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                string dest = args[1].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    File.Move(path, dest);
                    return Vm.MakeNone();
                }
                catch
                {
                    throw new Error($"Error moving file from '{path}' to '{dest}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeExpected("tryMove", ["path", "destination"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                string dest = args[1].ExpectKind(ValueKind.String, "Expected string", pos).String;
                try
                {
                    File.Move(path, dest);
                    return Value.True;
                }
                catch
                {
                    return Value.False;
                }
            }));

            file.SetNative(Value.FromNativeExpected("size", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    return new Value(new FileInfo(path).Length);
                }
                catch
                {
                    throw new Error($"Error getting size of file: '{path}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeExpected("extension", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    return new Value(Path.GetExtension(path));
                }
                catch
                {
                    throw new Error($"Error getting extension of file: '{path}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeExpected("fileName", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    return new Value(Path.GetFileName(path));
                }
                catch
                {
                    throw new Error($"Error getting file name of file: '{path}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeExpected("fullPath", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    return new Value(Path.GetFullPath(path));
                }
                catch
                {
                    throw new Error($"Error getting full path of file: '{path}'", pos);
                }
            }));

            file.SetNative(Value.FromNativeExpected("fileNameNoExtension", ["path"], fileValue, (args, pos) =>
            {
                string path = args[0].ExpectKind(ValueKind.String, "Expected string", pos).String;
                ErrorIfFileDoesntExist(path, pos);
                try
                {
                    return new Value(Path.GetFileNameWithoutExtension(path));
                }
                catch
                {
                    throw new Error($"Error getting file name without extension of file: '{path}'", pos);
                }
            }));

            return fileValue;
        }

        void ErrorIfFileDoesntExist(string path, Position position)
        {
            if (!File.Exists(path))
                throw new Error($"Path '{path}' does not exist", position);
        }
    }
}
