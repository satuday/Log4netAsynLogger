using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Core;
using log4net.Appender;
using System.Threading;
using System.Configuration;
using System.Collections.Concurrent;

namespace Log4netAsynAppender
{
    public class AsynchronousAdoNetAppender : AdoNetAppender
    {
        private ConcurrentQueue<LoggingEvent> pendingEvents;
        private readonly object lockObject = new object();
        private readonly ManualResetEvent manualResetEvent;
        private bool isClosing;

        public AsynchronousAdoNetAppender()
        {
            pendingEvents = new ConcurrentQueue<LoggingEvent>();
            manualResetEvent = new ManualResetEvent(false);
            Start();
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (LoggingEvent loggingEvent in loggingEvents)
            {
                Append(loggingEvent);
            }
        }
        protected override void Append(LoggingEvent loggingEvent)
        {
            //LocationInformation properties is [?] when it is never accessed. very strange???
            var lc = loggingEvent.LocationInformation;
            //**********************************//

            if (FilterEvent(loggingEvent))
            {
                Enqueue(loggingEvent);
            }
        }
        private void Start()
        {
            if (!isClosing)
            {
                Thread thread = new Thread(LogMessages);
                thread.Start();
            }
        }
        private void LogMessages()
        {
            LoggingEvent loggingEvent;
            while (!isClosing || pendingEvents.Count != 0)
            {
                while (!DeQueue(out loggingEvent))
                {
                    manualResetEvent.WaitOne(10000);
                    //Thread.Sleep(2000);
                    if (isClosing)
                    {
                        break;
                    }
                }
                if (loggingEvent != null)
                {
                    try
                    {
                        base.Append(loggingEvent);
                    }
                    catch(Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("{0} - Error while writing log message[{1}] to database. {2}", DateTime.Now, loggingEvent.MessageObject, ex.Message);
                    }
                }
            }
            manualResetEvent.Set();
        }

        private void Enqueue(LoggingEvent loggingEvent)
        {
            pendingEvents.Enqueue(loggingEvent);
        }

        private bool DeQueue(out LoggingEvent loggingEvent)
        {
            if (pendingEvents.Count > 0)
            {
                return pendingEvents.TryDequeue(out loggingEvent);
            }
            else
            {
                loggingEvent = null;
                return false;
            }
        }

        protected override void OnClose()
        {
            isClosing = true;
            manualResetEvent.WaitOne();
            base.OnClose();
        }
    }
}
