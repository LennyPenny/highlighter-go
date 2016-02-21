using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bindgen
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputpath = "";

            if (args.Length == 1)
                inputpath = args[1];
            else
                inputpath = "output.json";


        }
    }
}
