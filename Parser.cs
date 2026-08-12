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
            throw ThrowUnexpected();
        }

        Stmt ParseIdentifier()
        {
            Position position = Current().Position;

            Expr assignee = ParsePostfix();

            Expect(TokenType.Equal);

            Expr expr = ParseExpr();

            Expect(TokenType.Semicolon);

            if (assignee is NameExpr nameExpr)
                return new VarStmt(nameExpr.Name, expr, position);

            throw new Error("Invalid assign target", position);
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

        List<Stmt> ParseBody()
        {
            List<Stmt> stmts = new List<Stmt>();

            if (!Check(TokenType.LeftBrace))
            {
                stmts.Add(ParseStmt());
                return stmts;
            }

            Expect(TokenType.LeftBrace);

            while (NotAtEnd() && !Check(TokenType.RightBrace))
                stmts.Add(ParseStmt());

            Expect(TokenType.RightBrace);

            return stmts;
        }

        bool Check(params TokenType[] types)
        {
            for (int i = 0; i < types.Length; i++)
                if (Current().TokenType == types[i])
                    return true;
            return false;
        }

        Token Current() => _tokens[_i];
        bool NotAtEnd() => !Check(TokenType.Eof);
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
            return left;
        }

        Expr ParseUnary()
        {
            if (Check(TokenType.Sub, TokenType.Not))
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
