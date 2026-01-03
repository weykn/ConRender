using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConRender
{
    public class IO
    {
        public static void Print(string message)
        {
            Console.WriteLine(message);
        }

        public static void Exit(int exitCode = 0)
        {
            Environment.Exit(exitCode);
        }

        public static object ArgumentError(string message)
        {
            Print(message);
            Exit(1);
            return 0;
        }
    }
}
