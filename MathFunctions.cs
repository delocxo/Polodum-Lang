using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Polodum
{
    internal static class MathFunctions
    {
        public static Value Register()
        {
            Namespace @namespace = new Namespace("math");
            Value value = new Value(@namespace);

            @namespace.Set("pi", new Value(Math.PI));
            @namespace.Set("e", new Value(Math.E));
            @namespace.Set("tau", new Value(Math.Tau));
            @namespace.Set("halfPi", new Value(Math.PI / 2d));

            @namespace.Set("floor", Value.FromNativeExpected(1, "floor", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Floor(number));
            }));

            @namespace.Set("ceil", Value.FromNativeExpected(1, "ceil", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Ceiling(number));
            }));

            @namespace.Set("round", Value.FromNativeExpected(2, "round", ["x", "digits"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                int digits = args[1].ExpectIntInRangeIn(0, 15, "Round digits out of range", pos);
                return new Value(Math.Round(number, digits));
            }));

            @namespace.Set("min", Value.FromNativeExpected(2, "min", ["x", "y"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double number1 = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Min(number, number1));
            }));

            @namespace.Set("max", Value.FromNativeExpected(2, "max", ["x", "y"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double number1 = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Max(number, number1));
            }));

            @namespace.Set("clamp", Value.FromNativeExpected(3, "clamp", ["x", "min", "max"], value, (args, pos) =>
            {
                double value = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double min = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double max = args[2].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                if (min > max)
                    throw new Error("Min clamp value cannot be more than clamps max value", pos);
                return new Value(Math.Clamp(value, min, max));
            }));

            @namespace.Set("sqrt", Value.FromNativeExpected(1, "sqrt", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Sqrt(number));
            }));

            @namespace.Set("pow", Value.FromNativeExpected(2, "pow", ["x", "exponent"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double exponent = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Pow(number, exponent));
            }));

            @namespace.Set("sin", Value.FromNativeExpected(1, "sin", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Sin(number));
            }));

            @namespace.Set("cos", Value.FromNativeExpected(1, "cos", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Cos(number));
            }));

            @namespace.Set("tan", Value.FromNativeExpected(1, "tan", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Tan(number));
            }));

            @namespace.Set("atan2", Value.FromNativeExpected(2, "atan2", ["y", "x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double number2 = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Atan2(number, number2));
            }));

            @namespace.Set("random", Value.FromNativeExpected(2, "random", ["min", "max"], value, (args, pos) =>
            {
                double min = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double max = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(min + (Random.Shared.NextDouble() * (max - min)));
            }));

            @namespace.Set("random01", Value.FromNativeExpected(0, "random01", [], value, (args, pos) =>
            {
                return new Value(Random.Shared.NextDouble());
            }));

            return value;
        }
    }
}
