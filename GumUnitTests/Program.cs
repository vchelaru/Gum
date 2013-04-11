using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumUnitTests
{
    class Program
    {
        static void Main(string[] args)
        {
            bool succeeded = false;
            try
            {

                TestFramework.RunTests();
                succeeded = true;
            }

            catch (Exception e)
            {
                succeeded = false;
                System.Console.WriteLine("Error:\n" + e.ToString());
            }


            if (succeeded)
            {
                System.Console.WriteLine("All tests passed!");
            }

            System.Console.ReadLine();
        }
    }
}
