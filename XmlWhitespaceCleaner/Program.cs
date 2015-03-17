using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlNormalizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Do(args);
		}


        static void Do(string[] args)
        {
            Initialiser init = new Initialiser(args);

            if (!init.Run())
            {
                return;
            }

            Processor proc = new Processor(init);

            if (!proc.Run())
            {
                return;
            }

            Console.WriteLine(Strings.SUCCESS_RETURN);
        }
   }
}
