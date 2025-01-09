//
// ASCOM.byHrast.Focuser Local COM Server
//
// This is the core of a managed COM Local Server, capable of serving
// multiple instances of multiple interfaces, within a single
// executable. This implementes the equivalent functionality of VB6
// which has been extensively used in ASCOM for drivers that provide
// multiple interfaces to multiple clients (e.g. Meade Telescope
// and Focuser) as well as hubs (e.g., POTH).
//
// Written by: Robert B. Denny (Version 1.0.1, 29-May-2007)
// Modified by Chris Rowland and Peter Simpson to allow use with multiple devices of the same type March 2011
//
//
using Microsoft.Win32;

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Utilities;

namespace ASCOM.LocalServer
{
	public static class Server
    {

        #region Variables

        private static uint _mainThreadId; // Stores the main thread's thread id.
        private static bool _startedByCOM; // True if server started by COM (-embedding)
        private static FrmMain _localServerMainForm = null; // Reference to our main form.
        private static int _driversInUseCount; // Keeps a count on the total number of objects alive.
        private static int _serverLockCount; // Keeps a lock count on this application.
        private static ArrayList _driverTypes; // Served COM object types
        private static ArrayList _classFactories; // Served COM object class factories
        private static string _localServerAppId = "{3af61642-cbf9-4eaf-aa27-b8d3fce79049}"; // Our AppId
        private static readonly Object _lockObject = new object(); // Counter lock object
        private static TraceLogger _tl; // TraceLogger for the local server (not the served driver, which has its own) - primarily to help debug local server issues
        private static Task _gcTask; // The garbage collection task
        private static CancellationTokenSource _gcTokenSource; // Token source used to end periodic garbage collection.

        #endregion

        #region Local Server entry point (main)

        /// <summary>
        /// Main server entry point
        /// </summary>
        /// <param name="args">Command line parameters</param>
        [STAThread]
        static void Main(string[] args)
        {
			// Uncomment the following lines to allow the Visual Studio Debugger to be 
			// attached to the server for debugging.

			//int procId = Process.GetCurrentProcess().Id; /// ISKLJUCEN DEBUGGER
			//MessageBox.Show( $"Attach the debugger to process #{procId} now." );

			// Create a trace logger for the local server.
			_tl = new TraceLogger("", "ASCOM.LocalServer")
            {
                Enabled = true // Enable to debug local server operation (not usually required). Drivers have their own independent trace loggers.
            };
            _tl.LogMessage("Main", $"Server started");

            // Load driver COM assemblies and get types, ending the program if something goes wrong.
            _tl.LogMessage("Main", $"Loading drivers");
            if (!PopulateListOfAscomDrivers()) return;

            // Process command line arguments e.g. to Register/Unregister drivers, ending the program if required.
            _tl.LogMessage("Main", $"Processing command-line arguments");
            if (!ProcessArguments(args)) return;

            // Initialize variables.
            _tl.LogMessage("Main", $"Initialising variables");
            _driversInUseCount = 0;
            _serverLockCount = 0;
            _mainThreadId = GetCurrentThreadId();
            Thread.CurrentThread.Name = "byHrast Local Server Thread";

            // Create and configure the local server host form that runs the Windows message loop required to support driver operation
            _tl.LogMessage("Main", $"Creating host form");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _localServerMainForm = new FrmMain();
            if (_startedByCOM) _localServerMainForm.WindowState = FormWindowState.Minimized;

            // Register the class factories of the served objects
            _tl.LogMessage("Main", $"Registering class factories");
            RegisterClassFactories();

            // Start the garbage collection thread.
            _tl.LogMessage("Main", $"Starting garbage collection");
            StartGarbageCollection(10000);
            _tl.LogMessage("Main", $"Garbage collector thread started");

            // Start the message loop to serialize incoming calls to the served driver COM objects.
            try
            {
                _tl.LogMessage("Main", $"Starting main form");
                Application.Run(_localServerMainForm);
                _tl.LogMessage("Main", $"Main form has ended");
            }
            finally
            {
                // Revoke the class factories immediately without waiting until the thread has stopped
                _tl.LogMessage("Main", $"Revoking class factories");
                RevokeClassFactories();
                _tl.LogMessage("Main", $"Class factories revoked");

                // Now stop the Garbage Collector thread.
                _tl.LogMessage("Main", $"Stopping garbage collector");
                StopGarbageCollection();
            }

            _tl.LogMessage("Main", $"Local server closing");
            _tl.Dispose();

        }

