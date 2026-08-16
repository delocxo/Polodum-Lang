using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Polodum
{
    internal static class RaylibFunctions
    {
        public static Value Register()
        {
            Namespace @namespace = new Namespace("raylib");
            Value value = new Value(@namespace);

            Namespace keys = new Namespace("keys");
            Value keysValue = new Value(keys);

            #region Keys

            keys.Set("null", new Value(0));
            keys.Set("a", new Value(64));

            keys.Set("null", new Value((int)KeyboardKey.Null));

            keys.Set("space", new Value((int)KeyboardKey.Space));
            keys.Set("apostrophe", new Value((int)KeyboardKey.Apostrophe));
            keys.Set("comma", new Value((int)KeyboardKey.Comma));
            keys.Set("minus", new Value((int)KeyboardKey.Minus));
            keys.Set("period", new Value((int)KeyboardKey.Period));
            keys.Set("slash", new Value((int)KeyboardKey.Slash));

            keys.Set("zero", new Value((int)KeyboardKey.Zero));
            keys.Set("one", new Value((int)KeyboardKey.One));
            keys.Set("two", new Value((int)KeyboardKey.Two));
            keys.Set("three", new Value((int)KeyboardKey.Three));
            keys.Set("four", new Value((int)KeyboardKey.Four));
            keys.Set("five", new Value((int)KeyboardKey.Five));
            keys.Set("six", new Value((int)KeyboardKey.Six));
            keys.Set("seven", new Value((int)KeyboardKey.Seven));
            keys.Set("eight", new Value((int)KeyboardKey.Eight));
            keys.Set("nine", new Value((int)KeyboardKey.Nine));

            keys.Set("semicolon", new Value((int)KeyboardKey.Semicolon));
            keys.Set("equal", new Value((int)KeyboardKey.Equal));

            keys.Set("a", new Value((int)KeyboardKey.A));
            keys.Set("b", new Value((int)KeyboardKey.B));
            keys.Set("c", new Value((int)KeyboardKey.C));
            keys.Set("d", new Value((int)KeyboardKey.D));
            keys.Set("e", new Value((int)KeyboardKey.E));
            keys.Set("f", new Value((int)KeyboardKey.F));
            keys.Set("g", new Value((int)KeyboardKey.G));
            keys.Set("h", new Value((int)KeyboardKey.H));
            keys.Set("i", new Value((int)KeyboardKey.I));
            keys.Set("j", new Value((int)KeyboardKey.J));
            keys.Set("k", new Value((int)KeyboardKey.K));
            keys.Set("l", new Value((int)KeyboardKey.L));
            keys.Set("m", new Value((int)KeyboardKey.M));
            keys.Set("n", new Value((int)KeyboardKey.N));
            keys.Set("o", new Value((int)KeyboardKey.O));
            keys.Set("p", new Value((int)KeyboardKey.P));
            keys.Set("q", new Value((int)KeyboardKey.Q));
            keys.Set("r", new Value((int)KeyboardKey.R));
            keys.Set("s", new Value((int)KeyboardKey.S));
            keys.Set("t", new Value((int)KeyboardKey.T));
            keys.Set("u", new Value((int)KeyboardKey.U));
            keys.Set("v", new Value((int)KeyboardKey.V));
            keys.Set("w", new Value((int)KeyboardKey.W));
            keys.Set("x", new Value((int)KeyboardKey.X));
            keys.Set("y", new Value((int)KeyboardKey.Y));
            keys.Set("z", new Value((int)KeyboardKey.Z));

            keys.Set("leftBracket", new Value((int)KeyboardKey.LeftBracket));
            keys.Set("backslash", new Value((int)KeyboardKey.Backslash));
            keys.Set("rightBracket", new Value((int)KeyboardKey.RightBracket));
            keys.Set("grave", new Value((int)KeyboardKey.Grave));

            keys.Set("escape", new Value((int)KeyboardKey.Escape));
            keys.Set("enter", new Value((int)KeyboardKey.Enter));
            keys.Set("tab", new Value((int)KeyboardKey.Tab));
            keys.Set("backspace", new Value((int)KeyboardKey.Backspace));
            keys.Set("insert", new Value((int)KeyboardKey.Insert));
            keys.Set("delete", new Value((int)KeyboardKey.Delete));

            keys.Set("right", new Value((int)KeyboardKey.Right));
            keys.Set("left", new Value((int)KeyboardKey.Left));
            keys.Set("down", new Value((int)KeyboardKey.Down));
            keys.Set("up", new Value((int)KeyboardKey.Up));

            keys.Set("pageUp", new Value((int)KeyboardKey.PageUp));
            keys.Set("pageDown", new Value((int)KeyboardKey.PageDown));
            keys.Set("home", new Value((int)KeyboardKey.Home));
            keys.Set("end", new Value((int)KeyboardKey.End));

            keys.Set("capsLock", new Value((int)KeyboardKey.CapsLock));
            keys.Set("scrollLock", new Value((int)KeyboardKey.ScrollLock));
            keys.Set("numLock", new Value((int)KeyboardKey.NumLock));
            keys.Set("printScreen", new Value((int)KeyboardKey.PrintScreen));
            keys.Set("pause", new Value((int)KeyboardKey.Pause));

            keys.Set("f1", new Value((int)KeyboardKey.F1));
            keys.Set("f2", new Value((int)KeyboardKey.F2));
            keys.Set("f3", new Value((int)KeyboardKey.F3));
            keys.Set("f4", new Value((int)KeyboardKey.F4));
            keys.Set("f5", new Value((int)KeyboardKey.F5));
            keys.Set("f6", new Value((int)KeyboardKey.F6));
            keys.Set("f7", new Value((int)KeyboardKey.F7));
            keys.Set("f8", new Value((int)KeyboardKey.F8));
            keys.Set("f9", new Value((int)KeyboardKey.F9));
            keys.Set("f10", new Value((int)KeyboardKey.F10));
            keys.Set("f11", new Value((int)KeyboardKey.F11));
            keys.Set("f12", new Value((int)KeyboardKey.F12));

            keys.Set("leftShift", new Value((int)KeyboardKey.LeftShift));
            keys.Set("leftControl", new Value((int)KeyboardKey.LeftControl));
            keys.Set("leftAlt", new Value((int)KeyboardKey.LeftAlt));
            keys.Set("leftSuper", new Value((int)KeyboardKey.LeftSuper));

            keys.Set("rightShift", new Value((int)KeyboardKey.RightShift));
            keys.Set("rightControl", new Value((int)KeyboardKey.RightControl));
            keys.Set("rightAlt", new Value((int)KeyboardKey.RightAlt));
            keys.Set("rightSuper", new Value((int)KeyboardKey.RightSuper));

            keys.Set("kbMenu", new Value((int)KeyboardKey.KeyboardMenu));

            keys.Set("kp0", new Value((int)KeyboardKey.Kp0));
            keys.Set("kp1", new Value((int)KeyboardKey.Kp1));
            keys.Set("kp2", new Value((int)KeyboardKey.Kp2));
            keys.Set("kp3", new Value((int)KeyboardKey.Kp3));
            keys.Set("kp4", new Value((int)KeyboardKey.Kp4));
            keys.Set("kp5", new Value((int)KeyboardKey.Kp5));
            keys.Set("kp6", new Value((int)KeyboardKey.Kp6));
            keys.Set("kp7", new Value((int)KeyboardKey.Kp7));
            keys.Set("kp8", new Value((int)KeyboardKey.Kp8));
            keys.Set("kp9", new Value((int)KeyboardKey.Kp9));

            keys.Set("kpDecimal", new Value((int)KeyboardKey.KpDecimal));
            keys.Set("kpDivide", new Value((int)KeyboardKey.KpDivide));
            keys.Set("kpMultiply", new Value((int)KeyboardKey.KpMultiply));
            keys.Set("kpSubtract", new Value((int)KeyboardKey.KpSubtract));
            keys.Set("kpAdd", new Value((int)KeyboardKey.KpAdd));
            keys.Set("kpEnter", new Value((int)KeyboardKey.KpEnter));
            keys.Set("kpEqual", new Value((int)KeyboardKey.KpEqual));

            keys.Set("back", new Value((int)KeyboardKey.Back));
            keys.Set("menu", new Value((int)KeyboardKey.Menu));
            keys.Set("volumeUp", new Value((int)KeyboardKey.VolumeUp));
            keys.Set("volumeDown", new Value((int)KeyboardKey.VolumeDown));

            #endregion Keys

            Namespace mouseButtons = new Namespace("mouseButtons");
            Value mouseButtonsValue = new Value(mouseButtons);

            #region Mouse
            mouseButtons.Set("left", new Value((int)MouseButton.Left));
            mouseButtons.Set("right", new Value((int)MouseButton.Right));
            mouseButtons.Set("middle", new Value((int)MouseButton.Middle));
            mouseButtons.Set("side", new Value((int)MouseButton.Side));
            mouseButtons.Set("extra", new Value((int)MouseButton.Extra));
            mouseButtons.Set("forward", new Value((int)MouseButton.Forward));
            mouseButtons.Set("back", new Value((int)MouseButton.Back));

            @namespace.Set("mouseButtons", mouseButtonsValue);
            #endregion Mouse

            Namespace time = new Namespace("time");
            Value timeValue = new Value(time);

            time.Set("frameTime", Value.FromNativeExpected(0, "frameTime", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetFrameTime());
            }));

            time.Set("fps", Value.FromNativeExpected(0, "fps", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetFPS());
            }));

            time.Set("time", Value.FromNativeExpected(0, "time", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetTime());
            }));

            @namespace.Set("time", timeValue);

            @namespace.Set("openWindow", Value.FromNativeExpected(3, "openWindow", ["width", "height", "title"], value, (args, pos) =>
            {
                int width = args[0].ExpectIntInRangeIn(1, int.MaxValue, "Window width out of range", pos);
                int height = args[1].ExpectIntInRangeIn(1, int.MaxValue, "Window height out of range", pos);
                string title = args[2].ExpectKinds("Expected window title string", pos, ValueKind.String).String;
                Raylib.InitWindow(width, height, title);
                return Vm.MakeNone();
            }));

            @namespace.Set("closeWindow", Value.FromNativeExpected(0, "closeWindow", [], value, (args, pos) =>
            {
                Raylib.CloseWindow();
                return Vm.MakeNone();
            }));

            @namespace.Set("windowShouldClose", Value.FromNativeExpected(0, "windowShouldClose", [], value, (args, pos) =>
            {
                return new Value((bool)Raylib.WindowShouldClose());
            }));

            @namespace.Set("beginDraw", Value.FromNativeExpected(0, "beginDraw", [], value, (args, pos) =>
            {
                Raylib.BeginDrawing();
                return Vm.MakeNone();
            }));

            @namespace.Set("endDraw", Value.FromNativeExpected(0, "endDraw", [], value, (args, pos) =>
            {
                Raylib.EndDrawing();
                return Vm.MakeNone();
            }));

            @namespace.Set("clear", Value.FromNativeExpected(4, "clear", ["r", "g", "b", "a"], value, (args, pos) =>
            {
                Color color = ExpectColor(args, pos);
                Raylib.ClearBackground(color);
                return Vm.MakeNone();
            }));

            @namespace.Set("drawText", Value.FromNativeExpected(8, "drawText", ["text", "x", "y", "size", "r", "g", "b", "a"], value, (args, pos) =>
            {
                string title = args[0].ExpectKinds("Expected text", pos, ValueKind.String).String;
                int x = args[1].ExpectInt(pos);
                int y = args[2].ExpectInt(pos);
                int size = args[3].ExpectInt(pos);
                Color color = ExpectColor(args, pos);
                Raylib.DrawText(title, x, y, size, color);
                return Vm.MakeNone();
            }));

            @namespace.Set("drawRect", Value.FromNativeExpected(8, "drawRect", ["x", "y", "width", "height", "r", "g", "b", "a"], value, (args, pos) =>
            {
                int x = args[0].ExpectInt(pos);
                int y = args[1].ExpectInt(pos);
                int width = args[2].ExpectInt(pos);
                int height = args[3].ExpectInt(pos);
                Color color = ExpectColor(args, pos);
                Raylib.DrawRectangle(x, y, width, height, color);
                return Vm.MakeNone();
            }));

            @namespace.Set("drawCircle", Value.FromNativeExpected(7, "drawCircle", ["x", "y", "radius", "r", "g", "b", "a"], value, (args, pos) =>
            {
                int x = args[0].ExpectInt(pos);
                int y = args[1].ExpectInt(pos);
                float radius = args[2].ExpectFloat32(pos);
                Color color = ExpectColor(args, pos);
                Raylib.DrawCircle(x, y, radius, color);
                return Vm.MakeNone();
            }));

            @namespace.Set("keyDown", Value.FromNativeExpected(1, "keyDown", ["key"], value, (args, pos) =>
            {
                KeyboardKey key = ExpectKey(args[0], pos);
                return new Value((bool)Raylib.IsKeyDown(key));
            }));

            @namespace.Set("keyPressed", Value.FromNativeExpected(1, "keyPressed", ["key"], value, (args, pos) =>
            {
                KeyboardKey key = ExpectKey(args[0], pos);
                return new Value((bool)Raylib.IsKeyPressed(key));
            }));

            @namespace.Set("keyReleased", Value.FromNativeExpected(1, "keyReleased", ["key"], value, (args, pos) =>
            {
                KeyboardKey key = ExpectKey(args[0], pos);
                return new Value((bool)Raylib.IsKeyReleased(key));
            }));

            @namespace.Set("keyUp", Value.FromNativeExpected(1, "keyUp", ["key"], value, (args, pos) =>
            {
                KeyboardKey key = ExpectKey(args[0], pos);
                return new Value((bool)Raylib.IsKeyUp(key));
            }));

            @namespace.Set("getKeyName", Value.FromNativeExpected(1, "getKeyName", ["key"], value, (args, pos) =>
            {
                KeyboardKey key = ExpectKey(args[0], pos);
                unsafe
                {
                    sbyte* name = Raylib.GetKeyName(key);
                    string result = Marshal.PtrToStringUTF8((IntPtr)name) ?? "";
                    return new Value(result);
                }
            }));

            @namespace.Set("getKeyPressed", Value.FromNativeExpected(0, "getKeyPressed", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetKeyPressed());
            }));

            @namespace.Set("mouseDown", Value.FromNativeExpected(1, "mouseDown", ["mouseButton"], value, (args, pos) =>
            {
                MouseButton mouseButton = ExpectMouseButton(args[0], pos);
                return new Value((bool)Raylib.IsMouseButtonDown(mouseButton));
            }));

            @namespace.Set("mousePressed", Value.FromNativeExpected(1, "mousePressed", ["mouseButton"], value, (args, pos) =>
            {
                MouseButton mouseButton = ExpectMouseButton(args[0], pos);
                return new Value((bool)Raylib.IsMouseButtonPressed(mouseButton));
            }));

            @namespace.Set("mouseReleased", Value.FromNativeExpected(1, "mouseReleased", ["mouseButton"], value, (args, pos) =>
            {
                MouseButton mouseButton = ExpectMouseButton(args[0], pos);
                return new Value((bool)Raylib.IsMouseButtonReleased(mouseButton));
            }));

            @namespace.Set("mouseUp", Value.FromNativeExpected(1, "mouseUp", ["mouseButton"], value, (args, pos) =>
            {
                MouseButton mouseButton = ExpectMouseButton(args[0], pos);
                return new Value((bool)Raylib.IsMouseButtonUp(mouseButton));
            }));

            @namespace.Set("mouseX", Value.FromNativeExpected(0, "mouseX", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetMouseX());
            }));

            @namespace.Set("mouseY", Value.FromNativeExpected(0, "mouseY", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetMouseY());
            }));

            @namespace.Set("mouseWheel", Value.FromNativeExpected(0, "mouseWheel", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetMouseWheelMove());
            }));

            @namespace.Set("setTargetFps", Value.FromNativeExpected(1, "setTargetFps", ["targetFps"], value, (args, pos) =>
            {
                int targetFps = args[0].ExpectIntInRangeIn(0, int.MaxValue, "Target fps out of range", pos);
                Raylib.SetTargetFPS(targetFps);
                return Vm.MakeNone();
            }));

            @namespace.Set("windowWidth", Value.FromNativeExpected(0, "windowWidth", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetScreenWidth());
            }));

            @namespace.Set("windowHeight", Value.FromNativeExpected(0, "windowHeight", [], value, (args, pos) =>
            {
                return new Value(Raylib.GetScreenHeight());
            }));

            return value;
        }

        static Color ExpectColor(PoloArray args, Position position)
        {
            byte r = (byte)args[args.Count - 4].ExpectIntInRangeIn(0, 255, "Red out of range", position);
            byte g = (byte)args[args.Count - 3].ExpectIntInRangeIn(0, 255, "Green out of range", position);
            byte b = (byte)args[args.Count - 2].ExpectIntInRangeIn(0, 255, "Blue out of range", position);
            byte a = (byte)args[args.Count - 1].ExpectIntInRangeIn(0, 255, "Alpha out of range", position);
            return new Color(r, g, b, a);
        }

        static KeyboardKey ExpectKey(Value key, Position position)
        {
            int raw = key.ExpectIntInRangeIn(0, (int)KeyboardKey.KeyboardMenu, "Key out of range", position);
            if (!Enum.IsDefined(typeof(KeyboardKey), raw))
                throw new Error($"Invalid keyboard key '{raw}'", position);
            return (KeyboardKey)raw;
        }

        static MouseButton ExpectMouseButton(Value button, Position position)
        {
            return (MouseButton)button.ExpectIntInRangeIn(0, 6, "Mouse button out of range", position);
        }
    }
}
