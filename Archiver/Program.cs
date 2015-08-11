using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Archiver
{
    class Program
    {
        static void Main(string[] args)
        {
            Archive arch = new Archive("D:/images/1", 1024, 1024*1024);
            Console.ReadKey();
        }
    }
}
