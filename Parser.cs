using Polodum;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;

namespace Polodum
{
    internal class Parser
    {
        List<Token> _tokens;
        int _i = 0;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public List<Stmt> Parse()
        {
            List<Stmt> stmts = new List<Stmt>();

            while (NotAtEnd())
                stmts.Add(ParseStmt());

            return stmts;
        }

        Stmt ParseStmt()
        {
            if (Check(TokenType.Identifier))
                return ParseIdentifier();
            else if (Check(TokenType.Out))
                return ParseOut();
            else if (Check(TokenType.Ret))
                return ParseRet();
            else if (Check(TokenType.Proc))
                return ParseProc();
            else if (Check(TokenType.If))
                return ParseIf();
            else if (Check(TokenType.Leave))
                return ParseLeave();
            else if (Check(TokenType.Break))
                return ParseBreak();
            else if (Check(TokenType.Continue))
                return ParseContinue();
            else if (Check(TokenType.For))
                return ParseFor();
            throw ThrowUnexpected();
        }

        Stmt ParseIdentifier()
        {
            Position position = Current().Position;

            Expr assignee = ParsePostfix();

            if (assignee is CallExpr callExpr)
            {
                Expect(TokenType.Semicolon);
                return new CallStmt(callExpr, position);
            }

            Expect(TokenType.Equal);

            Expr expr = ParseExpr();

            Expect(TokenType.Semicolon);

            if (assignee is NameExpr nameExpr)
                return new VarStmt(nameExpr.Name, expr, position);

            throw new Error("Invalid assign target", position);
        }

        OutStmt ParseOut()
        {
            Position position = Current().Position;

            Next();

            Expr expr = ParseExpr();

            Expect(TokenType.Semicolon);

            return new OutStmt(expr, position);
        }

        RetStmt ParseRet()
        {
            Position position = Current().Position;

            Next();

            Expr expr = ParseExpr();
            Expr? condition = null;

            if (Match(TokenType.If))
                condition = ParseExpr();

            Expect(TokenType.Semicolon);

            return new RetStmt(expr, condition, position);
        }

        ProcStmt ParseProc()
        {
            Position position = Current().Position;

            Next();

            string name = ParseName();

            List<string> paremeters = ParseNames(TokenType.LeftParen, TokenType.RightParen);

            List<Stmt> body = ParseBody(false);

            return new ProcStmt(name, paremeters, body, position);
        }

        IfStmt ParseIf()
        {
            Position position = Current().Position;

            Next();

            List<IfBranch> branches = new List<IfBranch>();

            Expr condition = ParseExpr();

            Expect(TokenType.Do);

            List<Stmt> body = ParseIfBody();

            branches.Add(new IfBranch(condition, body));

            while (Match(TokenType.ElseIf))
            {
                Expr elseIfCondition = ParseExpr();

                Expect(TokenType.Do);

                List<Stmt> elseIfbody = ParseIfBody();

                branches.Add(new IfBranch(elseIfCondition, elseIfbody));
            }

            List<Stmt>? elseBody = null;

            if (Match(TokenType.Else))
                elseBody = ParseIfBody();

            Expect(TokenType.End);

            return new IfStmt(branches, elseBody, position);
        }

        List<Stmt> ParseIfBody()
        {
            List<Stmt> stmts = new List<Stmt>();

            while (NotAtEnd() && !Check(TokenType.ElseIf, TokenType.Else, TokenType.End))
                stmts.Add(ParseStmt());

            return stmts;
        }

        LeaveStmt ParseLeave()
        {
            Position position = Current().Position;

            Next();

            Expr? condition = null;

            if (Match(TokenType.If))
                condition = ParseExpr();

            Expect(TokenType.Semicolon);

            return new LeaveStmt(condition, position);
        }

        BreakStmt ParseBreak()
        {
            Position position = Current().Position;

            Next();

            Expr? condition = null;

            if (Match(TokenType.If))
                condition = ParseExpr();

            Expect(TokenType.Semicolon);

            return new BreakStmt(condition, position);
        }

        ContinueStmt ParseContinue()
        {
            Position position = Current().Position;

            Next();

            Expr? condition = null;

            if (Match(TokenType.If))
                condition = ParseExpr();

            Expect(TokenType.Semicolon);

            return new ContinueStmt(condition, position);
        }

        ForStmt ParseFor()
        {
            Position position = Current().Position;

            Next();

            if (Check(TokenType.Do))
            {
                Expr condition = new BoolExpr(false, position);
                List<Stmt> body = ParseBody(true);
                return new ForStmt(condition, body, position);
            }

            Expr otherCondition = ParseExpr();
            List<Stmt> otherBody = ParseBody(true);
            return new ForStmt(otherCondition, otherBody, position);
        }

        Error ThrowUnexpected()
        {
            Token token = Current();
            string? keyword = Lexer.GetKeywordFromType(token.TokenType);
            string? symbol = Lexer.GetSymbolFromType(token.TokenType);
            if (keyword != null)
                throw new Error($"Unexpected keyword '{keyword}'", token.Position);
            else if (symbol != null)
                throw new Error($"Unexpected symbol '{symbol}'", token.Position);
            else
                throw new Error($"Unexpected token '{token.TokenType}'", token.Position);
        }

        string ParseName()
        {
            string name = Current().Lexeme;
            Eat("Expected name", TokenType.Identifier);
            return name;
        }

        List<Stmt> ParseBody(bool usesDo)
        {
            List<Stmt> stmts = new List<Stmt>();

            if (usesDo)
                Expect(TokenType.Do);

            while (NotAtEnd() && !Check(TokenType.End))
                stmts.Add(ParseStmt());

            Expect(TokenType.End);

            return stmts;
        }

