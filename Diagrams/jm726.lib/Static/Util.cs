#region Namespace imports

using System;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.IO;

#endregion

namespace common {
    /// <summary>
    ///   A static class which provides utility methods for functions commonly used throughout the system.
    /// 
    ///   The methods provided deal with threading. They essentially provide wrappers so calls to Thread.Sleep
    ///   and Monitor.Wait are wrapped in the appropriate lock and try catch statements. This class also provides
    ///   a method which works with the Debug class to hang waiting for all threads which have been marked as started but
    ///   not marked as stopped to Stop.
    /// </summary>
    public static class Util {
        /// <summary>
        ///   Counts the number of attempts that have been made to join with a thread
        ///   Increments every time an attempt is made to join with a thread
        ///   Decriments every a join attempt is successful
        ///   Useful for highlighting joins which don't return
        /// </summary>
        private static int _joinCount;

        /// <summary>
        /// The configuration events that was loaded before the SwitchConfig method was called. Used so the configuration can be reset.
        /// </summary>
        private static String _oldConfig = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
/*

        public static Assembly AssemblyResolver(object sender, ResolveEventArgs args) {
            DB.Print("I am code that is running, resolving " + args.Name);
            try {
                Assembly assembly = Assembly.LoadFile(args.Name);
                if (assembly != null) {
                    DB.Print("Simple resolve worked");
                    return assembly;
                }
            }
            catch {}
            return null;
        }
*/



        /// <summary>
        ///   Do a thread wait for the specified time or until a specified target is notified
        ///   If neither target nor time is set nothing will happen
        ///   If _cont is false nothing will happen
        ///   If time is set to 0 will wait for 1000000ms
        /// </summary>
        /// <param name = "target">[optional]Target to wait on. If target is notified wait will return</param>
        /// <param name = "time">[optional]Time to wait, in milliseconds. If target is set will return before time if target is notified. If set to 0 will wait for 1000000ms</param>
        /// <param name = "_cont">[optional]Whether the continue test set by the caller is true. Won't wait if this is set to false</param>
        public static void Wait(int time = -1, bool cont = true, object target = null) {
            //Don't bother pausing if !_cont or if none of the possible wait variables are set
            if (!cont || (time <= 0 && target == null)) {
                //PrintError("Wait - sleep", "Neither timeout nor target set");
                return;
            }
            try {
                if (target == null)
                    Thread.Sleep(time);
                else {
                    lock (target)
                        if (time > 0)
                            Monitor.Wait(target, time);
                        else if (time == 0)
                            Monitor.Wait(target, 1000000);
                        else
                            Monitor.Wait(target);
                }
            }
            catch (ThreadInterruptedException e) {
                PrintError("Wait - sleep", "Thread Interrupted");
            }
            catch (ThreadStateException e) {
                PrintError("Wait - sleep",
                           "Thread in Invalid State (" + Thread.CurrentThread.ThreadState.ToString() + ")");
            }
            catch (ThreadAbortException e) {
                PrintError("Wait - sleep", "Thread Aborting");
            }
            catch (Exception e) {
                PrintError("Wait - sleep", e);
            }
        }


        /// <summary>
        ///   Wake up all threads waiting on the target Object
        /// </summary>
        /// <param name = "target">The Object to wake threads waiting for</param>
        public static void Wake(Object target) {
            if (target == null) {
                PrintError("Wake", "Null Target");
                return;
            }
            try {
                lock (target)
                    Monitor.PulseAll(target);
            }
            catch (SynchronizationLockException e) {
                PrintError("Wait - wait timeout", "Lock not owned on " + target);
            }
            catch (ThreadInterruptedException e) {
                PrintError("Wake", "Thread Interrupted");
            }
            catch (ThreadStateException e) {
                PrintError("Wake", "Thread in Invalid State (" + Thread.CurrentThread.ThreadState.ToString() + ")");
            }
            catch (ThreadAbortException e) {
                PrintError("Wake", "Thread Aborting");
            }
            catch (Exception e) {
                PrintError("Wait - sleep", e);
            }
        }

        /// <summary>
        ///   Join with a thread that is currently running
        /// </summary>
        /// <param name = "t"></param>
        public static void Join(Thread t) {
            if (t == null || !t.IsAlive)
                return;
            try {
                //TODO Log thread events
                //DB.Print(++_joinCount + " - Joining with " + t.Name, Levels.THREADS);
                //if (t.Join(10000))
                //    DB.Print(--_joinCount + " - Joined with " + t.Name, Levels.THREADS);
                //else
                //    DB.Print(_joinCount + " - Failed to join with " + t.Name, Levels.THREADS);
            }
            catch (ThreadInterruptedException e) {
                PrintError("Wake", "Thread Interrupted");
            }
            catch (ThreadStateException e) {
                PrintError("Wake", "Thread in Invalid State (" + Thread.CurrentThread.ThreadState.ToString() + ")");
            }
            catch (ThreadAbortException e) {
                PrintError("Wake", "Thread Aborting");
            }
            catch (Exception e) {
                PrintError("Wait - sleep", e);
            }
        }

        /// <summary>
        ///   Blocks until all of the threads the database Object is tracking are closed down
        ///   or it times out at 1 minute
        /// </summary>
        public static void WaitForThreads(int timeout) {
            //TODO re-implement the thread counting mechanism
            //DateTime start = DateTime.Now;
            //bool cont = true;
            //if (DB.ThreadsRemaining)
            //    DB.Print("Waiting on Threads", Levels.ALWAYS, Levels.SHUTDOWN);
            //while (DB.ThreadsRemaining && cont) {
            //    Wait(50);
            //    cont = DateTime.Now.Subtract(start).TotalMilliseconds < timeout;
            //}
        }

        /// <summary>
        ///   Utility method for printing an error if it occured while running one of the functions
        /// </summary>
        /// <param name = "operation">The method which the error occured in</param>
        /// <param name = "_prim">The error</param>
        private static void PrintError(string operation, Exception e) {
            //TODO reimplement logging
            //PrintError(operation, e.GetType().Name + " (" + e.Message + ")");
        }

        /// <summary>
        ///   Print out a message about an error that occured performing one of the functions
        /// </summary>
        /// <param name = "operation">The operation which threw the error</param>
        /// <param name = "message">The message to print</param>
        private static void PrintError(string operation, string message) {
            //TODO reimplement logging
            //DB.Print(("(" + operation + ")").PadRight(20) + " Error - " + message, Levels.DEBUG);
        }

        /// <summary>
        /// Switch the config events that the application is using. Mainly useful for debugging.
        /// </summary>
        /// <param name="configFile">The name of the config events to switch to</param>
        public static void SwitchConfig(String configFile) {
            Console.WriteLine("Switching to " + Path.Combine(Environment.CurrentDirectory, configFile));
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", Path.Combine(Environment.CurrentDirectory, configFile));
            FieldInfo fiInit = typeof (ConfigurationManager).GetField("s_initState",
                                                                      BindingFlags.NonPublic | BindingFlags.Static);
            if (fiInit != null)
                fiInit.SetValue(null, 0);

            ConfigurationManager.RefreshSection("appSettings");
        }

        /// <summary>
        /// Reset the config events back to the original config events that was in place before the switch.s
        /// </summary>
        public static void ResetConfig() {
            if (_oldConfig == null)
                return;
            SwitchConfig(_oldConfig);
            _oldConfig = null;
        }
    }
}