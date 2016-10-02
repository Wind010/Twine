//---------------------------------------------------------------------------------------------------------
// <copyright file="TestRetryExec.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Just a console utility to test RetryExec with code signing.
// </summary>
//---------------------------------------------------------------------------------------------------------

namespace TestRetryExec
{
    using System;

    class TestRetryExec
    {
        static int Main(string[] args)
        {
            Console.WriteLine("TestRetryExec");
            
            if (args[0].Equals("Fail", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("\"Error: SignerSign() failed.\" (-2147012867/0x80072efd)");
                Console.Error.WriteLine("TestRetryExec - Pass");
                Console.Error.Close();
                return 1;
            }
            else if (args[0].Equals("Pass", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("TestRetryExec - Pass");
                return 0;
            }

            switch (args[0].ToUpper())
            {
                case "FAIL":
                    Console.Error.WriteLine("\"Error: SignerSign() failed.\" (-2147012867/0x80072efd)");
                    Console.Error.WriteLine("TestRetryExec - Pass");
                    Console.Error.Close();
                    return 1;
                case "PASS":
                    Console.WriteLine("TestRetryExec - Pass");
                    return 0; 
                case "EXCEPTION":
                    throw new Exception("Error: SignerSign() failed.\" (-2147012867/0x80072efd)");
            }

            return -1;
        }
    }
}