        List<string> ParseNames(TokenType end)
        {
            if (Match(end))
                return new List<string>();

            List<string> names = new List<string>()
            {
                ParseName()
            };

            while (Match(TokenType.Comma))
                names.Add(ParseName());

            Expect(end);

            return names;
        }

        List<string> ParseNames(TokenType start, TokenType end)
        {
            Expect(start);
            return ParseNames(end);
        }

        List<Expr> ParseArgs(TokenType start, TokenType end)
        {
            Expect(start);

            if (Match(end))
                return new List<Expr>();

            List<Expr> args = new List<Expr>()
            {
                ParseExpr()
            };

            while (Match(TokenType.Comma))
                args.Add(ParseExpr());

            Expect(end);

            return args;
        }

        bool Check(params TokenType[] types)
        {
            for (int i = 0; i < types.Length; i++)
                if (Current().TokenType == types[i])
                    return true;
            return false;
        }

        Token Current() => _tokens[_i];
        Token Peek() => _tokens[_i + 1];
        bool NotAtEnd() => !Check(TokenType.Eof);
        bool PeekNotAtEnd() => _i + 1 < _tokens.Count;
        bool AtEnd() => Check(TokenType.Eof);
        void Next() => _i++;

        void Eat(string message, params TokenType[] types)
        {
            if (Check(types))
            {
                Next();
                return;
            }
            throw new Error(message, Current().Position);
        }

        bool Match(params TokenType[] types)
        {
            if (Check(types))
            {
                Next();
                return true;
            }
            return false;
        }

        void Expect(TokenType type)
        {
            if (Check(type))
            {
                Next();
                return;
            }
            string? keyword = Lexer.GetKeywordFromType(type);
            string? symbol = Lexer.GetSymbolFromType(type);
            if (keyword != null)
                throw new Error($"Expected keyword '{keyword}'", Current().Position);
            else if (symbol != null)
                throw new Error($"Expected symbol '{symbol}'", Current().Position);
            else
                throw new Error($"Expected token '{type}'", Current().Position);
        }

        Expr ParsePrimary()
        {
            Token token = Current();

            if (Match(TokenType.Number))
                return new NumberExpr(double.Parse(token.Lexeme), token.Position);
            else if (Match(TokenType.String))
                return new StringExpr(token.Lexeme, token.Position);
            else if (Match(TokenType.True))
                return new BoolExpr(true, token.Position);
            else if (Match(TokenType.False))
                return new BoolExpr(false, token.Position);
            else if (Match(TokenType.Identifier))
                return new NameExpr(token.Lexeme, token.Position);
            else if (Match(TokenType.LeftParen))
            {
                Expr expr = ParseExpr();
                Expect(TokenType.RightParen);
                return expr;
            }

            throw new Error("Invalid expression", token.Position);
        }

        Expr ParsePostfix()
        {
            Expr left = ParsePrimary();
            while (Check(TokenType.LeftParen))
            {
                if (Check(TokenType.LeftParen))
                {
                    Position position = Current().Position;

                    List<Expr> args = ParseArgs(TokenType.LeftParen, TokenType.RightParen);

                    left = new CallExpr(left, args, position);

                    continue;
                }

                break;
            }
            return left;
        }

        Expr ParseUnary()
        {
            if (Check(TokenType.Sub, TokenType.Bang))
            {
                Token op = Current();

                Next();

                Expr right = ParseUnary();

                return new UnaryExpr(right, op.TokenType, op.Position);
            }
            return ParsePostfix();
        }

        Expr ParseTerm()
        {
            Expr left = ParseUnary();

            while (Check(TokenType.Mul, TokenType.Div, TokenType.Mod))
            {
                Token op = Current();

                Next();

                Expr right = ParseUnary();

                left = new BinaryExpr(left, right, op.TokenType, op.Position);
            }

            return left;
        }

        Expr ParseFactor()
        {
            Expr left = ParseTerm();

            while (Check(TokenType.Add, TokenType.Sub))
            {
                Token op = Current();

                Next();

                Expr right = ParseTerm();

                left = new BinaryExpr(left, right, op.TokenType, op.Position);
            }

            return left;
        }

        Expr ParseComparison()
        {
            Expr left = ParseFactor();

            while (Check(TokenType.Less, TokenType.Greater, TokenType.LessEq, TokenType.GreaterEq))
            {
                Token op = Current();

                Next();

                Expr right = ParseFactor();

                left = new BinaryExpr(left, right, op.TokenType, op.Position);
            }

            return left;
        }

        Expr ParseEquality()
        {
            Expr left = ParseComparison();

            while (Check(TokenType.NotEqual, TokenType.IsEqual))
            {
                Token op = Current();

                Next();

                Expr right = ParseComparison();

                left = new BinaryExpr(left, right, op.TokenType, op.Position);
            }

            return left;
        }

        Expr ParseAnd()
        {
            Expr left = ParseEquality();

            while (Check(TokenType.And))
            {
                Token op = Current();

                Next();

                Expr right = ParseEquality();

                left = new BinaryExpr(left, right, op.TokenType, op.Position);
            }

            return left;
        }

        Expr ParseOr()
        {
            Expr left = ParseAnd();

            while (Check(TokenType.Or))
            {
                Token op = Current();

                Next();

                Expr right = ParseAnd();

                left = new BinaryExpr(left, right, op.TokenType, op.Position);
            }

            return left;
        }

        Expr ParseExpr() => ParseOr();
    }
}
