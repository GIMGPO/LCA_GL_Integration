using System;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;

namespace XmlManipulator
{
	class XmlConsole
	{
		[STAThread]
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

			Console.WriteLine(ResStrings.SUCCESS_RETURN);
		}
	}
}
