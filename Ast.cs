using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum
{
    internal abstract record Expr(Position Position);
    internal record NumberExpr(double Value, Position Position) : Expr(Position);
    internal record StringExpr(string Value, Position Position) : Expr(Position);
    internal record BoolExpr(bool Value, Position Position) : Expr(Position);
    internal record NameExpr(string Name, Position Position) : Expr(Position);
    internal record UnaryExpr(Expr Right, TokenType Op, Position Position) : Expr(Position);
    internal record CallExpr(Expr Callee, List<Expr> Arguments, Position Position) : Expr(Position);
    internal record ArrayExpr(List<Expr> Exprs, Position Position) : Expr(Position);
    internal record IndexExpr(Expr Target, Expr Index, Position Position) : Expr(Position);
    internal record BinaryExpr(Expr Left, Expr Right, TokenType Op, Position Position) : Expr(Position);

    internal abstract class Stmt
    {
        public Stmt(Position position, bool topLevel, bool allowedAtLocalScope)
        {
            Position = position;
            AllowedInImport = topLevel;
            AllowedAtLocalScope = allowedAtLocalScope;
        }

        public Position Position { get; }
        public bool AllowedInImport { get; }
        public bool AllowedAtLocalScope { get; }
    }

    internal class VarStmt : Stmt
    {
        public VarStmt(string name, Expr value, Position position) : base(position, false, true)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public Expr Value { get; }
    }

    internal class OutStmt : Stmt
    {
        public OutStmt(Expr value, Position position) : base(position, false, true)
        {
            Value = value;
        }

        public Expr Value { get; }
    }

    internal class RetStmt : Stmt
    {
        public RetStmt(Expr value, Expr? condition, Position position) : base(position, false, true)
        {
            Value = value;
            Condition = condition;
        }

        public Expr Value { get; }
        public Expr? Condition { get; }
    }

    internal class ProcStmt : Stmt
    {
        public ProcStmt(string name, List<string> paremeters, List<Stmt> body, Position position) : base(position, true, false)
        {
            Name = name;
            Paremeters = paremeters;
            Body = body;
        }

        public string Name { get; }
        public List<string> Paremeters { get; }
        public List<Stmt> Body { get; }
    }

    internal class CallStmt : Stmt
    {
        public CallStmt(CallExpr callExpr, Position position) : base(position, false, true)
        {
            CallExpr = callExpr;
        }

        public CallExpr CallExpr { get; }
    }

    internal class IfBranch
    {
        public IfBranch(Expr condition, List<Stmt> body)
        {
            Condition = condition;
            Body = body;
        }

        public Expr Condition { get; }
        public List<Stmt> Body { get; }
    }

    internal class IfStmt : Stmt
    {
        public IfStmt(List<IfBranch> branches, List<Stmt>? elseBody, Position position) : base(position, false, true)
        {
            Branches = branches;
            ElseBody = elseBody;
        }

        public List<IfBranch> Branches { get; }
        public List<Stmt>? ElseBody { get; }
    }

    internal class LeaveStmt : Stmt
    {
        public LeaveStmt(Expr? condition, Position position) : base(position, false, true)
        {
            Condition = condition;
        }

        public Expr? Condition { get; }
    }

    internal class BreakStmt : Stmt
    {
        public BreakStmt(Expr? condition, Position position) : base(position, false, true)
        {
            Condition = condition;
        }

        public Expr? Condition { get; }
    }

    internal class ContinueStmt : Stmt
    {
        public ContinueStmt(Expr? condition, Position position) : base(position, false, true)
        {
            Condition = condition;
        }

        public Expr? Condition { get; }
    }

    internal class ForStmt : Stmt
    {
        public ForStmt(Expr condition, List<Stmt> body, Position position) : base(position, false, true)
        {
            Condition = condition;
            Body = body;
        }

        public Expr Condition { get; }
        public List<Stmt> Body { get; }
    }

    internal class IndexSetStmt : Stmt
    {
        public IndexSetStmt(IndexExpr indexExpr, Expr value, Position position) : base(position, false, true)
        {
            IndexExpr = indexExpr;
            Value = value;
        }

        public IndexExpr IndexExpr { get; }
        public Expr Value { get; }
    }
}
