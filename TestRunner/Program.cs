﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using TestRunner.Properties;
using System.IO;

namespace TestRunner
{
    class MySettings : Dev.ISettings
    {
        Action<string> wrt; 
        public MySettings(Action<string> wrt)
        {
            this.wrt = wrt;
        }
        public string this[string name]
        {
            get
            {
                return (string)Settings.Default[name];
            }
        }


        public Action<string> GetWriter()
        {
            return wrt;
        }
    }

    class Program
    {
        static string[] TestPacks = new string[] 
        {
#if CASSANDRA_NET_40_OR_GREATER
            "Cassandra.Data.Linq.Test",
#endif
            "Cassandra.Native.Test"
        };


        static void Test(ref object testObj, Type type, MethodInfo mth, Dev.SettingsFixture mysetting, StreamWriter output, ref int Passed, ref int Failed)
        {
            if (testObj == null)
            {
                testObj = type.GetConstructor(new Type[] { }).Invoke(new object[] { });
                var ist = type.GetMethod("SetFixture");
                if (ist != null)
                {
                    ist.Invoke(testObj, new object[] { mysetting });
                }
            }
            try
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
                var s = type.FullName + "." + mth.Name + "() Start...";
                Console.WriteLine(new string(' ', 79));
                Console.WriteLine(s);
                output.WriteLine(new string('-', 79));
                output.WriteLine(s);
                Console.ResetColor();
                mth.Invoke(testObj, new object[] { });
                Passed++;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Green;
                s = type.FullName + "." + mth.Name + "() Passed";
                Console.WriteLine(s);
                Console.WriteLine(new string(' ', 79));
                output.WriteLine(s);
                output.WriteLine(new string('-', 79));
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                if (ex.InnerException is Dev.AssertException)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    var s = type.FullName + "." + mth.Name + "() Failed!";
                    Console.WriteLine(s);
                    output.WriteLine(s);
                    s = (ex.InnerException as Dev.AssertException).UserMessage;
                    Console.WriteLine(s);
                    Console.WriteLine(new string(' ', 79));
                    output.WriteLine(s);
                    output.WriteLine(new string('-', 79));
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Magenta;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine("Exception");
                    Console.WriteLine(ex.InnerException.Source);
                    Console.WriteLine(ex.InnerException.Message);
                    output.WriteLine("Exception");
                    output.WriteLine(ex.InnerException.Source);
                    output.WriteLine(ex.InnerException.Message);
                    if (ex.InnerException.InnerException != null)
                        printInnerException(ex.InnerException.InnerException, output);
                }
                Console.WriteLine(ex.InnerException.StackTrace);
                output.WriteLine(ex.InnerException.StackTrace);
                Console.ResetColor();
                Failed++;
                output.Flush();
            }
            Console.WriteLine();
            output.WriteLine();            
        }

        static void Main(string[] args)
        {
            var mysetting = new Dev.SettingsFixture();

            StreamWriter output = null;

            var setting = new MySettings((str) => { output.WriteLine(str); });           
            mysetting.Initialize(setting);

            var tstDir = setting["TestFolder"].Replace("$TEST_ROOT", Directory.GetCurrentDirectory());
            Directory.CreateDirectory(tstDir);

            output = new StreamWriter(tstDir + "/" + DateTime.Now.ToShortDateString().Replace("/", "_") + "+" + DateTime.Now.ToShortTimeString().Replace(":", "_") + ".log");

            int Passed = 0;
            int Failed = 0;

            foreach (var asmn in TestPacks)
            {
                var asm = Assembly.Load(asmn);
                foreach (var type in asm.GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(Dev.IgnoreAttribute), true).Length > 0)
                        continue;
                    if (type.Name.EndsWith("Tests") && type.IsPublic)
                    {
                        object testObj = null;
                        foreach (var mth in type.GetMethods())
                        {
                            if (mth.GetCustomAttributes(typeof(Dev.PriorityAttribute), true).Length == 0)
                                continue;
                            if (mth.GetCustomAttributes(typeof(Dev.FactAttribute), true).Length > 0)
                            {
                                Test(ref testObj, type, mth, mysetting, output, ref Passed, ref Failed);
                            }
                        }
                        if (testObj != null)
                        {
                            var ist = type.GetMethod("Dispose");
                            if (ist != null)
                                ist.Invoke(testObj, new object[] { });
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                }
            }

            foreach (var asmn in TestPacks)
            {
                var asm = Assembly.Load(asmn);
                foreach (var type in asm.GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(Dev.IgnoreAttribute), true).Length > 0)
                        continue;
                    if (type.GetCustomAttributes(typeof(Dev.PriorityAttribute), true).Length > 0)
                        continue;
                    if (type.Name.EndsWith("Tests") && type.IsPublic)
                    {
                        object testObj = null;
                        foreach (var mth in type.GetMethods())
                        {
                            if (mth.GetCustomAttributes(typeof(Dev.PriorityAttribute), true).Length > 0)
                                continue;
                            if (mth.GetCustomAttributes(typeof(Dev.FactAttribute), true).Length > 0)
                            {
                                Test(ref testObj, type, mth, mysetting, output, ref Passed, ref Failed);
                            }
                        }
                        if (testObj != null)
                        {
                            var ist = type.GetMethod("Dispose");
                            if (ist != null)
                                ist.Invoke(testObj, new object[] { });
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Black;
            if (Failed > 0)
                Console.BackgroundColor = ConsoleColor.Red;
            else
                Console.BackgroundColor = ConsoleColor.Green;

            var st = Failed > 0 ? string.Format("[{0} (of {1}) Failures]", Failed, Failed + Passed) : string.Format("[All {0} Passed :)]", Passed);
            st += " Press Any Key To Close The Program";
            Console.WriteLine(new string(' ', 79));
            Console.WriteLine(st + new string(' ', 79 - st.Length));
            Console.WriteLine(new string(' ', 79));
            output.WriteLine(new string('-', 79));
            output.WriteLine(st);
            output.WriteLine(new string('-', 79));
            output.Close();
            if (Failed > 0)
                Console.ReadKey();
        }

        static void printInnerException(Exception ex, StreamWriter output)
        {
            Console.WriteLine("(");
            Console.WriteLine("Exception");
            Console.WriteLine(ex.Source);
            Console.WriteLine(ex.Message);
            output.WriteLine("Exception");
            output.WriteLine(ex.Source);
            output.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            output.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
                printInnerException(ex.InnerException, output);
            Console.WriteLine(")");
        }
    }
}
