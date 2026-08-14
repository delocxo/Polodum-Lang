using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
        Value[] _globals;
        Dictionary<string, Record> _existingRecords = new Dictionary<string, Record>();

        public void MatchRecord(string name, Record record, Position position)
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
            }
        }

        public Vm(Chunk chunk)
        {
            _globals = new Value[chunk.GlobalCount];
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
                        stack.Push(_globals[instruction.A]);
                        break;

                    case Opcode.StoreGlobal:
                        _globals[instruction.A] = stack.Pop();
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

                            int id = ValueKind.Register(name);

                            Record record = new Record(recordFields, id);

                            MatchRecord(name, record, callFrame.GetPosition(instruction));

                            stack.Push(new Value(record));
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
                            List<Value> arguments = new List<Value>(argCount);

                            for (int i = 0; i < argCount; i++)
                                arguments.Add(default);

                            for (int i = argCount - 1; i >= 0; i--)
                                arguments[i] = stack.Pop();

                            if (target.IsKind(ValueKind.Function))
                            {
                                FunctionInfo functionInfo = target.FunctionInfo;

                                ValidateArguments(functionInfo.Arity, argCount, ArgumentMode.Expected, functionInfo.Name, callFrame.GetPosition(instruction));

                                CallFrame newCallFrame = new CallFrame(functionInfo.Chunk);

                                for (int i = 0; i < functionInfo.Arity; i++)
                                    newCallFrame.Locals[i] = arguments[i];

                                _frames.Push(newCallFrame);

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

                            if (target.IsRecord)
                            {
                                Record record = target.Record;
                                if (!record.Fields.TryGetValue(memberName, out RecordField? recordField))
                                    throw new Error($"Record '{target.KindName}' does not contain field '{memberName}'", position);
                                stack.Push(recordField.Value);
                                break;
                            }

                            throw new Error($"Type '{target.KindName}' cannot be member accessed", position);
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

                            throw new Error($"Type '{target.KindName}' cannot be member accessed", position);
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

        Error ThrowBinaryError(Value left, Value right, string op, Instruction instruction)
        {
            CallFrame current = _frames.Peek();
            Position position = current.GetPosition(instruction);
            return new Error($"Cannot apply '{op}' to {left.KindName} and {right.KindName}", position); 
        }

        void ValidateArguments(int arity, int got, ArgumentMode mode, string name, Position position)
        {
            if (mode == ArgumentMode.Expected)
                if (got != arity)
                    throw new Error($"Function '{name}' expected {arity}, got {got}", position);
        }
    }
}
