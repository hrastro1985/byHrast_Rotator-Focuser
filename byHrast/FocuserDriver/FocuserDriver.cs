//tabs=4
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Focuser driver for byHrast
//
// Description:	 <To be completed by driver developer>
//
// Implements:	ASCOM Focuser interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using ASCOM.DeviceInterface;
using ASCOM.Utilities;

using ASCOM.LocalServer;

namespace ASCOM.byHrast.Focuser
{
	//
	// Your driver's DeviceID is ASCOM.byHrast.Focuser
	//
	// The Guid attribute sets the CLSID for ASCOM.byHrast.Focuser
	// The ClassInterface/None attribute prevents an empty interface called
	// _byHrast from being created and used as the [default] interface
	//
	// TODO Replace the not implemented exceptions with code to implement the function or
	// throw the appropriate ASCOM exception.
	//

	/// <summary>
	/// ASCOM Focuser Driver for byHrast.
	/// </summary>
	[ComVisible( true )]
	[Guid( "85090b01-8e22-4cb8-8292-3a3b8c66ed9a" )]
	[ProgId( "ASCOM.byHrast.Focuser" )]
	[ServedClassName( "Focuser by Hrast" )] // Driver description that appears in the Chooser, customise as required
	[ClassInterface( ClassInterfaceType.None )]
	public class Focuser : DeviceDriverBase, IFocuserV3
	{
		// Constants used for Profile persistence
		//internal const string comPortProfileName = "COM Port";
		internal const string _traceStateProfileName = "Trace Level";
		internal const string _traceStateDefault = "true";

		//internal static string _driverID; // ASCOM DeviceID (COM ProgID) for this driver, the value is retrieved from the ServedClassName attribute in the class initialiser.
		internal static string _driverDescription; // The value is retrieved from the ServedClassName attribute in the class initialiser.
		internal static bool _connectedState; // variable to hold the connected state
		internal static Util _utilities; // Private variable to hold an ASCOM Utilities object
		internal static AstroUtils _astroUtilities; // Variable to hold an ASCOM AstroUtilities object to provide the Range method
		internal static TraceLogger _tl; // Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)

		/// <summary>
		/// Initializes a new instance of the <see cref="byHrast"/> class. Must be public to successfully register for COM.
		/// </summary>
		public Focuser()
		{
			try
			{
				// Pull the ProgID from the ProgID class attribute.
				Attribute attr = Attribute.GetCustomAttribute( this.GetType(), typeof( ProgIdAttribute ) );
				_driverID = ( (ProgIdAttribute)attr ).Value ?? "PROGID NOT SET!";  // Get the driver ProgIDfrom the ProgID attribute.

				// Pull the display name from the ServedClassName class attribute.
				attr = Attribute.GetCustomAttribute( this.GetType(), typeof( ServedClassNameAttribute ) );
				_driverDescription = ( (ServedClassNameAttribute)attr ).DisplayName ?? "DISPLAY NAME NOT SET!";  // Get the driver description that displays in the ASCOM Chooser from the ServedClassName attribute.

				_tl = new TraceLogger( "", "ASCOM.byHrast.Focuser" );
				ReadProfile(); // Read device configuration from the ASCOM Profile store, including the trace state

				_tl.LogMessage( "Focuser", "Starting initialisation" );
				_tl.LogMessage( "Focuser", $"ProgID: {_driverID}, Description: {_driverDescription}" );

				_connectedState = false; // Initialise connected to false
				_utilities = new Util(); //Initialise util object
				_astroUtilities = new AstroUtils(); // Initialise astro-utilities object

				//TODO: Implement your additional construction here

				_tl.LogMessage( "Focuser", "Completed initialisation" );
			}
			catch ( Exception ex )
			{
				_tl.LogMessageCrLf( "Focuser", $"Initialisation exception: {ex}" );
				MessageBox.Show( $"{ex.Message}", "Exception creating ASCOM.byHrast.Focuser", MessageBoxButtons.OK, MessageBoxIcon.Error );
			}

		}

