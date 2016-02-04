using System;
using System.Threading;


namespace SemaphoreTest
{
    class Program
    {
        static Thread[] threads = new Thread[10];
        static Semaphore sem = new Semaphore(1,1);
        static void C_sharpcorner()
        {
            Console.WriteLine("{0} is waiting in line...", Thread.CurrentThread.Name);
            sem.WaitOne();
            Console.WriteLine("{0} enters the C_sharpcorner.com!", Thread.CurrentThread.Name);
            Thread.Sleep(300);
            Console.WriteLine("{0} is leaving the C_sharpcorner.com", Thread.CurrentThread.Name);
            sem.Release();
        }
        static void Main(string[] args)
        {
            for (int i = 0; i < 10; i++)
            {
                threads[i] = new Thread(C_sharpcorner);
                threads[i].Name = "thread_" + i;
                threads[i].Start();
            }
            Console.Read();
        }
    }
}
