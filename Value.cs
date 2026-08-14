global using PoloArray = System.Collections.Generic.List<Polodum.Value>;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Polodum
{
    internal enum ArgumentMode
    {
        Expected,
    }

    internal class RecordField
    {
        public RecordField(string name, bool mutable, Value value)
        {
            Name = name;
            Mutable = mutable;
            Value = value;
        }

        public string Name { get; set; }
        public bool Mutable { get; set; }
        public Value Value { get; set; }
    }

    internal class Record
    {
        public Record(Dictionary<string, RecordField> fields, int id)
        {
            Fields = fields.ToFrozenDictionary();
            Id = id;
        }

        public FrozenDictionary<string, RecordField> Fields { get; }
        public int Id { get; }
    }

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

        public Value(FunctionInfo functionInfo) : this()
        {
            Object = functionInfo;
            Kind = ValueKind.Function;
        }

        public Value(PoloArray array) : this()
        {
            Object = array;
            Kind = ValueKind.Array;
        }

        public Value(Record record) : this()
        {
            Object = record;
            Kind = record.Id;
            IsRecord = true;
        }

        public override string ToString()
        {
            if (Kind == ValueKind.String)
                return String;

            else if (Kind == ValueKind.Number)
                return Number.ToString(CultureInfo.InvariantCulture);

            else if (Kind == ValueKind.Bool)
                return Bool ? "true" : "false";

            else if (Kind == ValueKind.Function)
            {
                string paremeters = string.Join(", ", FunctionInfo.Parameters);
                return $"<function {FunctionInfo.Name}({paremeters})>";
            }

            else if (Kind == ValueKind.Array)
            {
                string elements = string.Join(", ", Array.Select(x => x.Stringify()));
                return $"[{elements}]";
            }

            else if (IsRecord)
            {
                string fields = string.Join(", ", Record.Fields.Select(x =>
                {
                    RecordField field = x.Value;
                    return $"{(field.Mutable ? "mut " : "")}{field.Name} = {field.Value.Stringify()}";
                }));
                return $"{KindName} {{ {fields} }}";
            }

            return "invalid type";
        }

        public string Stringify()
        {
            if (Kind == ValueKind.String)
                return $"\"{ToString()}\"";
            return ToString();
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

            else if (left.IsKind(ValueKind.Function) && right.IsKind(ValueKind.Function))
                return left.FunctionInfo == right.FunctionInfo;

            else if (left.IsKind(ValueKind.Array) && right.IsKind(ValueKind.Array))
                return left.Array == right.Array;

            else if (left.IsRecord && right.IsRecord)
                return left.Kind == right.Kind;

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

        public int ExpectInt(Position position)
        {
            if (!IsKind(ValueKind.Number))
                throw new Error("Expected number", position);

            if (!double.IsInteger(Number))
                throw new Error("Expected integer", position);

            if (Number < int.MinValue || Number > int.MaxValue)
                throw new Error("Integer out of range", position);

            return (int)Number;
        }

        public int ExpectIntInRange(bool isExclusive, int min, int max, string message, Position position)
        {
            int integer = ExpectInt(position);
            if (integer < min || (isExclusive ? integer >= max : integer > max))
                throw new Error($"Index {integer} is outside of the range {min}-{(isExclusive ? max - 1 : max)}: {message}", position);
            return integer;
        }

        public int ExpectIntInRangeEx(int min, int max, string message, Position position)
        {
            return ExpectIntInRange(true, min, max, message, position);
        }

        public int ExpectIntInRangeIn(int min, int max, string message, Position position)
        {
            return ExpectIntInRange(false, min, max, message, position);
        }

        public Value ExpectKinds(string message, Position position, params int[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
                if (IsKind(kinds[i]))
                    return this;
            throw new Error($"Expected type(s): {string.Join(", ", kinds.Select(x => ValueKind.GetName(x)))}: {message}", position);
        }

        public int Kind { get; }
        public bool IsRecord { get; } = false;
        public double Number { get; }
        public string String { get; } = string.Empty;
        public bool Bool { get; }
        public object? Object { get; }
        public FunctionInfo FunctionInfo => (FunctionInfo)Object!;
        public PoloArray Array => (PoloArray)Object!;
        public Record Record => (Record)Object!;
    }
}
