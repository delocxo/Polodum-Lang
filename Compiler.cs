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
        public List<int> Continue { get; } = new List<int>();
        public List<int> Breaks { get; } = new List<int>();
    }

    internal class ResolveResult
    {
        public ResolveResult(int slot, bool isGlobal)
        {
            Slot = slot;
            IsGlobal = isGlobal;
        }

        public int Slot { get; }
        public bool IsGlobal { get; }

        public Opcode StoreOpcode => IsGlobal ? Opcode.StoreGlobal : Opcode.StoreLocal;
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

                        var result = ResolveOrDeclareVariable(varStmt.Name, varStmt.Position);

                        Chunk.AddInstruction(new Instruction(result.StoreOpcode, result.Slot), varStmt.Position);

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

                        _forContexts.Push(new ForContext());

                        BeginScope();

                        CompileStmts(forStmt.Body);

                        EndScope();

                        Chunk.AddInstruction(new Instruction(Opcode.Jump, loopStart), forStmt.Position);

                        Chunk.PatchJump(jumpIfFalse);

                        var context = _forContexts.Pop();

                        foreach (int jump in context.Breaks)
                            Chunk.PatchJump(jump);

                        foreach (int jump in context.Continue)
                            Chunk.PatchJump(jump, loopStart);

                        break;
                    }

                case ForeachStmt foreachStmt:
                    {
                        // [1, 2, 3, 4, 5]
                        int collection = Chunk.MakeLocal();

                        CompileExpr(foreachStmt.Collection);

                        Chunk.AddInstruction(new Instruction(Opcode.CanIterateStoreLocal, collection), foreachStmt.Position);

                        int length = Chunk.MakeLocal();

                        // Loads [1, 2, 3, 4, 5]
                        Chunk.AddInstruction(new Instruction(Opcode.LoadLocal, collection), foreachStmt.Position);

                        // 5
                        Chunk.AddInstruction(new Instruction(Opcode.GetLength), foreachStmt.Position);

                        // Store 5
                        Chunk.AddInstruction(new Instruction(Opcode.StoreLocal, length), foreachStmt.Position);

                        // Load 0
                        Chunk.AddInstruction(new Instruction(Opcode.LoadConst, Chunk.AddConstant(new Value(0))), foreachStmt.Position);

                        int i = Chunk.MakeLocal();

                        // Store 0 in i
                        Chunk.AddInstruction(new Instruction(Opcode.StoreLocal, i), foreachStmt.Position);

                        int loopStart = Chunk.Instructions.Count;

                        // Check if i < 5
                        Chunk.AddInstruction(new Instruction(Opcode.LoadLocal, i), foreachStmt.Position);
                        Chunk.AddInstruction(new Instruction(Opcode.LoadLocal, length), foreachStmt.Position);
                        Chunk.AddInstruction(new Instruction(Opcode.Less), foreachStmt.Position);

                        int jumpIfFalse = Chunk.AddInstruction(new Instruction(Opcode.JumpIfFalsePop), foreachStmt.Position);

                        _forContexts.Push(new ForContext());

                        BeginScope();

                        // Store the name item
                        //CompileExpr(indexExpr.Index);
                        //CompileExpr(indexExpr.Target);
                        //Chunk.AddInstruction(new Instruction(Opcode.Index), indexExpr.Position);



                        // Load i
                        Chunk.AddInstruction(new Instruction(Opcode.LoadLocal, i), foreachStmt.Position);
                        // Load the array
                        Chunk.AddInstruction(new Instruction(Opcode.LoadLocal, collection), foreachStmt.Position);
                        Chunk.AddInstruction(new Instruction(Opcode.Index), foreachStmt.Position);
                        int nextSlot = Chunk.LocalCount++;
                        Scopes.Peek().Add(foreachStmt.Name, nextSlot);
                        // Store array[i]
                        Chunk.AddInstruction(new Instruction(Opcode.StoreLocal, nextSlot), foreachStmt.Position);

                        CompileStmts(foreachStmt.Body);

                        int continueStart = Chunk.Instructions.Count;

                        // i++
                        Chunk.AddInstruction(new Instruction(Opcode.LoadLocal, i), foreachStmt.Position);
                        Chunk.AddInstruction(new Instruction(Opcode.LoadConst, Chunk.AddConstant(new Value(1))), foreachStmt.Position);
                        Chunk.AddInstruction(new Instruction(Opcode.Add), foreachStmt.Position);
                        Chunk.AddInstruction(new Instruction(Opcode.StoreLocal, i), foreachStmt.Position);

                        EndScope();

                        Chunk.AddInstruction(new Instruction(Opcode.Jump, loopStart), foreachStmt.Position);

                        Chunk.PatchJump(jumpIfFalse);

                        var context = _forContexts.Pop();

                        foreach (int jump in context.Breaks)
                            Chunk.PatchJump(jump);

                        foreach (int jump in context.Continue)
                            Chunk.PatchJump(jump, continueStart);
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

                            context.Continue.Add(Chunk.AddInstruction(new Instruction(Opcode.Jump), continueStmt.Position));

                            Chunk.PatchJump(jumpIfFalse);

                            break;
                        }

                        context.Continue.Add(Chunk.AddInstruction(new Instruction(Opcode.Jump), continueStmt.Position));

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

                case UnpackedVarStmt unpackedVarStmt:
                    {
                        List<int> slots = new List<int>();

                        CheckForDuplicateNames(unpackedVarStmt.UnpackedVariables.Select(x => x.Name).ToList(), " is a duplicated unpacked variable", unpackedVarStmt.Position);

                        foreach (var unpackedVar in unpackedVarStmt.UnpackedVariables)
                        {
                            if (unpackedVar.IsDiscard)
                                slots.Add(-1);
                            else
                                slots.Add(ResolveOrDeclareVariable(unpackedVar.Name, unpackedVarStmt.Position).Slot);
                        }

                        CompileExpr(unpackedVarStmt.Expr);

                        Chunk.AddInstruction(new Instruction(Opcode.UnpackStoreLocals, IsGlobal ? 1 : 0, slots.ToArray()), unpackedVarStmt.Position);
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
                        else if (Vm.Globals.TryGetValue(nameExpr.Name, out Value value))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.LoadConst, Chunk.AddConstant(value)), nameExpr.Position);
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
                            TokenType.Mul => Opcode.Unpack,
                            _ => throw new UnreachableException()
                        };
                        Chunk.AddInstruction(new Instruction(op), unaryExpr.Position);
                        break;
                    }

                case CallExpr callExpr:
                    {
                        if (callExpr.Callee is NameExpr nameExpr &&
                            callExpr.Arguments.Count == 0 &&
                            nameExpr.Name == "none" &&
                            IsBuiltinName("none"))
                        {
                            int constant = Chunk.AddConstant(Vm.MakeNone());
                            Chunk.AddInstruction(new Instruction(Opcode.LoadConst, constant), callExpr.Position);
                            break;
                        }
                        callExpr.Arguments.ForEach(CompileExpr);
                        CompileExpr(callExpr.Callee);
                        Chunk.AddInstruction(new Instruction(Opcode.Call, callExpr.Arguments.Count), callExpr.Position);
                        break;
                    }

                case ArrayExpr arrayExpr:
                    {
                        arrayExpr.Exprs.Reverse();
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

                        Record record = new Record(recordExpr.Fields
                            .Select(x => new RecordField(x.Name, x.Mutable, Value.False))
                            .ToDictionary(x => x.Name, x => x),
                            ValueKind.Register(recordExpr.Name));

                        MatchRecord(recordExpr.Name, record, recordExpr.Position);
                        recordExpr.Fields.Reverse();

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
                        if (TryResolveGetMember(memberExpr, out Value value))
                        {
                            Chunk.AddInstruction(new Instruction(Opcode.LoadConst, Chunk.AddConstant(value)), memberExpr.Position);
                            break;
                        }
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

        ResolveResult ResolveOrDeclareVariable(string name, Position position)
        {
            if (Functions.ContainsKey(name))
                throw new Error($"Variable '{name}' is an already existing function", position);

            Scope scope = Scopes.Peek();

            if (TryResolveLocal(name, out int local))
            {
                return new ResolveResult(local, false);
            }
            else if (Globals.TryGetValue(name, out int global))
            {
                return new ResolveResult(global, true);
            }

            if (IsGlobal)
            {
                int nextGlobalSlot = Chunk.MakeGlobal();
                Globals.Add(name, nextGlobalSlot);
                return new ResolveResult(nextGlobalSlot, true);
            }
            else
            {
                int nextLocalSlot = Chunk.MakeLocal();
                scope.Add(name, nextLocalSlot);
                return new ResolveResult(nextLocalSlot, false);
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

        bool TryResolveGetMember(MemberExpr memberExpr, out Value value)
        {
            if (memberExpr.Target is NameExpr nameExpr && Vm.Globals.TryGetValue(nameExpr.Name, out value) && IsBuiltinName(nameExpr.Name))
            {
                if (value.IsKind(ValueKind.Namespace))
                {
                    value = value.Namespace.Get(memberExpr.MemberName, memberExpr.Position);
                    return true;
                }
            }
            else if (memberExpr.Target is MemberExpr memberExpr2 && TryResolveGetMember(memberExpr2, out value))
            {
                if (value.IsKind(ValueKind.Namespace))
                {
                    value = value.Namespace.Get(memberExpr.MemberName, memberExpr.Position);
                    return true;
                }
            }

            value = default;
            return false;
        }

        void MatchRecord(string name, Record record, Position position)
        {
            if (Vm.ExistingRecords.TryGetValue(name, out Record? other))
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
            Vm.ExistingRecords.Add(name, record);
        }

        bool IsBuiltinName(string name) => 
            !Functions.ContainsKey(name) 
            && !TryResolveLocal(name, out _) 
            && !TryResolveGlobal(name, out _);
    }
}
