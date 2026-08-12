using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum
{
    internal enum TokenType
    {
        String, Number, Identifier,

        True, False, Mut, End, Do, Proc, For,
        If, Else, ElseIf, Out,

        Add, Sub, Mul, Div, Mod,
        IsEqual, NotEqual, Less, Greater,
        LessEq, GreaterEq, And, Or, Not,

        Equal, Semicolon, LeftBracket, RightBracket,
        LeftBrace, RightBrace, LeftParen, RightParen,

        Eof,
    }

    internal record Token(TokenType TokenType, string Lexeme, Position Position);
}
