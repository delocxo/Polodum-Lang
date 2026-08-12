using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Polodum
{
    using Scope = Dictionary<string, int>;

    internal class Compiler
    {
        public Chunk Chunk { get; } = new Chunk();
        public Stack<Scope> Scopes { get; } = new Stack<Scope>();

        bool _isGlobal;
        Dictionary<string, bool> _compiledFiles = new Dictionary<string, bool>();
        Scope _globals = new Scope();

        bool IsGlobal => _isGlobal || Scopes.Count == 0;

        public Compiler(bool isGlobal)
        {
            Scopes.Push(new Scope());
            _isGlobal = isGlobal;
        }

        public void CompileFile(string path, Position position, bool isEntry)
        {
            if (!File.Exists(path))
            {
                if (isEntry)
                {
                    Console.Error.WriteLine($"File '{path}' does not exist");
                    Environment.Exit(1);
                }
                else
                    throw new Error($"File '{path}' does not exist", position);
            }

            if (_compiledFiles.TryGetValue(path, out bool finished))
            {
                if (!finished)
                    throw new Error($"Circular import detected: '{path}'", position);
                return;
            }

            _compiledFiles[path] = false;

            string source = File.ReadAllText(path);
            Lexer lexer = new Lexer(source, path);
            List<Token> tokens = lexer.Lex();
            Parser parser = new Parser(tokens);
            List<Stmt> ast = parser.Parse();
            foreach (Stmt stmt in ast)
            {
                if (!isEntry && !stmt.AllowedInImport)
                    throw new Error("Statement cannot be used at the top level", stmt.Position);
            }
            foreach (Stmt stmt in ast)
                CompileStmt(stmt);

            _compiledFiles[path] = true;
        }

        void CompileStmt(Stmt stmt)
        {
            switch (stmt)
            {
                case VarStmt varStmt:
                    {
                        CompileExpr(varStmt.Value);

                        Scope scope = Scopes.Peek();

                        if (scope.TryGetValue(varStmt.Name, out int local))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.StoreLocal, local), varStmt.Position);
                            break;
                        }
                        else if (_globals.TryGetValue(varStmt.Name, out int global))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.StoreGlobal, global), varStmt.Position);
                            break;
                        }

                        if (IsGlobal)
                        {
                            int nextSlot = Chunk.GlobalCount++;
                            _globals.Add(varStmt.Name, nextSlot);
                            Chunk.AddInstruction(new Instruction(Opcode.StoreGlobal, nextSlot), varStmt.Position);
                        }
                        else
                        {
                            int nextSlot = Chunk.LocalCount++;
                            scope.Add(varStmt.Name, nextSlot);
                            Chunk.AddInstruction(new Instruction(Opcode.StoreLocal, nextSlot), varStmt.Position);
                        }

                        break;
                    }

                case OutStmt outStmt:
                    {
                        CompileExpr(outStmt.Value);
                        Chunk.AddInstruction(new Instruction(Opcode.Out), outStmt.Position);
                        break;
                    }
            }
        }

        void CompileExpr(Expr expr)
        {
            switch (expr)
            {
                case NumberExpr numberExpr:
                    {
                        int constant = Chunk.AddConstant(new Value(numberExpr.Value));
                        Chunk.AddInstruction(new Instruction(Opcode.LoadConst, constant), numberExpr.Position);
                        break;
                    }

                case StringExpr stringExpr:
                    {
                        int constant = Chunk.AddConstant(new Value(stringExpr.Value));
                        Chunk.AddInstruction(new Instruction(Opcode.LoadConst, constant), stringExpr.Position);
                        break;
                    }

                case BoolExpr boolExpr:
                    {
                        int constant = Chunk.AddConstant(new Value(boolExpr.Value));
                        Chunk.AddInstruction(new Instruction(Opcode.LoadConst, constant), boolExpr.Position);
                        break;
                    }

                case NameExpr nameExpr:
                    {
                        if (TryResolveLocal(nameExpr.Name, out int local))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.LoadLocal, local), nameExpr.Position);
                        }
                        else if (TryResolveGlobal(nameExpr.Name, out int global))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.LoadGlobal, global), nameExpr.Position);
                        }
                        break;
                    }

                case UnaryExpr unaryExpr:
                    {
                        CompileExpr(unaryExpr.Right);
                        Opcode op = unaryExpr.Op switch
                        {
                            TokenType.Sub => Opcode.Neg,
                            TokenType.Not => Opcode.Not,
                            _ => throw new UnreachableException()
                        };
                        Chunk.AddInstruction(new Instruction(op), unaryExpr.Position);
                        break;
                    }

                case BinaryExpr binaryExpr:
                    {
                        if (binaryExpr.Op == TokenType.And)
                        {
                            CompileExpr(binaryExpr.Left);

                            int endJump = Chunk.AddInstruction(new Instruction(Opcode.JumpIfFalse), binaryExpr.Position);

                            Chunk.AddInstruction(new Instruction(Opcode.Pop), binaryExpr.Position);

                            CompileExpr(binaryExpr.Right);

                            Chunk.PatchJump(endJump);
                            break;
                        }
                        else if (binaryExpr.Op == TokenType.Or)
                        {
                            CompileExpr(binaryExpr.Left);

                            int endJump = Chunk.AddInstruction(new Instruction(Opcode.JumpIfTrue), binaryExpr.Position);

                            Chunk.AddInstruction(new Instruction(Opcode.Pop), binaryExpr.Position);

                            CompileExpr(binaryExpr.Right);

                            Chunk.PatchJump(endJump);
                            break;
                        }

                        CompileExpr(binaryExpr.Left);
                        CompileExpr(binaryExpr.Right);

                        Opcode op = binaryExpr.Op switch
                        {
                            TokenType.Add => Opcode.Add,
                            TokenType.Sub => Opcode.Sub,
                            TokenType.Mul => Opcode.Mul,
                            TokenType.Div => Opcode.Div,
                            TokenType.Mod => Opcode.Mod,
                            TokenType.Less => Opcode.Less,
                            TokenType.Greater => Opcode.Greater,
                            TokenType.LessEq => Opcode.LessEq,
                            TokenType.GreaterEq => Opcode.GreaterEq,
                            TokenType.IsEqual => Opcode.Equals,
                            TokenType.NotEqual => Opcode.NotEqual,
                            _ => throw new UnreachableException()
                        };

                        Chunk.AddInstruction(new Instruction(op), binaryExpr.Position);

                        break;
                    }
            }
        }

        void BeginScope()
        {
            Scopes.Push(new Scope());
        }

        void EndScope()
        {
            Scopes.Pop();
        }

        bool TryResolveLocal(string name, out int local)
        {
            foreach (Scope scope in Scopes)
                if (scope.TryGetValue(name, out local))
                    return true;
            local = 0;
            return false;
        }

        bool TryResolveGlobal(string name, out int local)
        {
            return _globals.TryGetValue(name, out local);
        }

        public void AddHalt() => Chunk.Instructions.Add(new Instruction(Opcode.Halt));
    }
}
