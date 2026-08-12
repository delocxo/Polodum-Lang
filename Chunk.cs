using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum
{
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

        public void PatchJump(int index)
        {
            Instruction instruction = Instructions[index];
            instruction.A = Instructions.Count;
        }
    }
}
