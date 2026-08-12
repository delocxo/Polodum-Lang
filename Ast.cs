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
    internal record BinaryExpr(Expr Left, Expr Right, TokenType Op, Position Position) : Expr(Position);

    internal abstract class Stmt
    {
        public Stmt(Position position, bool topLevel)
        {
            Position = position;
            AllowedInImport = topLevel;
        }

        public Position Position { get; }
        public bool AllowedInImport { get; }
    }

    internal class VarStmt : Stmt
    {
        public VarStmt(string name, Expr value, Position position) : base(position, false)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public Expr Value { get; }
    }
}
