namespace Polodum
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: polodum <file.polo>");
                Environment.Exit(1);
            }

            try
            {
                Compiler compiler = new Compiler(true);
                compiler.CompileFile(args[0], new Position(0, 0, ""), true);
                compiler.AddHalt();
                //compiler.Chunk.Print();
                Vm vm = new Vm(compiler.Chunk);
                vm.Execute();
            }
            catch (Error e)
            {
                e.Exit();
            }
        }
    }
}
