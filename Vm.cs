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

                            throw new Error($"type '{target.KindName}' is not callable", callFrame.GetPosition(instruction));
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
