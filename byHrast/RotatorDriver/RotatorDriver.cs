//tabs=4
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Rotator driver for byHrast
//
// Description:	 <To be completed by driver developer>
//
// Implements:	ASCOM Rotator interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.DeviceInterface;
using ASCOM.LocalServer;
using ASCOM.Utilities;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ASCOM.byHrast.Rotator
{
	//
	// Your driver's DeviceID is ASCOM.byHrast.Rotator
	//
	// The Guid attribute sets the CLSID for ASCOM.byHrast.Rotator
	// The ClassInterface/None attribute prevents an empty interface called
	// _byHrast from being created and used as the [default] interface
	//
	// TODO Replace the not implemented exceptions with code to implement the function or
	// throw the appropriate ASCOM exception.
	//

	/// <summary>
	/// ASCOM Rotator Driver for byHrast.
	/// </summary>
	[ComVisible( true )]
	[Guid( "a01c478e-54f2-4b0f-8e31-7cef43a80fc4" )]
	[ProgId( "ASCOM.byHrast.Rotator" )]
	[ServedClassName( "Rotator byHrast" )] // Driver description that appears in the Chooser, customise as required
	[ClassInterface( ClassInterfaceType.None )]
	public class Rotator : DeviceDriverBase, IRotatorV3
	{
		// Constants used for Profile persistence
		//internal const string comPortProfileName = "COM Port";
		//internal const string comPortDefault = "COM1";
		internal const string _traceStateProfileName = "Trace Level";
		internal const string _traceStateDefault = "true";

		internal const string _RevStateProfileName = "Reverse"; //moj dio
		internal const string _RevStateDefault = "false"; //moj dio
		internal const string _HoldStateProfileName = "Hold"; //moj dio
		internal const string _HoldStateDefault = "true"; //moj dio

		//internal static string driverID; // ASCOM DeviceID (COM ProgID) for this driver, the value is retrieved from the ServedClassName attribute in the class initialiser.
		internal static string _driverDescription; // The value is retrieved from the ServedClassName attribute in the class initialiser.
		internal static bool _connectedState; // variable to hold the connected state
		internal static bool _Rev; //moj dio
		internal static bool _Hold; //moj dio
		internal static Util _utilities; // Private variable to hold an ASCOM Utilities object
		internal static AstroUtils _astroUtilities; // Variable to hold an ASCOM AstroUtilities object to provide the Range method
		internal static TraceLogger _tl; // Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)


		/// <summary>
		/// Initializes a new instance of the <see cref="byHrast"/> class. Must be public to successfully register for COM.
		/// </summary>
		public Rotator()
		{
			try
			{
				// Pull the ProgID from the ProgID class attribute.
				Attribute attr = Attribute.GetCustomAttribute( this.GetType(), typeof( ProgIdAttribute ) );
				_driverID = ( (ProgIdAttribute)attr ).Value ?? "PROGID NOT SET!";  // Get the driver ProgIDfrom the ProgID attribute.

				// Pull the display name from the ServedClassName class attribute.
				attr = Attribute.GetCustomAttribute( this.GetType(), typeof( ServedClassNameAttribute ) );
				_driverDescription = ( (ServedClassNameAttribute)attr ).DisplayName ?? "DISPLAY NAME NOT SET!";  // Get the driver description that displays in the ASCOM Chooser from the ServedClassName attribute.

				_tl = new TraceLogger( "", "ASCOM.byHrast.Rotator" );
				ReadProfile(); // Read device configuration from the ASCOM Profile store, including the trace state

				_tl.LogMessage( "Rotator", "Starting initialisation" );
				_tl.LogMessage( "Rotator", $"ProgID: {_driverID}, Description: {_driverDescription}" );

				_connectedState = false; // Initialise connected to false
				_utilities = new Util(); //Initialise util object
				_astroUtilities = new AstroUtils(); // Initialise astro-utilities object

				//TODO: Implement your additional construction here

				_tl.LogMessage( "Rotator", "Completed initialisation" );
			}
			catch (Exception ex)
			{
				_tl.LogMessageCrLf( "Rotator", $"Initialisation exception: {ex}" );
				MessageBox.Show( $"{ex.Message}", "Exception creating ASCOM.byHrast.Rotator", MessageBoxButtons.OK, MessageBoxIcon.Error );
			}

		}

		// PUBLIC COM INTERFACE IRotatorV3 IMPLEMENTATION

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
			if (IsConnected)
				MessageBox.Show( "Already connected, just press OK" );

			using (SetupDialogForm F = new SetupDialogForm( _tl ))
			{
				var result = F.ShowDialog();
				if (result == DialogResult.OK)
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
				if (value == IsConnected)
                {

					ReadProfile(); /// pročitaj vrijednosti varijabli

					if (_Rev == true)
					{ // Ako sam oznacio reverse prilikom setupa
						_tl.LogMessage("Reverse ON","", true);
						SharedResources.SendMessage("RW");
					}
					else
					{
						_tl.LogMessage("Reverse OFF", "", true);
						SharedResources.SendMessage("RC");
					}
					if (_Hold == true)
					{ // Ako sam oznacio hold prilikom setupa
						_tl.LogMessage("Hold ON", "", true);
						SharedResources.SendMessage("RH");
					}
					else
					{
						_tl.LogMessage("Reverse OFF", "", true);
						SharedResources.SendMessage("RX");
					}
					return;
				}
					

				if (value)
				{
					_connectedState = true;
					LogMessage( "Connected Set", "Connecting to port {0}", SerialPortName );
					// TODO connect to the device
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
				string driverInfo = "Rotator byHrast";
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
				string name = "Rotator byHrast";
				_tl.LogMessage( "Name Get", name );
				return name;
			}
		}

		#endregion

		#region IRotator Implementation

		private float rotatorPosition = 0; // Synced or mechanical position angle of the rotator
		private float mechanicalPosition = 0; // Mechanical position angle of the rotator

		public bool CanReverse
		{
			get
			{
				_tl.LogMessage( "CanReverse Get", true.ToString() );
				return false;
			}
		}

		public void Halt()
		{
			_tl.LogMessage( "Halt", "" );
			SharedResources.SendMessage("FQ"); // Set the focuser position
		}

		public bool IsMoving
		{
			get
			{
				_tl.LogMessage( "IsMoving Get", true.ToString() ); // This rotator has instantaneous movement
				if (SharedResources.SendMessage("RM") == "0")
					return false;
				else return true;
			}
		}

		public void Move( float Position )
		{
			_tl.LogMessage( "Move", Position.ToString() ); // Move by this amount
			rotatorPosition += Position;
			rotatorPosition = (float)_astroUtilities.Range(Position, 0.0, true, 360.0, false ); // Ensure value is in the range 0.0..359.9999...
			SharedResources.SendMessage("RR" + (Position).ToString());
		}

		public void MoveAbsolute( float Position )
		{
			_tl.LogMessage( "MoveAbsolute", Position.ToString() ); // Move to this position
			rotatorPosition = Position;
			rotatorPosition = (float)_astroUtilities.Range( rotatorPosition, 0.0, true, 360.0, false ); // Ensure value is in the range 0.0..359.9999...
			SharedResources.SendMessage("RA" + (rotatorPosition).ToString());
		}

		public float Position
		{
			get
			{
				_tl.LogMessage( "Position Get", rotatorPosition.ToString() ); // This rotator has instantaneous movement
				rotatorPosition = float.Parse(SharedResources.SendMessage("RP"));
				rotatorPosition = rotatorPosition / 100;
				return rotatorPosition;
			}
		}

		public bool Reverse
		{
			get
			{
				_tl.LogMessage("Reverse Get", "Not implemented");
				throw new PropertyNotImplementedException("Reverse", false);
			}
			set
			{
				_tl.LogMessage("Reverse Set", "Not implemented");
				throw new PropertyNotImplementedException("Reverse", true);
			}
		}

		public float StepSize
		{
			get
			{
				_tl.LogMessage( "StepSize Get", "Not implemented" );
				return float.Parse(SharedResources.SendMessage("RS"));
			}
		}

		public float TargetPosition
		{
			get
			{
				_tl.LogMessage( "TargetPosition Get", rotatorPosition.ToString() ); // This rotator has instantaneous movement
				return rotatorPosition = float.Parse(SharedResources.SendMessage("RP"));
			}
		}

		// IRotatorV3 methods

		public float MechanicalPosition
		{
			get
			{
				_tl.LogMessage( "MechanicalPosition Get", mechanicalPosition.ToString() );

				return rotatorPosition;
			}
		}

		public void MoveMechanical( float Position )
		{
			_tl.LogMessage( "MoveMechanical", Position.ToString() ); // Move to this position

			// TODO: Implement correct sync behaviour. i.e. if the rotator has been synced the mechanical and rotator positions won't be the same
			mechanicalPosition = (float)_astroUtilities.Range( Position, 0.0, true, 360.0, false ); // Ensure value is in the range 0.0..359.9999...
			rotatorPosition = (float)_astroUtilities.Range( Position, 0.0, true, 360.0, false ); // Ensure value is in the range 0.0..359.9999...
			SharedResources.SendMessage("RR" + (mechanicalPosition).ToString());
		}

		public void Sync( float Position )
		{
			_tl.LogMessage( "Sync", Position.ToString() ); // Sync to this position

			// TODO: Implement correct sync behaviour. i.e. the rotator mechanical and rotator positions may not be the same
			rotatorPosition = (float)_astroUtilities.Range( Position, 0.0, true, 360.0, false ); // Ensure value is in the range 0.0..359.9999...
			SharedResources.SendMessage("RY" + (rotatorPosition).ToString());

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
			Attribute attr = Attribute.GetCustomAttribute( typeof( Rotator ), typeof( ProgIdAttribute ) );
			string driverID = ( (ProgIdAttribute)attr ).Value ?? "PROGID NOT SET!";  // Get the driver ProgIDfrom the ProgID attribute.

			using (var P = new Profile())
			{
				P.DeviceType = "Rotator";
				if (bRegister)
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
			if (!IsConnected)
			{
				throw new NotConnectedException( message );
			}
		}

		/// <summary>
		/// Read the device configuration from the ASCOM Profile store
		/// </summary>
		internal void ReadProfile()
		{
			using (Profile driverProfile = new Profile())
			{
				driverProfile.DeviceType = "Rotator";
				_tl.Enabled = Convert.ToBoolean( driverProfile.GetValue( _driverID, _traceStateProfileName, string.Empty, _traceStateDefault ) );
				SerialPortName = driverProfile.GetValue( _driverID, _comPortProfileName, string.Empty, _comPortDefault );
				_Rev = Convert.ToBoolean(driverProfile.GetValue(_driverID, _RevStateProfileName, string.Empty, _RevStateDefault));
				_Hold = Convert.ToBoolean(driverProfile.GetValue(_driverID, _HoldStateProfileName, string.Empty, _HoldStateDefault));
			}
		}

		/// <summary>
		/// Write the device configuration to the  ASCOM  Profile store
		/// </summary>
		internal void WriteProfile()
		{
			using (Profile driverProfile = new Profile())
			{
				driverProfile.DeviceType = "Rotator";
				driverProfile.WriteValue( _driverID, _traceStateProfileName, _tl.Enabled.ToString() );
				driverProfile.WriteValue( _driverID, _comPortProfileName, SerialPortName );
				driverProfile.WriteValue( _driverID, _RevStateProfileName, _Rev.ToString());
				driverProfile.WriteValue( _driverID, _HoldStateProfileName, _Hold.ToString());
			}

			// Whenever we update the serial port name for the rotator we also need to change it for the focuser.

			UpdateSerialPortProfile( "Focuser", SerialPortName );
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