		// PUBLIC COM INTERFACE IFocuserV3 IMPLEMENTATION

		#region Common properties and methods.

		/// <summary>
		/// Displays the Setup Dialogue form.
		/// If the user clicks the OK button to dismiss the form, then
		/// the new settings are saved, otherwise the old values are reloaded.
		/// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
		/// </summary>
		public void SetupDialog()
		{
			// consider only showing the setup dialogue if not connected
			// or call a different dialogue if connected
			if ( IsConnected )
				MessageBox.Show( "Already connected, just press OK" );

			using ( SetupDialogForm F = new SetupDialogForm( _tl ) )
			{
				var result = F.ShowDialog();

				if ( result == DialogResult.OK )
				{
					WriteProfile(); // Persist device configuration values to the ASCOM Profile store
				}
			}
		}

		public ArrayList SupportedActions
		{
			get
			{
				_tl.LogMessage( "SupportedActions Get", "Returning empty arraylist" );

				return new ArrayList();
			}
		}

		public string Action( string actionName, string actionParameters )
		{
			LogMessage( "", "Action {0}, parameters {1} not implemented", actionName, actionParameters );

			throw new ActionNotImplementedException( "Action " + actionName + " is not implemented by this driver" );
		}

		public void CommandBlind( string command, bool raw )
		{
			CheckConnected( "CommandBlind" );
			// TODO The optional CommandBlind method should either be implemented OR throw a MethodNotImplementedException
			// If implemented, CommandBlind must send the supplied command to the mount and return immediately without waiting for a response

			throw new MethodNotImplementedException( "CommandBlind" );
		}

		public bool CommandBool( string command, bool raw )
		{
			CheckConnected( "CommandBool" );
			// TODO The optional CommandBool method should either be implemented OR throw a MethodNotImplementedException
			// If implemented, CommandBool must send the supplied command to the mount, wait for a response and parse this to return a True or False value

			// string retString = CommandString(command, raw); // Send the command and wait for the response
			// bool retBool = XXXXXXXXXXXXX; // Parse the returned string and create a boolean True / False value
			// return retBool; // Return the boolean value to the client

			throw new MethodNotImplementedException( "CommandBool" );
		}

		public string CommandString( string command, bool raw )
		{
			CheckConnected( "CommandString" );
			// TODO The optional CommandString method should either be implemented OR throw a MethodNotImplementedException
			// If implemented, CommandString must send the supplied command to the mount and wait for a response before returning this to the client

			throw new MethodNotImplementedException( "CommandString" );
		}

		public void Dispose()
		{
			// Clean up the trace logger and util objects
			_tl.Enabled = false;
			_tl.Dispose();
			_tl = null;
			_utilities.Dispose();
			_utilities = null;
			_astroUtilities.Dispose();
			_astroUtilities = null;
		}

		public bool Connected
		{
			get
			{
				LogMessage( "Connected", "Get {0}", IsConnected );
				return IsConnected;
			}
			set
			{
				_tl.LogMessage( "Connected", "Set {0}", value );
				if ( value == IsConnected )
					return;

				if ( value )
				{
					_connectedState = true;
					LogMessage( "Connected Set", "Connecting to port {0}", SerialPortName );
					// TODO connect to the device
					SharedResources.ConnectToDevice( SerialPortName );
				}
				else
				{
					_connectedState = false;
					LogMessage( "Connected Set", "Disconnecting from port {0}", SerialPortName );
					// TODO disconnect from the device
				}
			}
		}

		public string Description
		{
			// TODO customise this device description
			get
			{
				_tl.LogMessage( "Description Get", _driverDescription );
				return _driverDescription;
			}
		}

		public string DriverInfo
		{
			get
			{
				Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				// TODO customise this driver description
				string driverInfo = "Focuser byHrast";
				_tl.LogMessage( "DriverInfo Get", driverInfo );
				return driverInfo;
			}
		}

		public string DriverVersion
		{
			get
			{
				Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				string driverVersion = String.Format( CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor );
				_tl.LogMessage( "DriverVersion Get", driverVersion );
				return driverVersion;
			}
		}

