
using System;

namespace SvoxBot
{
    class Program
    {
        static void Main(String[] args)
        {
            string prefix = Environment.GetEnvironmentVariable("SVOX_PREFIX");
            string token = Environment.GetEnvironmentVariable("SVOX_TOKEN");

            if (String.IsNullOrEmpty(prefix))
            {
                Console.WriteLine("SVOX_PREFIX is empty, using default value.");
                prefix = "!";
            }

            if (String.IsNullOrEmpty(token))
            {
                throw new Exception("SVOX_TOKEN is empty!");
            }

            // run bot
            new SvoxBot(token, prefix).RunBotAsync().GetAwaiter().GetResult();
        }
    }
}
