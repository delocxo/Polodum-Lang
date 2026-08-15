using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum
{
    internal class FunctionInfo
    {
        public FunctionInfo(string name, List<string> parameters)
        {
            Name = name;
            Arity = parameters.Count;
            Chunk = new Chunk();
            Parameters = parameters;
        }

        public string Name { get; }
        public int Arity { get; }
        public Chunk Chunk { get; set; }
        public List<string> Parameters { get; }
    }

    internal class Chunk
    {
        public List<Position> Positions { get; } = new List<Position>();
        public List<Value> Constants { get; } = new List<Value>();
        public List<Instruction> Instructions { get; } = new List<Instruction>();
        public int LocalCount { get; set; }
        public int GlobalCount { get; set; }

        public int AddPosition(Position position)
        {
            int index = Positions.Count;
            Positions.Add(position);
            return index;
        }

        public int AddConstant(Value value)
        {
            for (int i = 0; i < Constants.Count; i++)
                if (Value.CheckEquallity(value, Constants[i]))
                    return i;
            
            int index = Constants.Count;
            Constants.Add(value);
            return index;
        }

        public int AddInstruction(Instruction instruction, Position position)
        {
            int index = Instructions.Count;
            int positionIndex = AddPosition(position);
            instruction.PositionIndex = positionIndex;
            Instructions.Add(instruction);
            return index;
        }

        public void PatchJump(int index, int target)
        {
            Instruction instruction = Instructions[index];
            instruction.A = target;
            Instructions[index] = instruction;
        }

        public void PatchJump(int index)
        {
            PatchJump(index, Instructions.Count);
        }

        public int MakeSyntheticLocal() => LocalCount++;

        public void Print()
        {
            for (int i = 0; i < Constants.Count; i++)
            {
                Value constant = Constants[i];
                if (constant.Kind == ValueKind.Function)
                {
                    Console.WriteLine("FUNCTION START");
                    Console.WriteLine($"{constant.ToString()}");
                    constant.FunctionInfo.Chunk.Print();
                    Console.WriteLine("FUNCTION END");
                }
            }
            for (int i = 0; i < Instructions.Count; i++)
            {
                Instruction instruction = Instructions[i];
                Console.WriteLine($"Instruction {i}. {instruction.Opcode} {instruction.A} {instruction.B} {instruction.C} {instruction.D}");
            }
            for (int i = 0; i < Constants.Count; i++)
            {
                Console.WriteLine($"Constant {i}. {Constants[i].Stringify()}");
            }
        }
    }
}
