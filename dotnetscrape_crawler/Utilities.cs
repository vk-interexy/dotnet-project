using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
//using Tidy.Core;

namespace dotnetscrape_crawler
{
    
    public static class Utilities
    {


        //public static string CleanUpHTML(string badHtmlString)
        //{
        //    HtmlTidy tidy = new HtmlTidy();

        //    tidy.Options.MakeClean = true;
        //    tidy.Options.SmartIndent = true;
        //    tidy.Options.CharEncoding = CharEncoding.Utf8;
        //    tidy.Options.LiteralAttribs = true;

        //    //Clean bad html using TIDY
        //    // http://sourceforge.net/projects/tidynet/
            
        //    MemoryStream input = new MemoryStream();
        //    MemoryStream output = new MemoryStream();
        //    byte[] badHtml = Encoding.UTF8.GetBytes(badHtmlString);
        //    input.Write(badHtml, 0, badHtml.Length);
        //    input.Position = 0;
        //    //TidyMessageCollection tidyMsg = new TidyMessageCollection();
        //    tidy.Parse(input, output);
        //    return Encoding.UTF8.GetString(output.ToArray());

        //    ///* Set the options you want */
        //    //tidy.Options. = true;
        //    //tidy.Options.XmlOut = true;
        //    //tidy.Options.MakeClean = true;
        //    //tidy.Options.CharEncoding = Tidy.Core.CharEncoding.Utf8;

        //    ///* Declare the parameters that is needed */
        //    //System.IO.MemoryStream input = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(strHtml));
        //    //System.IO.MemoryStream output = new System.IO.MemoryStream();
        //    //tidy.Parse(input, output, new TidyMessageCollection());

        //    //return System.Text.Encoding.UTF8.GetString(output.ToArray());



        //}

        public static string GenerateWebRequestId()
        {
            return $"%-%{Guid.NewGuid()}%-%";
        }

        /// <summary>
        /// Starts the given tasks and waits for them to complete. This will run, at most, the specified number of tasks in parallel.
        /// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
        /// </summary>
        /// <param name="tasksToRun">The tasks to run.</param>
        /// <param name="maxActionsToRunInParallel">The maximum number of tasks to run in parallel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void StartAndWaitAllThrottled(IEnumerable<Task> tasksToRun, int maxActionsToRunInParallel, CancellationToken cancellationToken = new CancellationToken())
        {
            StartAndWaitAllThrottled(tasksToRun, maxActionsToRunInParallel, -1, cancellationToken);
        }

        /// <summary>
        /// Starts the given tasks and waits for them to complete. This will run the specified number of tasks in parallel.
        /// <para>NOTE: If a timeout is reached before the Task completes, another Task may be started, potentially running more than the specified maximum allowed.</para>
        /// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
        /// </summary>
        /// <param name="tasksToRun">The tasks to run.</param>
        /// <param name="maxActionsToRunInParallel">The maximum number of tasks to run in parallel.</param>
        /// <param name="timeoutInMilliseconds">The maximum milliseconds we should allow the max tasks to run in parallel before allowing another task to start. Specify -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void StartAndWaitAllThrottled(IEnumerable<Task> tasksToRun, int maxActionsToRunInParallel, int timeoutInMilliseconds, CancellationToken cancellationToken = new CancellationToken())
        {
            // Convert to a list of tasks so that we don't enumerate over it multiple times needlessly.
            var tasks = tasksToRun.ToList();

            using (var throttler = new SemaphoreSlim(maxActionsToRunInParallel))
            {
                var postTaskTasks = new List<Task>();

                // Have each task notify the throttler when it completes so that it decrements the number of tasks currently running.
                tasks.ForEach(t => postTaskTasks.Add(t.ContinueWith(tsk => throttler.Release())));

                // Start running each task.
                foreach (var task in tasks)
                {
                    // Increment the number of tasks currently running and wait if too many are running.
                    throttler.Wait(timeoutInMilliseconds, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    task.Start();
                }

                // Wait for all of the provided tasks to complete.
                // We wait on the list of "post" tasks instead of the original tasks, otherwise there is a potential race condition where the throttler&#39;s using block is exited before some Tasks have had their "post" action completed, which references the throttler, resulting in an exception due to accessing a disposed object.
                Task.WaitAll(postTaskTasks.ToArray(), cancellationToken);
            }
        }

        public static void LogDebug(string message)
        {
            if (LogLevel.DEBUG >= Config.LogLevel)
            {
                Log("DEBUG", message);
            }
        }

        public static void LogInfo(string message)
        {
            if (LogLevel.INFO >= Config.LogLevel)
            {
                Log("INFO", message);
            }
        }

        public static void LogError(string message)
        {
            if (LogLevel.ERROR >= Config.LogLevel)
            {
                Log("ERROR", message);
            }
        }

        private static void Log(string level, string message)
        {
            var now = DateTime.Now;
            Console.WriteLine($"[{level}][{now:yyyy-MM-dd HH:mm:ss.fff}]{message} - [*** Total Parts Added So Far: {DOTNETClient.totalPartsAdded} | Parts Updated: {DOTNETClient.totalPartsUpdated} | Parts Cached: {DOTNETClient.totalPartsCached} ***]");
        }
    }
}
