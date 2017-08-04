using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using oracle.kv.client.config;
using oracle.kv.client.log;
namespace oracle.kv.client {
    /// <summary>
    /// Launches a process in localhost.  
    /// </summary>
    internal class ProcessLauncher {
        Process process;
        string Signal;
        AutoResetEvent StartSignalSeen { get; set; }
        bool Started;
        int TimeoutMs;
        Logger Logger;
        public static int ProcessLaunched = 0;
        public static int ProcessKilled = 0;

        /// <summary>
        /// Launches a process in the same host.
        /// </summary>    
        /// <remarks>
        /// Launching a process involves two stages: i) physically starting
        /// an executable with given configuration and ii) ensuring that 
        /// executable has initialized propperly within a given timeout.
        ///
        /// This method spawns a process and monitors output and error 
        /// stream of spawned process for a text 'signal' to appear. 
        /// If a 'start signal' is not received in error/output stream of the
        /// spawned process within a timeout period,
        /// then considers that spawned process can not be 
        /// started and raises an exception.
        /// </remarks>
        /// <param name="executable">Executable to launch the process</param>
        /// <param name="args">Arguments to the process to be launched</param>
        /// <param name="timeoutMs">Time limit to launch the process</param>
        /// <param name="startSignal">remote process output to 
        /// signal that process has started successfully</param>
        public Process Launch(string executable, string args, int timeoutMs,
            string startSignal, Logger logger) {

            Logger = logger;

            Signal = startSignal;
            TimeoutMs = timeoutMs;
            StartSignalSeen = new AutoResetEvent(false);
            var Info = new ProcessStartInfo();
            Info.FileName = executable;
            Info.Arguments = args;
            Info.UseShellExecute = false;
            Info.CreateNoWindow = true;
            Info.ErrorDialog = false;
            Info.RedirectStandardOutput = true;
            Info.RedirectStandardError = true;

            process = Process.Start(Info);
            string command = Info.FileName + " " + Info.Arguments;
            Logger.Trace("Launching " + command);

            Monitor(process);
            StartTimer(timeoutMs);

            // blocks the calling thread. CheckForSignal method will set the
            // wait handle when remote process signals
            long startTime = DateTime.Now.Ticks;
            Stopwatch stopWatch = new Stopwatch();
            try {
                Logger.Trace("waiting for process to start in " + TimeoutMs + " ms...");
                StartSignalSeen.WaitOne(TimeoutMs);
            } catch (Exception ex) {
                killProcess(process);
                throw new ArgumentException("can not start process with"
                    + " command [" + command + "]", ex);
            }
            stopWatch.Stop();
            if (Started) {
                ProcessLaunched++;
                return process;
            } else {
                string msg = "can not start process with"
                     + " command [" + command + "] in "
                     + stopWatch.ElapsedMilliseconds + " ms";
                Logger.Trace(msg);
                killProcess(process);
                throw new ArgumentException(msg);
            }
        }

        void Monitor(Process p) {
            p.OutputDataReceived += new DataReceivedEventHandler(CheckForSignal);
            p.ErrorDataReceived += new DataReceivedEventHandler(CheckForSignal);
            p.OutputDataReceived += new DataReceivedEventHandler(LogRemote);
            p.ErrorDataReceived += new DataReceivedEventHandler(LogRemote);

            new Task(() => {
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
            }).Start();
        }

        /**
         * Receives remote process data. If data contains
         * a signal then considers that the remote process has started. 
         */
        void CheckForSignal(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) {
                Logger.Trace(e.Data);
                Started = e.Data.Contains(Signal);
                if (Started) {
                    StartSignalSeen.Set();
                }
                if (e.Data.Contains("ERROR")) {
                    killProcess(process);
                }
            }
        }

        void LogRemote(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) {
                Logger.Info(e.Data);
            }
        }

        /// <summary>
        /// Runs a  this instance.
        /// </summary>
        void StartTimer(int timeoutMs) {
            new Thread(new ParameterizedThreadStart((o) => {
                Thread.Sleep(timeoutMs);
                StartSignalSeen.Set();
            }))
            .Start();
        }

        internal void killProcess(Process p) {
            if (p == null || p.HasExited) return;
            try {
                p.Kill();
                ProcessKilled++;
            } catch (Exception) {
                Console.WriteLine("can not kill process " + p);
            }

        }
    }
}
