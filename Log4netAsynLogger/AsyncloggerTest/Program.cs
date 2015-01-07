using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace AsyncloggerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            Int32 i = 0;
            while (i < 100)
            {
                try
                {                
                    throw new NullReferenceException("TEST " + i.ToString() + " " +DateTime.Now.ToString());
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);
                }
                i++;
            }
            Console.WriteLine("shutdown.");
            Console.ReadKey();       
            LogManager.Shutdown();
        }
    }
}
