//
// ================
// Shared Resources
// ================
//
// This class is a container for all shared resources that may be needed
// by the drivers served by the Local Server. 
//
// NOTES:
//
//	* ALL DECLARATIONS MUST BE STATIC HERE!! INSTANCES OF THIS CLASS MUST NEVER BE CREATED!


using ASCOM;
using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ASCOM.LocalServer
{
    /// <summary>
    /// The resources shared by all drivers and devices, in this example it's a serial port with a shared SendMessage method an idea for locking the message and handling connecting is given.
    /// In reality extensive changes will probably be needed. Multiple drivers means that several applications connect to the same hardware device, aka a hub.
    /// Multiple devices means that there are more than one instance of the hardware, such as two focusers. In this case there needs to be multiple instances of the hardware connector, each with it's own connection count.
    /// </summary>
    public static class SharedResources
    {
        // Object used for locking to prevent multiple drivers accessing common code at the same time
        private static readonly object _lockObject = new object();

		// Shared serial port. This will allow multiple drivers to use one single serial port.
		private static Serial _sharedSerial = null;		// Shared serial port
        private static int _serialConnectionCount = 0;	// counter for the number of connections to the serial port

        // serial port configuration properties

        // Create the constant variables to define port characteristics.

        private const int _serialDataBits = 8;
        private const bool _serialDtrEnable = true;
        private const SerialHandshake _serialHandshake = SerialHandshake.None;
        private const SerialParity _serialParity = SerialParity.None;
        private const int _serialReceiveTimeout = 5;
        private const bool _serialRtsEnable = false;
        private const SerialSpeed _serialPortSpeed = SerialSpeed.ps9600;
        private const SerialStopBits _serialStopBits = SerialStopBits.One;

        // Public access to shared resources

        #region single serial port connector

        // This region shows a way that a single serial port could be connected to by multiple drivers.
        // Connected is used to handle the connections to the port.
        // SendMessage is a way that messages could be sent to the hardware without conflicts between different drivers.
        //
        // All this is for a single connection, multiple connections would need multiple ports and a way to handle connecting and disconnection from them - see the multi driver handling section for ideas.

        /// <summary>
        /// Shared serial port
        /// </summary>
        public static Serial SharedSerial
        {
            get
            {
				if ( _sharedSerial == null )
				{
					_sharedSerial = new Serial();
				}

                return _sharedSerial;
            }
        }

        private static string SerialPortName { get; set; }  

        /// <summary>
        /// Number of connections to the shared serial port
        /// </summary>
        public static int Connections
        {
            get
            {
                return _serialConnectionCount;
            }

            set
            {
                _serialConnectionCount = value;
            }
        }

        /// <summary>
        /// Example of a shared SendMessage method
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <remarks>
        /// The lock prevents different drivers tripping over one another. It needs error handling and assumes that the message will be sent unchanged and that the reply will always be terminated by a "#" character.
        /// </remarks>
        public static string SendMessage(string message)
        {
            lock (_lockObject)
            {
                message = message.Replace(",", "."); /// zamjena "krivih znakova"
                SharedSerial.Transmit(message + "#");
                // TODO replace this with your requirements
                string response = SharedSerial.ReceiveTerminated("#");
                response = response.Replace("#", "");
                response = response.Replace(",", ".");

                return response;
            }
        }

        #region Connect/Disconnect logic

        public static void ConnectToDevice( string portName )
		{
			lock ( _lockObject )
			{
				if ( _serialConnectionCount == 0 )
				{
					// Init the port characteristics;

					SharedSerial.DataBits = _serialDataBits;
					SharedSerial.DTREnable = _serialDtrEnable;
					SharedSerial.Handshake = _serialHandshake;
					SharedSerial.Parity = _serialParity;
					SharedSerial.PortName = portName;
					SharedSerial.ReceiveTimeout = _serialReceiveTimeout;
					SharedSerial.RTSEnable = _serialRtsEnable;
					SharedSerial.Speed = _serialPortSpeed;
					SharedSerial.StopBits = _serialStopBits;

					// Finally, open the port.

					SharedSerial.Connected = true;
				}

				_serialConnectionCount++;
			}
		}

		public static void DisconnectFromDevice()
		{
			// Close the port and dispose of it.

			lock ( _lockObject )
			{
				_serialConnectionCount--;

				if ( _serialConnectionCount <= 0 )
				{
					SharedSerial.Connected = false;
					SharedSerial.Dispose();
					_sharedSerial = null;
					_serialConnectionCount = 0;
				}
			}
		}

		#endregion Connect/Disconnect logic

	}

	#endregion
}
