using System;
using System.Diagnostics;
using System.Threading;
using StackExchange.Profiling;
using log4net.Appender;
using log4net.Core;

namespace MinProfileNhibernateLog4Net
{
    public class NhibernateProfilingAppender : AppenderSkeleton
    {
        private readonly ThreadLocal<IDisposable> timer = new ThreadLocal<IDisposable>();
        private string _buildingAnIdbcommandObjectForTheSqlstring = "Building an IDbCommand object for the SqlString";

        public bool Started { get; set; }


        public NhibernateProfilingAppender()
        {
            Console.WriteLine("hej hopp");
            Trace.WriteLine("Created");
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            //Sample output of what Nhibernate outputs and this code tries to "parse"
            //Building an IDbCommand object for the SqlString: INSERT INTO "RecipientList" (Name, Description) VALUES (?, ?); select last_insert_rowid()
            //Building an IDbCommand object for the SqlString: SELECT this_.Id as Id4_0_, this_.Name as Name4_0_, this_.Description as Descript3_4_0_ FROM "RecipientList" this_
            if (loggingEvent.LoggerName.Equals("NHibernate.AdoNet.AbstractBatcher"))
            {
               
                string msg = loggingEvent.MessageObject.ToString();

                if (msg.StartsWith(_buildingAnIdbcommandObjectForTheSqlstring))
                {
                    MiniProfiler profiler = MiniProfiler.Current;
                    timer.Value = profiler.Step(logText(msg));
                }
                //Sample of NHibernate ouptut when closing 
                //2011-06-30 10:23:23,515 [9] DEBUG NHibernate.AdoNet.AbstractBatcher - Closed IDbCommand, open IDbCommands: 0
                if ( msg.StartsWith("Closed IDbCommand"))
                {
                    if (timer.Value != null)
                    {
                        timer.Value.Dispose();

                        timer.Value = null;
                    }
                }
            }

          
        }

        /// <summary>
        /// Remove start "noise" (Building an...) to make a nicer log messgae for MiniProfiler
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string logText(string msg)
        {
            msg = msg.Substring(_buildingAnIdbcommandObjectForTheSqlstring.Length + 2 );
            if(msg.StartsWith("INSERT"))
            {
                return msg.Substring(0,msg.LastIndexOf('"')+1);
            }
            if(msg.StartsWith("SELECT"))
            {
                return "SELECT " + msg.Substring(msg.IndexOf("FROM"));
            }

             return msg;
        }

    }
}