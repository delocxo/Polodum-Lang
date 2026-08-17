using Polodum.NativeFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Polodum
{
    internal class CallFrame
    {
        public CallFrame(Chunk chunk)
        {
            Chunk = chunk;
            Locals = new Value[chunk.LocalCount];
        }

        public Chunk Chunk { get; }
        public int Ip { get; set; }
        public Value[] Locals { get; }

        public Position GetPosition(Instruction instruction) => Chunk.Positions[instruction.PositionIndex];
        public Value GetConstant(int index) => Chunk.Constants[index];
        public List<Instruction> Instructions => Chunk.Instructions;
        public Stack<Value> Stack { get; } = new Stack<Value>(1024);
    }

    internal class Vm
    {
        Stack<CallFrame> _frames = new Stack<CallFrame>();
        Value[] _vmGlobals;

        public static Value MakeNone() => Value.FromRecord([], ValueKind.None);

        public static Value MakeSome(Value value) => Value.FromRecord(new Dictionary<string, RecordField>()
        {
            {
                "value",
                new("value", false, value)
            }
        }, ValueKind.Some);

        static Value MakeField(string name, bool mutable, Value value) => Value.FromRecord(new Dictionary<string, RecordField>()
        {
            {
                "name",
                new("name", false, new Value(name))
            },
            {
                "mutable",
                new("mutable", false, new Value(mutable))
            },
            {
                "value",
                new("value", false, value)
            }
        }, ValueKind.Field);

        static Value MakeEnumValue(string enumName, string name, int value) => Value.FromRecord(new Dictionary<string, RecordField>()
        {
            {
                "enum",
                new RecordField("enum", false, new Value(enumName))
            },
            {
                "name",
                new RecordField("name", false, new Value(name))
            },
            {
                "value",
                new RecordField("value", false, new Value(value))
            }
        }, ValueKind.EnumValue);

        static Value MakeEnum(string name, List<string> values, Position position)
        {
            Dictionary<string, RecordField> fields = new Dictionary<string, RecordField>()
            {
                {
                    "name",
                    new RecordField("name", false, new Value(name))
                }
            };

            for (int i = 0; i < values.Count; i++)
            {
                string valueName = values[i];

                if (valueName == "name")
                    throw new Error("Enum member 'name' is reserved", position);

                if (fields.ContainsKey(valueName))
                    throw new Error($"Enum already contains value '{valueName}'", position);

                fields.Add(valueName, new RecordField(valueName, false, MakeEnumValue(name, valueName, i)));
            }

            return Value.FromRecord(fields, ValueKind.Enum);
        }

        Dictionary<string, Record> _existingRecords = new Dictionary<string, Record>()
        {
            {
                "Some",
                MakeSome(MakeNone()).Record
            },
            {
                "None",
                MakeNone().Record
            },
            {
                "Field",
                MakeField("", false, MakeNone()).Record
            },
            {
                "Enum",
                MakeEnum("", [], new Position(0, 0, "")).Record
            },
            {
                "EnumValue",
                MakeEnumValue("", "", 0).Record
            }
        };

        Dictionary<string, Value> _globals = new Dictionary<string, Value>()
        {
            {
                "input",
                Value.FromNativeExpected("input", [], null, (_, _) =>
                {
                    return new Value(Console.ReadLine() ?? "");
                })
            },
            {
                "print",
                Value.FromNativeExpected("print", ["object"], null, (args, _) =>
                {
                    Console.Write(args[0]);
                    return MakeNone();
                })
            },
            {
                "println",
                Value.FromNativeExpected("println", ["object"], null, (args, _) =>
                {
                    Console.WriteLine(args[0]);
                    return MakeNone();
                })
            },
            {
                "typeof",
                Value.FromNativeExpected("typeof", ["object"], null, (args, _) =>
                {
                    return new Value(args[0].KindName);
                })
            },
            {
                "some",
                Value.FromNativeExpected("some", ["value"], null, (args, _) =>
                {
                    return Value.FromRecord(new Dictionary<string, RecordField>()
                    {
                        {
                            "value",
                            new("value", false, args[0])
                        }
                    }, ValueKind.Some);
                })
            },
            {
                "none",
                Value.FromNativeExpected("none", [], null, (_, _) =>
                {
                    return Value.FromRecord([], ValueKind.None);
                })
            },
            {
                "enum",
                Value.FromNativeMinimum("enum", ["name"], "values", null, (args, pos) =>
                {
                    List<string> names = new List<string>();

                    string name = args[0]
                        .ExpectKinds("Expected string as enum name", pos, ValueKind.String)
                        .String;

                    for (int i = 1; i < args.Count; i++)
                    {
                        if (!args[i].IsKind(ValueKind.String))
                            throw new Error("Expected string for enum values", pos);
                        names.Add(args[i].String);
                    }

                    return MakeEnum(name, names, pos);
                })
            },
            {
                "toString",
                Value.FromNativeExpected("toString", ["value"], null, (args, _) =>
                {
                    return new Value(args[0].ToString());
                })
            },
        };

        void MatchRecord(string name, Record record, Position position)
        {
            if (_existingRecords.TryGetValue(name, out Record? other))
            {
                if (record.Fields.Count != other.Fields.Count)
                    throw new Error($"Record '{name}' does not match the record definition", position);

                foreach (var field in record.Fields)
                {
                    RecordField myField = field.Value;

                    if (!other.Fields.TryGetValue(field.Key, out RecordField? otherField))
                        throw new Error($"Record '{name}' does not match the record definition", position);

                    if (myField.Name != otherField.Name)
                        throw new Error($"Record '{name}' does not match the record definition", position);

                    if (myField.Mutable != otherField.Mutable)
                        throw new Error($"Record '{name}' does not match the record definition", position);
                }
                return;
            }
            _existingRecords.Add(name, record);
        }

        public Vm(Chunk chunk)
        {
            NativeFunctionsRegistry.RegisterAll(_globals);
            _vmGlobals = new Value[chunk.GlobalCount];
            _frames.Push(new CallFrame(chunk));
        }

        public void Execute()
        {
            for (; ; )
            {
                CallFrame callFrame = _frames.Peek();
                Instruction instruction = callFrame.Instructions[callFrame.Ip++];
                Stack<Value> stack = callFrame.Stack;

                switch (instruction.Opcode)
                {
                    case Opcode.LoadLocal:
                        stack.Push(callFrame.Locals[instruction.A]);
                        break;

                    case Opcode.StoreLocal:
                        callFrame.Locals[instruction.A] = stack.Pop();
                        break;

                    case Opcode.LoadGlobal:
                        stack.Push(_vmGlobals[instruction.A]);
                        break;

                    case Opcode.StoreGlobal:
                        _vmGlobals[instruction.A] = stack.Pop();
                        break;

                    case Opcode.LoadConst:
                        stack.Push(callFrame.GetConstant(instruction.A));
                        break;

                    case Opcode.MakeArray:
                        {
                            int argCount = instruction.A;

                            PoloArray array = new PoloArray(argCount);

                            for (int i = 0; i < argCount; i++)
                                array.Add(default);

                            for (int i = argCount - 1; i >= 0; i--)
                                array[i] = stack.Pop();

                            stack.Push(new Value(array));
                            break;
                        }

                    case Opcode.MakeRecord:
                        {
                            MakeRecord(stack, callFrame, instruction);
                            break;
                        }

                    case Opcode.UnpackStoreLocals:
                        {
                            Value value = stack.Pop();

                            bool isGlobal = instruction.A == 1 ? true : false;
                            int[] slots = instruction.Extra;
                            Position position = callFrame.GetPosition(instruction);

                            if (value.IsRecord)
                            {
                                Record record = value.Record;

                                if (slots.Length > record.Fields.Count)
                                    throw new Error($"Unpack variable count more than record field count", position);

                                var fields = record.Fields.Values;

                                for (int i = 0; i < slots.Length; i++)
                                {
                                    if (slots[i] == -1)
                                        continue;

                                    RecordField recordField = fields[i];

                                    if (isGlobal)
                                        _vmGlobals[slots[i]] = recordField.Value;
                                    else
                                        callFrame.Locals[slots[i]]  = recordField.Value;
                                }

                                break;
                            }

                            throw new Error($"Type '{value.KindName}' cannot be unpacked into variables", position);
                        }

                    case Opcode.GetName:
                        {
                            string name = callFrame.GetConstant(instruction.A).String;

                            if (_globals.TryGetValue(name, out Value value))
                            {
                                stack.Push(value);
                                break;
                            }

                            throw new Error($"'{name}' does not exist", callFrame.GetPosition(instruction));
                        }

                    // Guaranteed
                    case Opcode.GetLength:
                        {
                            Value value = stack.Pop();

                            if (value.IsKind(ValueKind.Array))
                            {
                                stack.Push(new Value(value.Array.Count));
                                break;
                            }

                            stack.Push(new Value(value.String.Length));
                            break;
                        }

                    case Opcode.CanIterateStoreLocal:
                        {
                            Value value = stack.Pop();
                            int local = instruction.A;

                            value.ExpectKinds($"Type '{value.Kind}' cannot be iterated over", callFrame.GetPosition(instruction), ValueKind.String, ValueKind.Array);

                            callFrame.Locals[local] = value;
                            break;
                        }

                    case Opcode.Add:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number + right.Number));
                                break;
                            }
                            else if (left.IsKind(ValueKind.String))
                            {
                                stack.Push(new Value(left.String + right.ToString()));
                                break;
                            }
                            else if (right.IsKind(ValueKind.String))
                            {
                                stack.Push(new Value(left.ToString() + right.String));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "+", instruction);
                        }

                    case Opcode.Sub:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number - right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "-", instruction);
                        }

                    case Opcode.Mul:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number * right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "*", instruction);
                        }

                    case Opcode.Div:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number / right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "/", instruction);
                        }

                    case Opcode.Mod:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number % right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "%", instruction);
                        }

                    case Opcode.Less:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number < right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "<", instruction);
                        }

                    case Opcode.Greater:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number > right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, ">", instruction);
                        }

                    case Opcode.LessEq:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number <= right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "<=", instruction);
                        }

                    case Opcode.GreaterEq:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(left.Number >= right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, ">=", instruction);
                        }

                    case Opcode.Equals:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            stack.Push(new Value(Value.CheckEquallity(left, right)));

                            break;
                        }

                    case Opcode.NotEqual:
                        {
                            Value right = stack.Pop();
                            Value left = stack.Pop();

                            stack.Push(new Value(!Value.CheckEquallity(left, right)));

                            break;
                        }

                    case Opcode.Not:
                        {
                            Value right = stack.Pop();

                            if (right.IsKind(ValueKind.Bool))
                            {
                                stack.Push(new Value(!right.Bool));
                                break;
                            }

                            throw new Error($"Cannot apply flip to {right.KindName}", callFrame.GetPosition(instruction));
                        }

                    case Opcode.Neg:
                        {
                            Value right = stack.Pop();

                            if (right.IsKind(ValueKind.Number))
                            {
                                stack.Push(new Value(-right.Number));
                                break;
                            }

                            throw new Error($"Cannot apply negate to {right.KindName}", callFrame.GetPosition(instruction));
                        }

                    case Opcode.Unpack:
                        {
                            Value right = stack.Pop();

                            if (right.IsKind(ValueKind.Array))
                            {
                                stack.Push(Value.FromUnpack(right.Array));
                                break;
                            }
                            else if (right.IsRecord)
                            {
                                Record record = right.Record;
                                PoloArray values = new PoloArray(record.Fields.Count);
                                foreach (var field in record.Fields.Values)
                                    values.Add(field.Value);
                                stack.Push(Value.FromUnpack(values));
                                break;
                            }

                            throw new Error($"Cannot unpack type '{right.KindName}'", callFrame.GetPosition(instruction));
                        }

                    case Opcode.Is:
                        {
                            Value left = stack.Pop();
                            Value type = callFrame.GetConstant(instruction.A);

                            if (!ValueKind.NameToId.TryGetValue(type.String, out int typeId))
                                throw new Error($"'{type.String}' is not a valid type", callFrame.GetPosition(instruction));

                            stack.Push(new Value(left.Kind == typeId));
                            break;
                        }

                    case Opcode.Isnt:
                        {
                            Value left = stack.Pop();
                            Value type = callFrame.GetConstant(instruction.A);

                            if (!ValueKind.NameToId.TryGetValue(type.String, out int typeId))
                                throw new Error($"'{type.String}' is not a valid type", callFrame.GetPosition(instruction));

                            stack.Push(new Value(left.Kind != typeId));
                            break;
                        }

                    case Opcode.JumpIfFalse:
                        {
                            Value condition = stack.Peek();
                            if (!Value.IsTruthy(condition))
                                callFrame.Ip = instruction.A;
                            break;
                        }

                    case Opcode.JumpIfTrue:
                        {
                            Value condition = stack.Peek();
                            if (Value.IsTruthy(condition))
                                callFrame.Ip = instruction.A;
                            break;
                        }

                    case Opcode.JumpIfFalsePop:
                        {

                            Value condition = stack.Pop();
                            if (!Value.IsTruthy(condition))
                                callFrame.Ip = instruction.A;
                            break;
                        }

                    case Opcode.JumpIfTruePop:
                        {
                            Value condition = stack.Pop();
                            if (Value.IsTruthy(condition))
                                callFrame.Ip = instruction.A;
                            break;
                        }

                    case Opcode.Jump:
                        {
                            callFrame.Ip = instruction.A;
                            break;
                        }

                    case Opcode.Call:
                        {
                            int argCount = instruction.A;
                            Value target = stack.Pop();
                            PoloArray arguments = new PoloArray(argCount);
                            Position position = callFrame.GetPosition(instruction);

                            for (int i = 0; i < argCount; i++)
                            {
                                Value argument = stack.Pop();
                                if (argument.IsKind(ValueKind.Unpack))
                                    for (int j = argument.Array.Count - 1; j >= 0; j--)
                                        arguments.Add(argument.Array[j]);
                                else
                                    arguments.Add(argument);
                            }

                            arguments.Reverse();

                            if (target.IsKind(ValueKind.Function))
                            {
                                FunctionInfo functionInfo = target.FunctionInfo;

                                ValidateArguments(functionInfo.Arity, arguments.Count, ArgumentMode.Expected, functionInfo.Name, position);

                                CallFrame newCallFrame = new CallFrame(functionInfo.Chunk);

                                for (int i = 0; i < functionInfo.Arity; i++)
                                    newCallFrame.Locals[i] = arguments[i];

                                _frames.Push(newCallFrame);

                                break;
                            }
                            else if (target.IsKind(ValueKind.NativeFunction))
                            {
                                NativeFunction nativeFunction = target.Native;

                                ValidateArguments(nativeFunction.Arity, arguments.Count, nativeFunction.ArgumentMode, nativeFunction.Name, position);

                                stack.Push(nativeFunction.Native(arguments, position));

                                break;
                            }

                            throw new Error($"Type '{target.KindName}' is not callable", callFrame.GetPosition(instruction));
                        }

                    case Opcode.Index:
                        {
                            Position position = callFrame.GetPosition(instruction);

                            Value target = stack.Pop();

                            target = target
                                .ExpectKinds($"Type '{target.KindName}' cannot be indexed", position, ValueKind.Array, ValueKind.String);

                            Value index = stack.Pop();

                            if (target.Kind == ValueKind.Array)
                            {
                                PoloArray array = target.Array;

                                int raw = index.ExpectIntInRangeEx(0, array.Count, "Array index out of range", position);

                                stack.Push(array[raw]);

                                break;
                            }
                            else
                            {
                                string str = target.String;

                                int raw = index.ExpectIntInRangeEx(0, str.Length, "String index out of range", position);

                                stack.Push(new Value(str[raw].ToString()));

                                break;
                            }
                        }

                    case Opcode.IndexSet:
                        {
                            Position position = callFrame.GetPosition(instruction);

                            Value target = stack.Pop();

                            target = target
                                .ExpectKinds($"Type '{target.KindName}' cannot be indexed", position, ValueKind.Array);

                            Value index = stack.Pop();

                            Value value = stack.Pop();

                            PoloArray array = target.Array;

                            int raw = index.ExpectIntInRangeEx(0, array.Count, "Array index out of range", position);

                            array[raw] = value;

                            break;
                        }

                    case Opcode.GetMember:
                        {
                            Value target = stack.Pop();
                            string memberName = callFrame.GetConstant(instruction.A).String;
                            Position position = callFrame.GetPosition(instruction);
                            stack.Push(GetMember(target, memberName, position));
                            break;
                        }

                    case Opcode.MemberSet:
                        {
                            Value target = stack.Pop();
                            Value value = stack.Pop();
                            string memberName = callFrame.GetConstant(instruction.A).String;
                            Position position = callFrame.GetPosition(instruction);

                            if (target.IsRecord)
                            {
                                Record record = target.Record;

                                if (!record.Fields.TryGetValue(memberName, out RecordField? recordField))
                                    throw new Error($"Record '{target.KindName}' does not contain field '{memberName}'", position);
                                
                                if (!recordField.Mutable)
                                    throw new Error($"Field '{recordField.Name}' of '{target.KindName}' is not mutable", position);

                                recordField.Value = value;
                                break;
                            }

                            throw new Error($"Type '{target.KindName}' cannot be member set", position);
                        }

                    case Opcode.Out:
                        {
                            Console.Write(stack.Pop());
                            break;
                        }

                    case Opcode.Pop:
                        {
                            stack.Pop();
                            break;
                        }

                    case Opcode.Ret:
                        {
                            Value result = stack.Pop();
                            _frames.Pop();
                            if (_frames.Count == 0)
                                return;
                            _frames.Peek().Stack.Push(result);
                            break;
                        }

                    case Opcode.Halt:
                        {
                            // Halt will always mean the program ends, its never in a function
                            return;
                        }
                }
            }
        }

        Value GetArrayMembers(Value arrayValue, string member, Position position)
        {
            PoloArray array = arrayValue.Array;

            if (member == "length")
                return new Value(array.Count);

            else if (member == "isEmpty")
                return new Value(array.Count == 0);

            else if (member == "push")
                return Value.FromNativeExpected("push", ["item"], arrayValue, (args, _) =>
                {
                    array.Add(args[0]);
                    return MakeNone();
                });

            else if (member == "pushRange")
                return Value.FromNativeExpected("pushRange", ["otherArray"], arrayValue, (args, pos) =>
                {
                    PoloArray otherArray = args[0]
                        .ExpectKinds("Expected other array", pos, ValueKind.Array)
                        .Array;
                    array.AddRange(otherArray);
                    return MakeNone();
                });

            else if (member == "insert")
                return Value.FromNativeExpected("insert", ["index", "item"], arrayValue, (args, pos) =>
                {
                    int index = args[0]
                        .ExpectIntInRangeIn(0, array.Count, "Insert index out of range", pos);
                    array.Insert(index, args[1]);
                    return MakeNone();
                });

            else if (member == "insertRange")
                return Value.FromNativeExpected("insertRange", ["index", "otherArray"], arrayValue, (args, pos) =>
                {
                    int index = args[0]
                        .ExpectIntInRangeIn(0, array.Count, "Insert index out of range", pos);
                    PoloArray otherArray = args[1]
                        .ExpectKinds("Expected other array", pos, ValueKind.Array)
                        .Array;
                    array.InsertRange(index, otherArray);
                    return MakeNone();
                });

            else if (member == "remove")
                return Value.FromNativeExpected("remove", ["value"], arrayValue, (args, pos) =>
                {
                    for (int i = 0; i < array.Count; i++)
                        if (Value.CheckEquallity(args[0], array[i]))
                        {
                            array.RemoveAt(i);
                            return Value.True;
                        }
                    return Value.False;
                });

            else if (member == "removeAt")
                return Value.FromNativeExpected("removeAt", ["index"], arrayValue, (args, pos) =>
                {
                    int index = args[0]
                        .ExpectIntInRangeEx(0, array.Count, "Remove index out of range", pos);
                    array.RemoveAt(index);
                    return MakeNone();
                });

            else if (member == "contains")
                return Value.FromNativeExpected("contains", ["value"], arrayValue, (args, pos) =>
                {
                    for (int i = 0; i < array.Count; i++)
                        if (Value.CheckEquallity(args[0], array[i]))
                            return Value.True;
                    return Value.False;
                });

            else if (member == "indexOf")
                return Value.FromNativeExpected("indexOf", ["value"], arrayValue, (args, pos) =>
                {
                    for (int i = 0; i < array.Count; i++)
                        if (Value.CheckEquallity(args[0], array[i]))
                            return new Value(i);
                    return new Value(-1);
                });

            else if (member == "clear")
                return Value.FromNativeExpected("clear", [], arrayValue, (args, pos) =>
                {
                    array.Clear();
                    return MakeNone();
                });

            else if (member == "reverse")
                return Value.FromNativeExpected("reverse", [], arrayValue, (args, pos) =>
                {
                    array.Reverse();
                    return MakeNone();
                });

            else if (member == "copy")
                return Value.FromNativeExpected("copy", [], arrayValue, (args, pos) =>
                {
                    PoloArray newArray = [.. array];
                    return new Value(newArray);
                });

            else if (member == "getRange")
                return Value.FromNativeExpected("getRange", ["start", "end"], arrayValue, (args, pos) =>
                {
                    int start = args[0]
                        .ExpectIntInRangeIn(0, array.Count, "Start index out of range", pos);

                    int end = args[1]
                        .ExpectIntInRangeIn(start, array.Count, "End index out of range", pos);

                    return new Value(array.GetRange(start, end - start));
                });

            throw new Error($"Type 'array' does not contain member '{member}'", position);
        }

        Value GetStringMembers(Value stringValue, string member, Position position)
        {
            string str = stringValue.String;

            if (member == "length")
                return new Value(str.Length);

            else if (member == "isEmpty")
                return new Value(str.Length == 0);

            else if (member == "contains")
                return Value.FromNativeExpected("contains", ["value"], stringValue, (args, _) =>
                {
                    return new Value(str.Contains(args[0].ToString()));
                });

            else if (member == "indexOf")
                return Value.FromNativeExpected("indexOf", ["value"], stringValue, (args, _) =>
                {
                    return new Value(str.IndexOf(args[0].ToString()));
                });

            else if (member == "startsWith")
                return Value.FromNativeExpected("startsWith", ["value"], stringValue, (args, _) =>
                {
                    return new Value(str.StartsWith(args[0].ToString()));
                });

            else if (member == "endsWith")
                return Value.FromNativeExpected("endsWith", ["value"], stringValue, (args, _) =>
                {
                    return new Value(str.EndsWith(args[0].ToString()));
                });

            else if (member == "trim")
                return Value.FromNativeExpected("trim", [], stringValue, (args, _) =>
                {
                    return new Value(str.Trim());
                });

            else if (member == "trimStart")
                return Value.FromNativeExpected("trimStart", [], stringValue, (args, _) =>
                {
                    return new Value(str.TrimStart());
                });

            else if (member == "trimEnd")
                return Value.FromNativeExpected("trimEnd", [], stringValue, (args, _) =>
                {
                    return new Value(str.TrimEnd());
                });

            else if (member == "toLower")
                return Value.FromNativeExpected("toLower", [], stringValue, (args, _) =>
                {
                    return new Value(str.ToLower());
                });

            else if (member == "toUpper")
                return Value.FromNativeExpected("toUpper", [], stringValue, (args, _) =>
                {
                    return new Value(str.ToUpper());
                });

            else if (member == "sub")
                return Value.FromNativeExpected("sub", ["start", "end"], stringValue, (args, pos) =>
                {
                    int start = args[0]
                        .ExpectIntInRangeIn(0, str.Length, "Start index out of range", pos);

                    int end = args[1]
                        .ExpectIntInRangeIn(start, str.Length, "End index out of range", pos);

                    return new Value(str.Substring(start, end - start));
                });

            else if (member == "replace")
                return Value.FromNativeExpected("replace", ["old", "new"], stringValue, (args, pos) =>
                {
                    return new Value(str.Replace(args[0].ToString(), args[1].ToString()));
                });

            else if (member == "repeat")
                return Value.FromNativeExpected("repeat", ["amount"], stringValue, (args, pos) =>
                {
                    int amount = args[0]
                        .ExpectIntInRangeIn(0, int.MaxValue, "Repeat amount out of range", pos);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < amount; i++)
                        sb.Append(str);
                    return new Value(sb.ToString());
                });

            else if (member == "split")
                return Value.FromNativeExpected("split", ["separator"], stringValue, (args, pos) =>
                {
                    string[] split = str.Split(args[0].ToString());
                    PoloArray splitArray = new PoloArray(split.Length);
                    for (int i = 0; i < split.Length; i++)
                        splitArray.Add(new Value(split[i]));
                    return new Value(splitArray);
                });

            else if (member == "isAlpha")
                return Value.FromNativeExpected("isAlpha", [], stringValue, (args, pos) =>
                {
                    return new Value(str.Length > 0 && str.All(char.IsLetter));
                });

            else if (member == "isDigit")
                return Value.FromNativeExpected("isDigit", [], stringValue, (args, pos) =>
                {
                    return new Value(str.Length > 0 && str.All(char.IsDigit));
                });

            else if (member == "isWhite")
                return Value.FromNativeExpected("isWhite", [], stringValue, (args, pos) =>
                {
                    return new Value(str.Length > 0 && str.All(char.IsWhiteSpace));
                });

            else if (member == "isAlphaDigit")
                return Value.FromNativeExpected("isAlphaDigit", [], stringValue, (args, pos) =>
                {
                    return new Value(str.Length > 0 && str.All(char.IsLetterOrDigit));
                });

            else if (member == "tryParse")
                return Value.FromNativeExpected("tryParse", [], stringValue, (args, pos) =>
                {
                    if (double.TryParse(str, out double result))
                        return MakeSome(new Value(result));
                    return MakeNone();
                });

            else if (member == "parse")
                return Value.FromNativeExpected("parse", [], stringValue, (args, pos) =>
                {
                    if (double.TryParse(str, out double result))
                        return new Value(result);
                    throw new Error("Failed to convert string to number", pos);
                });

            throw new Error($"Type 'string' does not contain member '{member}'", position);
        }

        Value GetNumberMembers(Value numberValue, string member, Position position)
        {
            double number = numberValue.Number;

            if (member == "isWhole")
                return Value.FromNativeExpected("isWhole", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsInteger(number));
                });

            else if (member == "trunc")
                return Value.FromNativeExpected("trunc", [], numberValue, (args, _) =>
                {
                    return new Value(double.Truncate(number));
                });

            else if (member == "isNan")
                return Value.FromNativeExpected("isNan", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsNaN(number));
                });

            else if (member == "isInfinity")
                return Value.FromNativeExpected("isInfinity", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsInfinity(number));
                });

            else if (member == "isPositiveInfinity")
                return Value.FromNativeExpected("isPositiveInfinity", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsPositiveInfinity(number));
                });

            else if (member == "isNegativeInfinity")
                return Value.FromNativeExpected("isNegativeInfinity", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsNegativeInfinity(number));
                });

            else if (member == "isFinite")
                return Value.FromNativeExpected("isFinite", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsFinite(number));
                });

            else if (member == "isNormal")
                return Value.FromNativeExpected("isNormal", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsNormal(number));
                });

            else if (member == "isSubnormal")
                return Value.FromNativeExpected("isSubnormal", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsSubnormal(number));
                });

            else if (member == "isNegative")
                return Value.FromNativeExpected("isNegative", [], numberValue, (args, _) =>
                {
                    return new Value(double.IsNegative(number));
                });

            throw new Error($"Type 'number' does not contain member '{member}'", position);
        }

        Value GetMember(Value target, string memberName, Position position)
        {
            if (target.IsRecord)
            {
                Record record = target.Record;

                if (memberName == "fieldCount")
                {
                    return new Value(record.Fields.Count);
                }
                else if (memberName == "getField")
                {
                    return Value.FromNativeExpected("getField", ["key"], target, (args, pos) =>
                    {
                        Value key = args[0];
                        key.ExpectKinds($"'{target.KindName}' getField expects only those types", pos, ValueKind.String, ValueKind.Number);

                        if (key.IsKind(ValueKind.String))
                        {
                            if (!record.Fields.TryGetValue(key.String, out RecordField? recordField))
                                throw new Error($"Record '{target.KindName}' does not contain field '{key.String}'", pos);

                            return MakeField(recordField.Name, recordField.Mutable, recordField.Value);
                        }

                        int intKey = key.ExpectIntInRangeEx(0, record.Fields.Count, $"'{target.KindName}' getField index out of range", pos);

                        RecordField otherField = record.Fields.ElementAt(intKey).Value;

                        return MakeField(otherField.Name, otherField.Mutable, otherField.Value);
                    });
                }
                else if (memberName == "getFields")
                {

                    return Value.FromNativeExpected("getFields", [], target, (args, pos) =>
                    {
                        PoloArray fieldArray = new PoloArray();

                        foreach (var fieldPair in record.Fields)
                        {
                            RecordField field = fieldPair.Value;
                            fieldArray.Add(MakeField(field.Name, field.Mutable, field.Value));
                        }

                        return new Value(fieldArray);
                    });
                }

                if (!record.Fields.TryGetValue(memberName, out RecordField? recordField))
                    throw new Error($"Record '{target.KindName}' does not contain field '{memberName}'", position);

                return recordField.Value;
            }

            else if (target.IsKind(ValueKind.Array))
            {
                return GetArrayMembers(target, memberName, position);
            }

            else if (target.IsKind(ValueKind.String))
            {
                return GetStringMembers(target, memberName, position);
            }

            else if (target.IsKind(ValueKind.Namespace))
            {
                return target.Namespace.Get(memberName, position);
            }

            else if (target.IsKind(ValueKind.Number))
            {
                return GetNumberMembers(target, memberName, position);
            }

            throw new Error($"Type '{target.KindName}' cannot be member accessed", position);
        }

        void MakeRecord(Stack<Value> stack, CallFrame callFrame, Instruction instruction)
        {
            string name = callFrame.GetConstant(instruction.A).String;
            int fieldCount = instruction.B;

            Dictionary<string, RecordField> recordFields = new Dictionary<string, RecordField>();

            for (int i = fieldCount - 1; i >= 0; i--)
            {
                string fieldName = stack.Pop().String;
                bool mutable = stack.Pop().Bool;
                Value value = stack.Pop();

                recordFields.Add(fieldName, new RecordField(fieldName, mutable, value));
            }

            recordFields = recordFields.Reverse().ToDictionary(x => x.Key, x => x.Value);

            int id = ValueKind.Register(name);

            Record record = new Record(recordFields, id);

            MatchRecord(name, record, callFrame.GetPosition(instruction));

            stack.Push(new Value(record));
        }

        Error ThrowBinaryError(Value left, Value right, string op, Instruction instruction)
        {
            CallFrame current = _frames.Peek();
            Position position = current.GetPosition(instruction);
            return new Error($"Cannot apply '{op}' to {left.KindName} and {right.KindName}", position); 
        }

        void ValidateArguments(int arity, int got, ArgumentMode mode, string name, Position position)
        {
            if (mode == ArgumentMode.Expected)
            {
                if (got != arity)
                    throw new Error($"Function '{name}' expected {arity} argument(s), got {got}", position);
            }
            else if (mode == ArgumentMode.Unlimited)
                return;
            else if (mode == ArgumentMode.Minimum)
            {
                if (got < arity)
                    throw new Error($"Function '{name}' expects atleast {arity} argument(s), got {got}", position);
            }
        }
    }
}
