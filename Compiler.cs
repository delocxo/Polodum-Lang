using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Polodum
{
    using Scope = Dictionary<string, int>;

    internal class IfContext
    {
        public List<int> Leaves { get; } = new List<int>(); 
    }

    internal class ForContext
    {
        public ForContext(int @continue)
        {
            Continue = @continue;
        }

        public int Continue { get; }
        public List<int> Breaks { get; } = new List<int>();
    }

    internal class Compiler
    {
        public Chunk Chunk { get; set; } = new Chunk();
        public Stack<Scope> Scopes { get; } = new Stack<Scope>();
        public Scope Globals { get; set; } = new Scope();
        public Dictionary<string, FunctionInfo> Functions { get; set; } = new Dictionary<string, FunctionInfo>();

        bool _isGlobal;
        Dictionary<string, bool> _compiledFiles = new Dictionary<string, bool>();
        Stack<IfContext> _ifContexts = new Stack<IfContext>();
        Stack<ForContext> _forContexts = new Stack<ForContext>();

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

            path = Path.GetFullPath(path);

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
                    throw new Error($"{stmt.GetType().Name} statement cannot be used at the top level", stmt.Position);

                if (stmt is ProcStmt procStmt)
                {
                    if (Functions.ContainsKey(procStmt.Name))
                        throw new Error($"Function '{procStmt.Name}' is already an existing function", procStmt.Position);

                    CheckForDuplicateNames(procStmt.Paremeters, " is a duplicate function parameter", procStmt.Position);

                    if (procStmt.Body.Count == 0 || procStmt.Body.Last() is not RetStmt retStmt || retStmt.Condition != null)
                        throw new Error($"Function '{procStmt.Name}' requires an explicit ret", procStmt.Position);

                    Functions.Add(procStmt.Name, new FunctionInfo(procStmt.Name, procStmt.Paremeters));
                }
            }
            foreach (Stmt stmt in ast)
                CompileStmt(stmt);

            _compiledFiles[path] = true;
        }

        public void CompileStmts(List<Stmt> ast)
        {
            foreach (Stmt stmt in ast)
                CompileStmt(stmt);
        }

        void CompileStmt(Stmt stmt)
        {
            if (!IsGlobal && !stmt.AllowedAtLocalScope)
                throw new Error($"{stmt.GetType().Name} statement cannot be used in a local scope", stmt.Position);

            switch (stmt)
            {
                case ProcStmt procStmt:
                    {
                        Compiler compiler = new Compiler(false)
                        {
                            Globals = Globals,
                            Functions = Functions
                        };

                        foreach (string parameter in procStmt.Paremeters)
                            compiler.Scopes
                                .Peek()
                                .Add(parameter, compiler.Chunk.LocalCount++);

                        compiler.CompileStmts(procStmt.Body);

                        FunctionInfo functionInfo = Functions[procStmt.Name];
                        functionInfo.Chunk = compiler.Chunk;

                        break;
                    }

                case VarStmt varStmt:
                    {
                        CompileExpr(varStmt.Value);

                        Scope scope = Scopes.Peek();

                        if (TryResolveLocal(varStmt.Name, out int local))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.StoreLocal, local), varStmt.Position);
                            break;
                        }
                        else if (Globals.TryGetValue(varStmt.Name, out int global))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.StoreGlobal, global), varStmt.Position);
                            break;
                        }

                        if (IsGlobal)
                        {
                            int nextSlot = Chunk.GlobalCount++;
                            Globals.Add(varStmt.Name, nextSlot);
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

                case RetStmt retStmt:
                    {
                        if (retStmt.Condition != null)
                        {
                            CompileExpr(retStmt.Condition);

                            int jumpIfFalse = Chunk.AddInstruction(new Instruction(Opcode.JumpIfFalsePop), retStmt.Position);

                            CompileExpr(retStmt.Value);

                            Chunk.AddInstruction(new Instruction(Opcode.Ret), retStmt.Position);

                            Chunk.PatchJump(jumpIfFalse);

                            break;
                        }

                        CompileExpr(retStmt.Value);
                        Chunk.AddInstruction(new Instruction(Opcode.Ret), retStmt.Position);

                        break;
                    }

                case CallStmt callStmt:
                    {
                        CompileExpr(callStmt.CallExpr);
                        Chunk.AddInstruction(new Instruction(Opcode.Pop), callStmt.Position);
                        break;
                    }

                case IfStmt ifStmt:
                    {
                        List<int> endJumps = new List<int>();

                        foreach (var branch in ifStmt.Branches)
                        {
                            CompileExpr(branch.Condition);

                            int falseJump = Chunk.AddInstruction(new Instruction(Opcode.JumpIfFalsePop), ifStmt.Position);

                            _ifContexts.Push(new IfContext());

                            BeginScope();

                            CompileStmts(branch.Body);

                            EndScope();

                            endJumps.AddRange(_ifContexts.Pop().Leaves);

                            int endJump = Chunk.AddInstruction(new Instruction(Opcode.Jump), ifStmt.Position);

                            endJumps.Add(endJump);

                            Chunk.PatchJump(falseJump);
                        }

                        if (ifStmt.ElseBody != null)
                        {
                            _ifContexts.Push(new IfContext());

                            BeginScope();

                            CompileStmts(ifStmt.ElseBody);

                            EndScope();

                            endJumps.AddRange(_ifContexts.Pop().Leaves);
                        }

                        foreach (int jump in endJumps)
                            Chunk.PatchJump(jump);

                        break;
                    }

                case LeaveStmt leaveStmt:
                    {
                        if (_ifContexts.Count == 0)
                            throw new Error("Cannot use leave outside a if-else-elseif statement", leaveStmt.Position);

                        var context = _ifContexts.Peek();

                        if (leaveStmt.Condition != null)
                        {
                            CompileExpr(leaveStmt.Condition);

                            int jumpIfFalse = Chunk.AddInstruction(new Instruction(Opcode.JumpIfFalsePop), leaveStmt.Position);

                            context.Leaves.Add(Chunk.AddInstruction(new Instruction(Opcode.Jump), leaveStmt.Position));

                            Chunk.PatchJump(jumpIfFalse);

                            break;
                        }

                        context.Leaves.Add(Chunk.AddInstruction(new Instruction(Opcode.Jump), leaveStmt.Position));

                        break;
                    }

                case ForStmt forStmt:
                    {
                        int loopStart = Chunk.Instructions.Count;

                        CompileExpr(forStmt.Condition);

                        int jumpIfFalse = Chunk.AddInstruction(new Instruction(Opcode.JumpIfFalsePop), forStmt.Position);

                        _forContexts.Push(new ForContext(loopStart));

                        BeginScope();

                        CompileStmts(forStmt.Body);

                        EndScope();

                        Chunk.AddInstruction(new Instruction(Opcode.Jump, loopStart), forStmt.Position);

                        Chunk.PatchJump(jumpIfFalse);

                        var context = _forContexts.Pop();

                        foreach (int jump in context.Breaks)
                            Chunk.PatchJump(jump);

                        break;
                    }

                case BreakStmt breakStmt:
                    {
                        if (_forContexts.Count == 0)
                            throw new Error("Cannot use break outside a for loop", breakStmt.Position);

                        var context = _forContexts.Peek();

                        if (breakStmt.Condition != null)
                        {
                            CompileExpr(breakStmt.Condition);

                            int jumpIfFalse = Chunk.AddInstruction(new Instruction(Opcode.JumpIfFalsePop), breakStmt.Position);

                            context.Breaks.Add(Chunk.AddInstruction(new Instruction(Opcode.Jump), breakStmt.Position));

                            Chunk.PatchJump(jumpIfFalse);

                            break;
                        }

                        context.Breaks.Add(Chunk.AddInstruction(new Instruction(Opcode.Jump), breakStmt.Position));

                        break;
                    }

                case ContinueStmt continueStmt:
                    {
                        if (_forContexts.Count == 0)
                            throw new Error("Cannot use continue outside a for loop", continueStmt.Position);

                        var context = _forContexts.Peek();

                        if (continueStmt.Condition != null)
                        {
                            CompileExpr(continueStmt.Condition);

                            int jumpIfFalse = Chunk.AddInstruction(new Instruction(Opcode.JumpIfFalsePop), continueStmt.Position);

                            Chunk.AddInstruction(new Instruction(Opcode.Jump, context.Continue), continueStmt.Position);

                            Chunk.PatchJump(jumpIfFalse);

                            break;
                        }

                        Chunk.AddInstruction(new Instruction(Opcode.Jump, context.Continue), continueStmt.Position);

                        break;
                    }

                case IndexSetStmt indexSetStmt:
                    {
                        IndexExpr indexExpr = indexSetStmt.IndexExpr;
                        CompileExpr(indexSetStmt.Value);
                        CompileExpr(indexExpr.Index);
                        CompileExpr(indexExpr.Target);
                        Chunk.AddInstruction(new Instruction(Opcode.IndexSet), indexExpr.Position);
                        break;
                    }

                case MemberSetStmt memberSetStmt:
                    {
                        MemberExpr memberExpr = memberSetStmt.MemberExpr;
                        CompileExpr(memberSetStmt.Value);
                        CompileExpr(memberExpr.Target);
                        int memberConstant = Chunk.AddConstant(new Value(memberExpr.MemberName));
                        Chunk.AddInstruction(new Instruction(Opcode.MemberSet, memberConstant), memberExpr.Position);
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
                        if (Functions.TryGetValue(nameExpr.Name, out FunctionInfo? functionInfo))
                        {
                            int constant = Chunk.AddConstant(new Value(functionInfo));
                            Chunk.AddInstruction(new Instruction(Opcode.LoadConst, constant), nameExpr.Position);
                        }
                        else if (TryResolveLocal(nameExpr.Name, out int local))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.LoadLocal, local), nameExpr.Position);
                        }
                        else if (TryResolveGlobal(nameExpr.Name, out int global))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.LoadGlobal, global), nameExpr.Position);
                        }
                        else
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.GetName, Chunk.AddConstant(new Value(nameExpr.Name))), nameExpr.Position);
                        }

                        break;
                    }

                case UnaryExpr unaryExpr:
                    {
                        CompileExpr(unaryExpr.Right);
                        Opcode op = unaryExpr.Op switch
                        {
                            TokenType.Sub => Opcode.Neg,
                            TokenType.Bang => Opcode.Not,
                            _ => throw new UnreachableException()
                        };
                        Chunk.AddInstruction(new Instruction(op), unaryExpr.Position);
                        break;
                    }

                case CallExpr callExpr:
                    {
                        callExpr.Arguments.ForEach(CompileExpr);
                        CompileExpr(callExpr.Callee);
                        Chunk.AddInstruction(new Instruction(Opcode.Call, callExpr.Arguments.Count), callExpr.Position);
                        break;
                    }

                case ArrayExpr arrayExpr:
                    {
                        arrayExpr.Exprs.ForEach(CompileExpr);
                        Chunk.AddInstruction(new Instruction(Opcode.MakeArray, arrayExpr.Exprs.Count), arrayExpr.Position);
                        break;
                    }

                case IndexExpr indexExpr:
                    {
                        CompileExpr(indexExpr.Index);
                        CompileExpr(indexExpr.Target);
                        Chunk.AddInstruction(new Instruction(Opcode.Index), indexExpr.Position);
                        break;
                    }

                case RecordExpr recordExpr:
                    {
                        CheckForDuplicateNames(recordExpr.Fields.Select(x => x.Name).ToList(), $"is a duplicate field inside record '{recordExpr.Name}'", recordExpr.Position);
                        foreach (var field in recordExpr.Fields)
                        {
                            CompileExpr(field.Expr);
                            Chunk.AddInstruction(new Instruction(Opcode.LoadConst, Chunk.AddConstant(new Value(field.Mutable))), recordExpr.Position);
                            Chunk.AddInstruction(new Instruction(Opcode.LoadConst, Chunk.AddConstant(new Value(field.Name))), recordExpr.Position);
                        }
                        int nameConstant = Chunk.AddConstant(new Value(recordExpr.Name));
                        Chunk.AddInstruction(new Instruction(Opcode.MakeRecord, nameConstant, recordExpr.Fields.Count), recordExpr.Position);
                        break;
                    }

                case MemberExpr memberExpr:
                    {
                        CompileExpr(memberExpr.Target);
                        int memberConstant = Chunk.AddConstant(new Value(memberExpr.MemberName));
                        Chunk.AddInstruction(new Instruction(Opcode.GetMember, memberConstant), memberExpr.Position);
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
                        else if (binaryExpr.Op is TokenType.Is or TokenType.Isnt)
                        {
                            if (binaryExpr.Right is not NameExpr nameExpr)
                                throw new Error($"Expected type name afer '{(binaryExpr.Op == TokenType.Is ? "is" : "isnt")}'", binaryExpr.Position);

                            int constant = Chunk.AddConstant(new Value(nameExpr.Name));

                            CompileExpr(binaryExpr.Left);

                            Chunk.AddInstruction(new Instruction(binaryExpr.Op == TokenType.Is ? Opcode.Is : Opcode.Isnt, constant), binaryExpr.Position);

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
            return Globals.TryGetValue(name, out local);
        }

        void CheckForDuplicateNames(List<string> names, string message, Position position)
        {
            HashSet<string> namesSet = new HashSet<string>();
            foreach (string name in names)
                if (!namesSet.Add(name))
                    throw new Error($"'{name}' {message}", position); 
        }

        public void AddHalt() => Chunk.Instructions.Add(new Instruction(Opcode.Halt));

    }
}