		public short InterfaceVersion
		{
			// set by the driver wizard
			get
			{
				LogMessage( "InterfaceVersion Get", "3" );
				return Convert.ToInt16( "3" );
			}
		}

		public string Name
		{
			get
			{
				string name = "Focuser byHrast";
				_tl.LogMessage( "Name Get", name );
				return name;
			}
		}

		#endregion

		#region IFocuser Implementation

		private int focuserPosition = 0; // Class level variable to hold the current focuser position
		private const int focuserSteps = 100000;

		public bool Absolute
		{
			get
			{
				_tl.LogMessage( "Absolute Get", true.ToString() );
				return true; // This is an absolute focuser
			}
		}

		public void Halt()
		{
			_tl.LogMessage( "Halt", "");
			SharedResources.SendMessage("FQ"); // Set the focuser position
		}

		public bool IsMoving
		{
			get
			{
				_tl.LogMessage( "IsMoving Get", true.ToString() );
				if (SharedResources.SendMessage("FM") == "0")
					return false;
				else return true;
			}
		}

		public bool Link
		{
			get
			{
				_tl.LogMessage( "Link Get", this.Connected.ToString() );
				return this.Connected; // Direct function to the connected method, the Link method is just here for backwards compatibility
			}
			set
			{
				_tl.LogMessage( "Link Set", value.ToString() );
				this.Connected = value; // Direct function to the connected method, the Link method is just here for backwards compatibility
			}
		}

		public int MaxIncrement
		{
			get
			{
				_tl.LogMessage( "MaxIncrement Get", focuserSteps.ToString() );
				return focuserSteps; // Maximum change in one move
			}
		}

		public int MaxStep
		{
			get
			{
				_tl.LogMessage( "MaxStep Get", focuserSteps.ToString() );
				return focuserSteps; // Maximum extent of the focuser, so position range is 0 to 10,000
			}
		}

		public void Move( int Position )
		{
			_tl.LogMessage( "Move", Position.ToString() );
			SharedResources.SendMessage("FA" + (Position - 50000).ToString()); // Set the focuser position
			}

		public int Position
		{
			get
			{

				return focuserPosition = Int16.Parse(SharedResources.SendMessage("FP")) + 50000; // Return the focuser position
			}
		}

		public double StepSize
		{
			get
			{
				_tl.LogMessage( "StepSize Get", "Not implemented" );
				throw new PropertyNotImplementedException( "StepSize", false );
			}
		}

		public bool TempComp
		{
			get
			{
				_tl.LogMessage( "TempComp Get", false.ToString() );
				return false;
			}
			set
			{
				_tl.LogMessage( "TempComp Set", "Not implemented" );
				throw new PropertyNotImplementedException( "TempComp", false );
			}
		}

		public bool TempCompAvailable
		{
			get
			{
				_tl.LogMessage( "TempCompAvailable Get", false.ToString() );
				return false; // Temperature compensation is not available in this driver
			}
		}

		public double Temperature
		{
			get
			{
				_tl.LogMessage( "Temperature Get", "Not implemented" );
				throw new PropertyNotImplementedException( "Temperature", false );
			}
		}

		#endregion

		#region Private properties and methods
		// here are some useful properties and methods that can be used as required
		// to help with driver development

		#region ASCOM Registration

		// Register or unregister driver for ASCOM. This is harmless if already
		// registered or unregistered. 
		//
		/// <summary>
		/// Register or unregister the driver with the ASCOM Platform.
		/// This is harmless if the driver is already registered/unregistered.
		/// </summary>
		/// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
		private static void RegUnregASCOM( bool bRegister )
		{
			Attribute attr = Attribute.GetCustomAttribute( typeof( Focuser ), typeof( ProgIdAttribute ) );
			string driverID = ( (ProgIdAttribute)attr ).Value ?? "PROGID NOT SET!";  // Get the driver ProgIDfrom the ProgID attribute.

			using ( var P = new Profile() )
			{
				P.DeviceType = "Focuser";
				if ( bRegister )
				{
					P.Register( driverID, _driverDescription );
				}
				else
				{
					P.Unregister( driverID );
				}
			}
		}

