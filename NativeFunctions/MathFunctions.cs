using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Polodum.NativeFunctions
{
    internal class MathFunctions : INativeFunctions
    {
        public string Name { get; } = "math";

        public Value Register()
        {
            Namespace @namespace = new Namespace("math");
            Value value = new Value(@namespace);

            @namespace.Set("pi", new Value(Math.PI));
            @namespace.Set("e", new Value(Math.E));
            @namespace.Set("tau", new Value(Math.Tau));
            @namespace.Set("halfPi", new Value(Math.PI / 2d));

            @namespace.Set("floor", Value.FromNativeExpected("floor", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Floor(number));
            }));

            @namespace.Set("ceil", Value.FromNativeExpected("ceil", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Ceiling(number));
            }));

            @namespace.Set("round", Value.FromNativeExpected("round", ["x", "digits"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                int digits = args[1].ExpectIntInRangeIn(0, 15, "Round digits out of range", pos);
                return new Value(Math.Round(number, digits));
            }));

            @namespace.Set("min", Value.FromNativeExpected("min", ["x", "y"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double number1 = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Min(number, number1));
            }));

            @namespace.Set("max", Value.FromNativeExpected("max", ["x", "y"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double number1 = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Max(number, number1));
            }));

            @namespace.Set("clamp", Value.FromNativeExpected("clamp", ["x", "min", "max"], value, (args, pos) =>
            {
                double value = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double min = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double max = args[2].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                if (min > max)
                    throw new Error("Min clamp value cannot be more than clamps max value", pos);
                return new Value(Math.Clamp(value, min, max));
            }));

            @namespace.Set("sqrt", Value.FromNativeExpected("sqrt", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Sqrt(number));
            }));

            @namespace.Set("pow", Value.FromNativeExpected("pow", ["x", "exponent"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double exponent = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Pow(number, exponent));
            }));

            @namespace.Set("sin", Value.FromNativeExpected("sin", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Sin(number));
            }));

            @namespace.Set("cos", Value.FromNativeExpected("cos", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Cos(number));
            }));

            @namespace.Set("tan", Value.FromNativeExpected("tan", ["x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Tan(number));
            }));

            @namespace.Set("atan2", Value.FromNativeExpected("atan2", ["y", "x"], value, (args, pos) =>
            {
                double number = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double number2 = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Atan2(number, number2));
            }));

            @namespace.Set("random", Value.FromNativeExpected("random", ["min", "max"], value, (args, pos) =>
            {
                double min = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double max = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(min + (Random.Shared.NextDouble() * (max - min)));
            }));

            @namespace.Set("random01", Value.FromNativeExpected("random01", [], value, (args, pos) =>
            {
                return new Value(Random.Shared.NextDouble());
            }));

            @namespace.SetNative(Value.FromNativeExpected("abs", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Abs(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("sign", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Sign(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("trunc", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Truncate(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("lerp", ["a", "b", "t"], value, (args, pos) =>
            {
                double a = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double b = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double t = args[2].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(a + (b - a) * t);
            }));

            @namespace.SetNative(Value.FromNativeExpected("inverseLerp", ["a", "b", "x"], value, (args, pos) =>
            {
                double a = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double b = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double x = args[2].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                if (x == a)
                    return new Value(0);
                else if (x == b)
                    return new Value(1);
                return new Value((x - a) / (b - a));
            }));

            @namespace.SetNative(Value.FromNativeExpected("asin", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Asin(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("acos", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Acos(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("atan", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Atan(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("degToRad", ["degrees"], value, (args, pos) =>
            {
                double degrees = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(degrees * (Math.PI / 180));
            }));

            @namespace.SetNative(Value.FromNativeExpected("radToDeg", ["radians"], value, (args, pos) =>
            {
                double radians = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(radians * (180 / Math.PI));
            }));

            @namespace.SetNative(Value.FromNativeExpected("log", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Log(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("log10", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Log10(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("log2", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Log2(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("exp", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Exp(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("cbrt", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Cbrt(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("clamp01", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Clamp(x, 0, 1));
            }));

            @namespace.SetNative(Value.FromNativeExpected("signedFract", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(x - Math.Truncate(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("fract", ["x"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(x - Math.Floor(x));
            }));

            @namespace.SetNative(Value.FromNativeExpected("moveTowards", ["current", "target", "delta"], value, (args, pos) =>
            {
                double current = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double target = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double delta = args[2].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(current + Math.Sign(target - current) * delta);
            }));

            @namespace.SetNative(Value.FromNativeExpected("approx", ["a", "b", "tolerance"], value, (args, pos) =>
            {
                double a = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double b = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double tolerance = args[2].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                if (double.IsNegative(tolerance))
                    throw new Error("Tolerance cannot be negative", pos);
                return new Value(Math.Abs(a - b) <= tolerance);
            }));

            @namespace.SetNative(Value.FromNativeExpected("hypot", ["x", "y"], value, (args, pos) =>
            {
                double x = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double y = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)));
            }));

            @namespace.SetNative(Value.FromNativeExpected("remap", ["value", "oldMin", "oldMax", "newMin", "newMax"], value, (args, pos) =>
            {
                double value = args[0].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double oldMin = args[1].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double oldMax = args[2].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double newMin = args[3].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                double newMax = args[4].ExpectKinds("Expected number", pos, ValueKind.Number).Number;
                return new Value(newMin + (value - oldMin) * (newMax - newMin) / (oldMax - oldMin));
            }));

            return value;
        }
    }
}
