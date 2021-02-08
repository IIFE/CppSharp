using CppSharp;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Generators;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 1)
            {
                ConsoleDriver.Run(new BusObjsManagedWrapperGenerator(args[0]));

                return 0;
            }

            return 1;
        }
    }
}