		/// <summary>
		/// This function registers the driver with the ASCOM Chooser and
		/// is called automatically whenever this class is registered for COM Interop.
		/// </summary>
		/// <param name="t">Type of the class being registered, not used.</param>
		/// <remarks>
		/// This method typically runs in two distinct situations:
		/// <list type="numbered">
		/// <item>
		/// In Visual Studio, when the project is successfully built.
		/// For this to work correctly, the option <c>Register for COM Interop</c>
		/// must be enabled in the project settings.
		/// </item>
		/// <item>During setup, when the installer registers the assembly for COM Interop.</item>
		/// </list>
		/// This technique should mean that it is never necessary to manually register a driver with ASCOM.
		/// </remarks>
		[ComRegisterFunction]
		public static void RegisterASCOM( Type t )
		{
			_ = t.Name; // Just included to remove a compiler informational message that the mandatory type parameter "t" is not used within the member
			RegUnregASCOM( true );
		}

		/// <summary>
		/// This function unregisters the driver from the ASCOM Chooser and
		/// is called automatically whenever this class is unregistered from COM Interop.
		/// </summary>
		/// <param name="t">Type of the class being registered, not used.</param>
		/// <remarks>
		/// This method typically runs in two distinct situations:
		/// <list type="numbered">
		/// <item>
		/// In Visual Studio, when the project is cleaned or prior to rebuilding.
		/// For this to work correctly, the option <c>Register for COM Interop</c>
		/// must be enabled in the project settings.
		/// </item>
		/// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
		/// </list>
		/// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
		/// </remarks>
		[ComUnregisterFunction]
		public static void UnregisterASCOM( Type t )
		{
			_ = t.Name; // Just included to remove a compiler informational message that the mandatory type parameter "t" is not used within the member
			RegUnregASCOM( false );
		}

		#endregion

		/// <summary>
		/// Returns true if there is a valid connection to the driver hardware
		/// </summary>
		private bool IsConnected
		{
			get
			{
				// TODO check that the driver hardware connection exists and is connected to the hardware
				return _connectedState;
			}
		}

		/// <summary>
		/// Use this function to throw an exception if we aren't connected to the hardware
		/// </summary>
		/// <param name="message"></param>
		private void CheckConnected( string message )
		{
			if ( !IsConnected )
			{
				throw new NotConnectedException( message );
			}
		}

		/// <summary>
		/// Read the device configuration from the ASCOM Profile store
		/// </summary>
		internal void ReadProfile()
		{
			using ( Profile driverProfile = new Profile() )
			{
				driverProfile.DeviceType = "Focuser";
				_tl.Enabled = Convert.ToBoolean( driverProfile.GetValue( _driverID, _traceStateProfileName, string.Empty, _traceStateDefault ) );
				SerialPortName = driverProfile.GetValue( _driverID, _comPortProfileName, string.Empty, _comPortDefault );
			}
		}

		/// <summary>
		/// Write the device configuration to the  ASCOM  Profile store
		/// </summary>
		internal void WriteProfile()
		{
			using ( Profile driverProfile = new Profile() )
			{
				driverProfile.DeviceType = "Focuser";
				driverProfile.WriteValue( _driverID, _traceStateProfileName, _tl.Enabled.ToString() );
				driverProfile.WriteValue( _driverID, _comPortProfileName, SerialPortName );
			}

			// Whenever we update the serial port name for the focuser we also need to change it for the Rotator.

			UpdateSerialPortProfile( "Rotator", SerialPortName );
		}

		/// <summary>
		/// Log helper function that takes formatted strings and arguments
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		internal void LogMessage( string identifier, string message, params object[] args )
		{
			var msg = string.Format( message, args );
			_tl.LogMessage( identifier, msg );
		}
		#endregion
	}
}
