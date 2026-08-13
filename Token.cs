using System;
using System.Collections.Generic;
using System.Text;

namespace Polodum
{
    internal enum TokenType
    {
        String, Number, Identifier,

        True, False, Mut, End, Do, Proc, For,
        If, Else, ElseIf, Out, Not, Is, Ret,
        Leave, Break, Continue,

        Add, Sub, Mul, Div, Mod,
        IsEqual, NotEqual, Less, Greater,
        LessEq, GreaterEq, And, Or, Bang,

        Equal, Semicolon, LeftBracket, RightBracket,
        LeftBrace, RightBrace, LeftParen, RightParen,
        Comma,

        Eof,
    }

    internal record Token(TokenType TokenType, string Lexeme, Position Position);
}