        #endregion

        #region Server Lock, Object Counting, and AutoQuit on COM start-up

        /// <summary>
        /// Returns the total number of objects alive currently. 
        /// </summary>
        public static int ObjectCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _driversInUseCount; // Return the object count
                }
            }
        }

        /// <summary>
        /// Performs a thread-safe incrementation of the object count. 
        /// </summary>
        /// <returns></returns>
        public static int IncrementObjectCount()
        {
            int newCount = Interlocked.Increment(ref _driversInUseCount); // Increment the object count.
            _tl.LogMessage("IncrementObjectCount", $"New object count: {newCount}");

            return newCount;
        }

        /// <summary>
        /// Performs a thread-safe decrementation the objects count.
        /// </summary>
        /// <returns></returns>
        public static int DecrementObjectCount()
        {
            int newCount = Interlocked.Decrement(ref _driversInUseCount); // Decrement the object count.
            _tl.LogMessage("DecrementObjectCount", $"New object count: {newCount}");

            return newCount;
        }

        /// <summary>
        /// Returns the current server lock count.
        /// </summary>
        public static int ServerLockCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _serverLockCount; // Return the lock count
                }
            }
        }

        /// <summary>
        /// Performs a thread-safe incrementation of the server lock count. 
        /// </summary>
        /// <returns></returns>
        public static int IncrementServerLockCount()
        {
            int newCount = Interlocked.Increment(ref _serverLockCount); // Increment the server lock count for this server.
            _tl.LogMessage("IncrementServerLockCount", $"New server lock count: {newCount}");

            return newCount;
        }

        /// <summary>
        /// Performs a thread-safe decrementation the server lock count.
        /// </summary>
        /// <returns></returns>
        public static int DecrementServerLockLock()
        {
            int newCount = Interlocked.Decrement(ref _serverLockCount); // Decrement the server lock count for this server.
            _tl.LogMessage("DecrementServerLockLock", $"New server lock count: {newCount}");
            return newCount;
        }

        /// <summary>
        /// Test whether the objects count and server lock count have both dropped to zero and, if so, terminate the application.
        /// </summary>
        /// <remarks>
        /// If the counts are zero, the application is terminated by posting a WM_QUIT message to the main thread's message loop, causing it to terminate and exit.
        /// </remarks>
        public static void ExitIf()
        {
            lock (_lockObject)
            {
                _tl.LogMessage("ExitIf", $"Object count: {ObjectCount}, Server lock count: {_serverLockCount}");
                if ((ObjectCount <= 0) && (ServerLockCount <= 0))
                {
                    if (_startedByCOM)
                    {
                        _tl.LogMessage("ExitIf", $"Server started by COM so shutting down the Windows message loop on the main process to end the local server.");

                        UIntPtr wParam = new UIntPtr(0);
                        IntPtr lParam = new IntPtr(0);
                        PostThreadMessage(_mainThreadId, 0x0012, wParam, lParam);
                    }
                }
            }
        }

        #endregion

        #region Dynamic Driver Assembly Loader

        /// <summary>
        /// Populates the list of ASCOM drivers by searching for driver classes within the local server executable.
        /// </summary>
        /// <returns>True if successful, otherwise False</returns>
        private static bool PopulateListOfAscomDrivers()
        {
            // Initialise the driver types list
            _driverTypes = new ArrayList();

            try
            {
                // Get the types contained within the local server assembly
                Assembly so = Assembly.GetExecutingAssembly(); // Get the local server assembly 
                Type[] types = so.GetTypes(); // Get the types in the assembly

                // Iterate over the types identifying those which are drivers
                foreach (Type type in types)
                {
                    _tl.LogMessage("PopulateListOfAscomDrivers", $"Found type: {type.Name}");

                    // Check to see if this type has the ServedClassName attribute, which indicates that this is a driver class.
                    object[] attrbutes = type.GetCustomAttributes(typeof(ServedClassNameAttribute), false);
                    if (attrbutes.Length > 0) // There is a ServedClassName attribute on this class so it is a driver
                    {
                        _tl.LogMessage("PopulateListOfAscomDrivers", $"  {type.Name} is a driver assembly");
                        _driverTypes.Add(type); // Add the driver type to the list
                    }
                }
                _tl.BlankLine();

                // Log discovered drivers
                _tl.LogMessage("PopulateListOfAscomDrivers", $"Found {_driverTypes.Count} drivers");
                foreach (Type type in _driverTypes)
                {
                    _tl.LogMessage("PopulateListOfAscomDrivers", $"Found Driver : {type.Name}");
                }
                _tl.BlankLine();
            }
            catch (Exception e)
            {
                _tl.LogMessageCrLf("PopulateListOfAscomDrivers", $"Exception: {e}");
                MessageBox.Show($"Failed to load served COM class assembly from within this local server - {e.Message}", "Rotator Simulator", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            return true;
        }

        #endregion

        #region COM Registration and Unregistration

        /// <summary>
        /// Register drivers contained in this local server. (Must run as Administrator.)
        /// </summary>
        /// <remarks>
        /// Do everything to register this for COM. Never use REGASM on this exe assembly! It would create InProcServer32 entries which would prevent proper activation!
        /// Using the list of COM object types generated during dynamic assembly loading, this method registers each driver for COM and registers it for ASCOM. 
        /// It also adds DCOM info for the local server itself, so it can be activated via an outbound connection from TheSky.
        /// </remarks>
        private static void RegisterObjects()
        {
            // Request administrator privilege if we don't already have it
            if (!IsAdministrator)
            {
                ElevateSelf("/register");
                return;
            }

            // If we reach here, we're running elevated

            // Initialise variables
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Attribute assemblyTitleAttribute = Attribute.GetCustomAttribute(executingAssembly, typeof(AssemblyTitleAttribute));
            string assemblyTitle = ((AssemblyTitleAttribute)assemblyTitleAttribute).Title;
            assemblyTitleAttribute = Attribute.GetCustomAttribute(executingAssembly, typeof(AssemblyDescriptionAttribute));
            string assemblyDescription = ((AssemblyDescriptionAttribute)assemblyTitleAttribute).Description;

            // Set the local server's DCOM/AppID information
            try
            {
                _tl.LogMessage("RegisterObjects", $"Setting local server's APPID");

                // Set HKCR\APPID\appid
                using (RegistryKey appIdKey = Registry.ClassesRoot.CreateSubKey($"APPID\\{_localServerAppId}"))
                {
                    appIdKey.SetValue(null, assemblyDescription);
                    appIdKey.SetValue("AppID", _localServerAppId);
                    appIdKey.SetValue("AuthenticationLevel", 1, RegistryValueKind.DWord);
                    appIdKey.SetValue("RunAs", "Interactive User", RegistryValueKind.String); // Added to ensure that only one copy of the local server runs if the user uses both elevated and non-elevated clients concurrently
                }

                // Set HKCR\APPID\exename.ext
                using (RegistryKey exeNameKey = Registry.ClassesRoot.CreateSubKey($"APPID\\{Application.ExecutablePath.Substring(Application.ExecutablePath.LastIndexOf('\\') + 1)}"))
                {
                    exeNameKey.SetValue("AppID", _localServerAppId);
                }
                _tl.LogMessage("RegisterObjects", $"APPID set successfully");
            }
            catch (Exception ex)
            {
                _tl.LogMessageCrLf("RegisterObjects", $"Setting AppID exception: {ex}");
                MessageBox.Show("Error while registering the server:\n" + ex.ToString(), "ASCOM.byHrast.Focuser", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            // Register each discovered driver
            foreach (Type driverType in _driverTypes)
            {
                _tl.LogMessage("RegisterObjects", $"Creating COM registration for {driverType.Name}");
                bool bFail = false;
                try
                {
                    // HKCR\CLSID\clsid
                    string clsId = Marshal.GenerateGuidForType(driverType).ToString("B");
                    string progId = Marshal.GenerateProgIdForType(driverType);
                    string deviceType = driverType.Name; // Generate device type from the Class name
                    _tl.LogMessage("RegisterObjects", $"Assembly title: {assemblyTitle}, ASsembly description: {assemblyDescription}, CLSID: {clsId}, ProgID: {progId}, Device type: {deviceType}");

                    using (RegistryKey clsIdKey = Registry.ClassesRoot.CreateSubKey($"CLSID\\{clsId}"))
                    {
                        clsIdKey.SetValue(null, progId);
                        clsIdKey.SetValue("AppId", _localServerAppId);
                        using (RegistryKey implementedCategoriesKey = clsIdKey.CreateSubKey("Implemented Categories"))
                        {
                            implementedCategoriesKey.CreateSubKey("{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}");
                        }

                        using (RegistryKey progIdKey = clsIdKey.CreateSubKey("ProgId"))
                        {
                            progIdKey.SetValue(null, progId);
                        }
                        clsIdKey.CreateSubKey("Programmable");

                        using (RegistryKey localServer32Key = clsIdKey.CreateSubKey("LocalServer32"))
                        {
                            localServer32Key.SetValue(null, Application.ExecutablePath);
                        }
                    }

                    // HKCR\CLSID\progid
                    using (RegistryKey progIdKey = Registry.ClassesRoot.CreateSubKey(progId))
                    {
                        progIdKey.SetValue(null, assemblyTitle);
                        using (RegistryKey clsIdKey = progIdKey.CreateSubKey("CLSID"))
                        {
                            clsIdKey.SetValue(null, clsId);
                        }
                    }

                    // Pull the display name from the ServedClassName attribute.
                    assemblyTitleAttribute = Attribute.GetCustomAttribute(driverType, typeof(ServedClassNameAttribute));
                    string chooserName = ((ServedClassNameAttribute)assemblyTitleAttribute).DisplayName ?? "MultiServer";
                    _tl.LogMessage("RegisterObjects", $"Registering {chooserName} ({driverType.Name}) in Profile");

                    using (var profile = new Profile())
                    {
                        profile.DeviceType = deviceType;
                        profile.Register(progId, chooserName);
                    }
                }
                catch (Exception ex)
                {
                    _tl.LogMessageCrLf("RegisterObjects", $"Driver registration exception: {ex}");
                    MessageBox.Show("Error while registering the server:\n" + ex.ToString(), "ASCOM.byHrast.Focuser", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    bFail = true;
                }

                // Stop processing drivers if something has gone wrong
                if (bFail) break;
            }
        }

        /// <summary>
        /// Unregister drivers contained in this local server. (Must run as administrator.)
        /// </summary>
        private static void UnregisterObjects()
        {
            // Request administrator privilege if we don't already have it
            if (!IsAdministrator)
            {
                ElevateSelf("/unregister");
                return;
            }

            // If we reach here, we're running elevated

            // Delete the Local Server's DCOM/AppID information
            Registry.ClassesRoot.DeleteSubKey($"APPID\\{_localServerAppId}", false);
            Registry.ClassesRoot.DeleteSubKey($"APPID\\{Application.ExecutablePath.Substring(Application.ExecutablePath.LastIndexOf('\\') + 1)}", false);

            // Delete each driver's COM registration
            foreach (Type driverType in _driverTypes)
            {
                _tl.LogMessage("UnregisterObjects", $"Removing COM registration for {driverType.Name}");

                string clsId = Marshal.GenerateGuidForType(driverType).ToString("B");
                string progId = Marshal.GenerateProgIdForType(driverType);

                // Remove ProgID entries
                Registry.ClassesRoot.DeleteSubKey($"{progId}\\CLSID", false);
                Registry.ClassesRoot.DeleteSubKey(progId, false);

                // Remove CLSID entries
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsId}\\Implemented Categories\\{{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}}", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsId}\\Implemented Categories", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsId}\\ProgId", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsId}\\LocalServer32", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsId}\\Programmable", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsId}", false);

                // Uncomment the following lines to remove ASCOM Profile information when unregistering.
                // Unregistering often occurs during version upgrades and, if the code below is enabled, will result in loss of all device configuration during the upgrade.
                // For this reason, enabling this capability is not recommended.

                //try
                //{
                //    TL.LogMessage("UnregisterObjects", $"Deleting ASCOM Profile registration for {driverType.Name} ({progId})");
                //    using (var profile = new Profile())
                //    {
                //        profile.DeviceType = driverType.Name;
                //        profile.Unregister(progId);
                //    }
                //}
                //catch (Exception) { }
            }
        }

        /// <summary>
        /// Test whether the session is running with elevated credentials
        /// </summary>
        private static bool IsAdministrator
        {
            get
            {
                WindowsIdentity userIdentity = WindowsIdentity.GetCurrent();
                WindowsPrincipal userPrincipal = new WindowsPrincipal(userIdentity);
                bool isAdministrator = userPrincipal.IsInRole(WindowsBuiltInRole.Administrator);

                _tl.LogMessage("IsAdministrator", isAdministrator.ToString());
                return isAdministrator;
            }
        }

        /// <summary>
        /// Elevate privileges by re-running ourselves with elevation dialogue
        /// </summary>
        /// <param name="argument">Argument to pass to ourselves</param>
        private static void ElevateSelf(string argument)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.Arguments = argument;
            processStartInfo.WorkingDirectory = Environment.CurrentDirectory;
            processStartInfo.FileName = Application.ExecutablePath;
            processStartInfo.Verb = "runas";
            try
            {
                _tl.LogMessage("IsAdministrator", $"Starting elevated process");
                Process.Start(processStartInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                _tl.LogMessage("IsAdministrator", $"The ASCOM.byHrast.Focuser was not " + (argument == "/register" ? "registered" : "unregistered because you did not allow it."));
                MessageBox.Show("The ASCOM.byHrast.Focuser was not " + (argument == "/register" ? "registered" : "unregistered because you did not allow it.", "ASCOM.byHrast.Focuser", MessageBoxButtons.OK, MessageBoxIcon.Warning));
            }
            catch (Exception ex)
            {
                _tl.LogMessageCrLf("IsAdministrator", $"Exception: {ex}");
                MessageBox.Show(ex.ToString(), "ASCOM.byHrast.Focuser", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            return;
        }

        #endregion

        #region Class Factory Support

        /// <summary>
        /// Register the class factories of drivers that this local server serves.
        /// </summary>
        /// <remarks>This requires the class factory name to be equal to the served class name + "ClassFactory".</remarks>
        /// <returns>True if there are no errors, otherwise false.</returns>
        private static bool RegisterClassFactories()
        {
            _tl.LogMessage("RegisterClassFactories", $"Registering class factories");
            _classFactories = new ArrayList();
            foreach (Type driverType in _driverTypes)
            {
                _tl.LogMessage("RegisterClassFactories", $"  Creating class factory for: {driverType.Name}");
                ClassFactory factory = new ClassFactory(driverType); // Use default context & flags
                _classFactories.Add(factory);

                _tl.LogMessage("RegisterClassFactories", $"  Registering class factory for: {driverType.Name}");
                if (!factory.RegisterClassObject())
                {
                    _tl.LogMessage("RegisterClassFactories", $"  Failed to register class factory for " + driverType.Name);
                    MessageBox.Show("Failed to register class factory for " + driverType.Name, "ASCOM.byHrast.Focuser", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return false;
                }
                _tl.LogMessage("RegisterClassFactories", $"  Registered class factory OK for: {driverType.Name}");
            }

            _tl.LogMessage("RegisterClassFactories", $"Making class factories live");
            ClassFactory.ResumeClassObjects(); // Served objects now go live
            _tl.LogMessage("RegisterClassFactories", $"Class factories live OK");
            return true;
        }

        /// <summary>
        /// Revoke the class factories
        /// </summary>
        private static void RevokeClassFactories()
        {
            _tl.LogMessage("RevokeClassFactories", $"Suspending class factories");
            ClassFactory.SuspendClassObjects(); // Prevent race conditions
            _tl.LogMessage("RevokeClassFactories", $"Class factories suspended OK");

            foreach (ClassFactory factory in _classFactories)
            {
                factory.RevokeClassObject();
            }
        }

        #endregion

        #region Command line argument processing

        /// <summary>
        ///Process the command-line arguments returning true to continue execution or false to terminate the application immediately.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool ProcessArguments(string[] args)
        {
            bool returnStatus = true;

            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "-embedding":
                        _tl.LogMessage("ProcessArguments", $"Started by COM: {args[0]}");
                        _startedByCOM = true; // Indicate COM started us and continue
                        returnStatus = true; // Continue on return
                        break;

                    case "-register":
                    case @"/register":
                    case "-regserver": // Emulate VB6
                    case @"/regserver":
                        _tl.LogMessage("ProcessArguments", $"Registering drivers: {args[0]}");
                        RegisterObjects(); // Register each served object
                        returnStatus = false; // Terminate on return
                        break;

                    case "-unregister":
                    case @"/unregister":
                    case "-unregserver": // Emulate VB6
                    case @"/unregserver":
                        _tl.LogMessage("ProcessArguments", $"Unregistering drivers: {args[0]}");
                        UnregisterObjects(); //Unregister each served object
                        returnStatus = false; // Terminate on return
                        break;

                    default:
                        _tl.LogMessage("ProcessArguments", $"Unknown argument: {args[0]}");
                        MessageBox.Show("Unknown argument: " + args[0] + "\nValid are : -register, -unregister and -embedding", "ASCOM.byHrast.Focuser", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                }
            }
            else
            {
                _startedByCOM = false;
                _tl.LogMessage("ProcessArguments", $"No arguments supplied");
            }

            return returnStatus;
        }

        #endregion

        #region Garbage collection support

        /// <summary>
        /// Start a garbage collection thread that can be cancelled
        /// </summary>
        /// <param name="interval">Frequency of garbage collections</param>
        private static void StartGarbageCollection(int interval)
        {
            // Create the garbage collection object
            _tl.LogMessage("StartGarbageCollection", $"Creating garbage collector with interval: {interval} seconds");
            GarbageCollection garbageCollector = new GarbageCollection(interval);

            // Create a cancellation token and start the garbage collection task 
            _tl.LogMessage("StartGarbageCollection", $"Starting garbage collector thread");
            _gcTokenSource = new CancellationTokenSource();
            _gcTask = Task.Factory.StartNew(() => garbageCollector.GCWatch(_gcTokenSource.Token), _gcTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _tl.LogMessage("StartGarbageCollection", $"Garbage collector thread started OK");
        }

        /// <summary>
        /// Stop the garbage collection task by sending it the cancellation token and wait for the task to complete
        /// </summary>
        private static void StopGarbageCollection()
        {
            // Signal the garbage collector thread to stop
            _tl.LogMessage("StopGarbageCollection", $"Stopping garbage collector thread");
            _gcTokenSource.Cancel();
            _gcTask.Wait();
            _tl.LogMessage("StopGarbageCollection", $"Garbage collector thread stopped OK");

            // Clean up
            _gcTask = null;
            _gcTokenSource.Dispose();
            _gcTokenSource = null;
        }

        #endregion

        #region kernel32.dll and user32.dll functions

        // Post a Windows Message to a specific thread (identified by its thread id). Used to post a WM_QUIT message to the main thread in order to terminate this application.)
        [DllImport("user32.dll")]
        static extern bool PostThreadMessage(uint idThread, uint Msg, UIntPtr wParam, IntPtr lParam);

        // Obtain the thread id of the calling thread allowing us to post the WM_QUIT message to the main thread.
        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        #endregion
    }
}
