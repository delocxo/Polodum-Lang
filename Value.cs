using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Polodum
{
    internal struct Value
    {
        public Value(double number) : this()
        {
            Number = number;
            Kind = ValueKind.Number;
        }

        public Value(string @string) : this()
        {
            String = @string;
            Kind = ValueKind.String;
        }

        public Value(bool @bool) : this()
        {
            Bool = @bool;
            Kind = ValueKind.Bool;
        }

        public override string ToString()
        {
            if (Kind == ValueKind.String)
                return String;

            else if (Kind == ValueKind.Number)
                return Number.ToString(CultureInfo.InvariantCulture);

            else if (Kind == ValueKind.Bool)
                return Bool.ToString();

            return "invalid type";
        }

        public static bool CheckEquallity(Value left, Value right)
        {
            if (left.Kind != right.Kind)
                return false;

            else if (left.IsKind(ValueKind.Number) && right.IsKind(ValueKind.Number))
                return left.Number == right.Number;

            else if (left.IsKind(ValueKind.String) && right.IsKind(ValueKind.String))
                return left.String == right.String;

            else if (left.IsKind(ValueKind.Bool) && right.IsKind(ValueKind.Bool))
                return left.Bool == right.Bool;

            return false;
        }

        public static bool IsTruthy(Value value)
        {
            if (value.IsKind(ValueKind.Bool))
                return value.Bool;
            return true;
        }

        public bool IsKind(int kind) => Kind == kind;
        public string KindName => ValueKind.GetName(Kind);

        public int Kind { get; }
        public double Number { get; }
        public string String { get; } = string.Empty;
        public bool Bool { get; }
    }
}
