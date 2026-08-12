using System;
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
    }

    internal class Vm
    {
        Stack<Value> _stack = new Stack<Value>();
        Stack<CallFrame> _frames = new Stack<CallFrame>();
        Value[] _globals;

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

                switch (instruction.Opcode)
                {
                    case Opcode.LoadLocal:
                        _stack.Push(callFrame.Locals[instruction.A]);
                        break;

                    case Opcode.StoreLocal:
                        callFrame.Locals[instruction.A] = _stack.Pop();
                        break;

                    case Opcode.LoadGlobal:
                        _stack.Push(_globals[instruction.A]);
                        break;

                    case Opcode.StoreGlobal:
                        _globals[instruction.A] = _stack.Pop();
                        break;

                    case Opcode.LoadConst:
                        _stack.Push(callFrame.GetConstant(instruction.A));
                        break;

                    case Opcode.Add:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number + right.Number));
                                break;
                            }
                            else if (left.IsKind(ValueKind.String))
                            {
                                _stack.Push(new Value(left.String + right.ToString()));
                                break;
                            }
                            else if (right.IsKind(ValueKind.String))
                            {
                                _stack.Push(new Value(left.ToString() + right.String));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "+", instruction);
                        }

                    case Opcode.Sub:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number - right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "-", instruction);
                        }

                    case Opcode.Mul:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number * right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "*", instruction);
                        }

                    case Opcode.Div:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number / right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "/", instruction);
                        }

                    case Opcode.Mod:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number % right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "%", instruction);
                        }

                    case Opcode.Less:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number < right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "<", instruction);
                        }

                    case Opcode.Greater:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number > right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, ">", instruction);
                        }

                    case Opcode.LessEq:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number <= right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, "<=", instruction);
                        }

                    case Opcode.GreaterEq:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(left.Number >= right.Number));
                                break;
                            }
                            throw ThrowBinaryError(left, right, ">=", instruction);
                        }

                    case Opcode.Equals:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            _stack.Push(new Value(Value.CheckEquallity(left, right)));

                            break;
                        }

                    case Opcode.NotEqual:
                        {
                            Value right = _stack.Pop();
                            Value left = _stack.Pop();

                            _stack.Push(new Value(!Value.CheckEquallity(left, right)));

                            break;
                        }

                    case Opcode.Not:
                        {
                            Value right = _stack.Pop();

                            if (right.IsKind(ValueKind.Bool))
                            {
                                _stack.Push(new Value(!right.Bool));
                                break;
                            }

                            throw new Error($"Cannot apply flip to {right.KindName}", callFrame.GetPosition(instruction));
                        }

                    case Opcode.Neg:
                        {
                            Value right = _stack.Pop();

                            if (right.IsKind(ValueKind.Number))
                            {
                                _stack.Push(new Value(-right.Number));
                                break;
                            }

                            throw new Error($"Cannot apply negate to {right.KindName}", callFrame.GetPosition(instruction));
                        }

                    case Opcode.JumpIfFalse:
                        {
                            Value condition = _stack.Peek();
                            if (!Value.IsTruthy(condition))
                                callFrame.Ip = instruction.A;
                            break;
                        }

                    case Opcode.JumpIfTrue:
                        {
                            Value condition = _stack.Peek();
                            if (Value.IsTruthy(condition))
                                callFrame.Ip = instruction.A;
                            break;
                        }

                    case Opcode.Jump:
                        {
                            callFrame.Ip = instruction.A;
                            break;
                        }

                    case Opcode.Pop:
                        {
                            _stack.Pop();
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
    }
}
