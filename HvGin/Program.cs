namespace HvGin
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SynthRdp.Start();
            while (SynthRdp.IsRunning)
            {
                Thread.Sleep(100);
            }
        }
    }
}
