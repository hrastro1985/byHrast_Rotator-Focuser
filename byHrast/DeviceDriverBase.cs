using ASCOM.LocalServer;
using ASCOM.Utilities;

using System;
using System.Runtime.InteropServices;

namespace ASCOM.byHrast
{
	public class DeviceDriverBase : ReferenceCountedObjectBase
	{
		protected const string _comPortDefault = "COM1";
		protected string _driverID; // ASCOM DeviceID (COM ProgID) for this driver, the value is retrieved from the ServedClassName attribute in the class initialiser.
		protected const string _comPortProfileName = "COM Port";

		public static string SerialPortName { get; set; }

		public void UpdateSerialPortProfile( string deviceType, string portName )
		{
			string driverID = "PROGID NOT SET!";

			if ( deviceType.ToLower() == "focuser" )
			{
				driverID = GetFocuserProgID();
			}
			else if ( deviceType.ToLower() == "rotator" )
			{
				driverID = GetRotatorProgID();
			}

			using (Profile driverProfile = new Profile())
			{
				driverProfile.DeviceType = deviceType;
				driverProfile.WriteValue( driverID, _comPortProfileName, portName );
			}
		}

		private string GetFocuserProgID()
		{
			// Pull the ProgID from the Focuser's ProgID class attribute.

			Attribute attr = Attribute.GetCustomAttribute( typeof( ASCOM.byHrast.Focuser.Focuser ), typeof( ProgIdAttribute ) );
			
			return ( (ProgIdAttribute)attr ).Value ?? "PROGID NOT SET!";  // Get the driver ProgIDfrom the ProgID attribute.

		}

		private string GetRotatorProgID()
		{
			// Pull the ProgID from the Rotator's ProgID class attribute.

			Attribute attr = Attribute.GetCustomAttribute( typeof( ASCOM.byHrast.Rotator.Rotator), typeof( ProgIdAttribute ) );

			return ( (ProgIdAttribute)attr ).Value ?? "PROGID NOT SET!";  // Get the driver ProgIDfrom the ProgID attribute.
		}
	}
}
