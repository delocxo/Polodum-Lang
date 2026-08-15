using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum
{
    internal enum Opcode
    {
        LoadLocal,
        StoreLocal,
        LoadGlobal,
        StoreGlobal,
        LoadConst,
        MakeArray,
        MakeRecord,

        GetName,
        GetLength,
        CanIterateStoreLocal,

        Add,
        Sub,
        Mul,
        Div,
        Mod,
        Less,
        Greater,
        LessEq,
        GreaterEq,
        Equals,
        NotEqual,
        Not,
        Neg,
        Is,
        Isnt,

        JumpIfFalse,
        JumpIfTrue,
        JumpIfFalsePop,
        JumpIfTruePop,
        Jump,

        Call,
        Index,
        IndexSet,
        GetMember,
        MemberSet,

        Out,
        Pop,
        Ret,
        Halt
    }

    internal struct Instruction
    {
        public Instruction(Opcode opcode) : this()
        {
            Opcode = opcode;
        }

        public Instruction(Opcode opcode, int a) : this()
        {
            Opcode = opcode;
            A = a;
        }

        public Instruction(Opcode opcode, int a, int b) : this()
        {
            Opcode = opcode;
            A = a;
            B = b;
        }

        public Instruction(Opcode opcode, int a, int b, int c) : this()
        {
            Opcode = opcode;
            A = a;
            B = b;
            C = c;
        }

        public Instruction(Opcode opcode, int a, int b, int c, int d) : this()
        {
            Opcode = opcode;
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public Opcode Opcode { get; }
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int D { get; set; }
        public int PositionIndex { get; set; }
    }
}
