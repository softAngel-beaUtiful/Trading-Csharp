using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

// <summary>The System.Net namespace provides a simple programming interface for many of the protocols used on networks today.</summary>
namespace TickQuant.Network 
{
	/// <summary>
	/// Implements the ICMP messaging service.
	/// </summary>
	/// <remarks>Currently, the implementation only supports the echo message (better known as 'ping').</remarks>
	public class Icmp 
	{
		/// <summary>
		/// Initializes an instance of the Icmp class.
		/// </summary>
		/// <param name="host">The host that will be used to communicate with.</param>
		public Icmp(IPAddress host) 
		{
			Host = host;
		}
		/// <summary>
		/// Generates the Echo message to send.
		/// </summary>
		/// <returns>An array of bytes that represents the ICMP echo message to send.</returns>
		protected byte[] GetEchoMessageBuffer(int weight) 
		{
			if (weight <1) weight = 1;
			EchoMessage message = new EchoMessage();
			message.Type = 8;	// ICMP echo
			message.Data = new byte[1 * weight];
			for (int i = 0; i < weight; i++) 
			{
	     		message.Data[i] = 32;	// Send spaces
			}
			message.CheckSum = message.GetChecksum();
			return message.GetObjectBytes();
		}
		/// <summary>
		/// Initiates an ICMP ping with a timeout of 1000 milliseconds.
		/// </summary>
		/// <exception cref="SocketException">There was an error while communicating with the remote server.</exception>
		/// <returns>A TimeSpan object that holds the time it takes for a packet to travel to the remote server and back. A value of TimeSpan.MaxValue indicates a timeout.</returns>
		/// <example>
		/// The following example will ping the server www.mentalis.org ten times and print the results in the Console.
		/// <c>
		/// <pre>
		/// Icmp icmp = new Icmp(Dns.GetHostEntry("www.mentalis.org").AddressList[0]);
		/// for (int i = 0; i &lt; 10; i++) {
		/// 	Console.WriteLine(icmp.Ping().TotalMilliseconds);
		/// }
		/// </pre>
		/// </c>
		/// </example>
		public PingRequestInformation Ping(Stopwatch stop) 
		{
			return Ping(1000, stop);
		}
		public PingRequestInformation Ping(int timeout, Stopwatch stop)
		{
			//By default TTL = 128
			return Ping(timeout, 128, stop);
		}
		public PingRequestInformation Ping(int timeout, int TTL, Stopwatch stop)
		{
			return Ping(timeout, TTL, 32, stop);
		}
		/// <summary>
		/// Initiates an ICMP ping.
		/// </summary>
		/// <param name="timeout">Specifies the timeout in milliseconds. If this value is set to Timeout.Infinite, the method will never time out.</param>
		/// <returns>A TimeSpan object that holds the time it takes for a packet to travel to the remote server and back. A value of TimeSpan.MaxValue indicates a timeout.</returns>
		public PingRequestInformation Ping(int timeout, int TTL, int weight, Stopwatch stop) 
		{
			TimeSpan ret;
			EndPoint remoteEP = new IPEndPoint(Host, 0);
			int returnTTL = 128;
			ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
			// Get the ICMP message
			byte [] buffer = GetEchoMessageBuffer(weight);
			
			// Set the timeout timer
			HasTimedOut = false;
			PingTimeOut = new Timer(new TimerCallback(PingTimedOut), null, timeout, Timeout.Infinite);
            // Get the current time
            stop.Restart();
            StartTime = stop.Elapsed;            
            // Send the ICMP message and receive the reply
            try
            {
                //Set the Time to live option (usefull for tracert)
                ClientSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, TTL);
                if (ClientSocket.SendTo(buffer, remoteEP) <= 0)
                    throw new SocketException();
                buffer = new byte[buffer.Length + 20];
                if (ClientSocket.ReceiveFrom(buffer, ref remoteEP) <= 0)
                    throw new SocketException();
                //Get the TTL from the return packet
                returnTTL = Convert.ToInt32(buffer[8].ToString());
            }
            catch (SocketException e)
            {
                if (HasTimedOut)
                    return new PingRequestInformation(returnTTL, Host.ToString(), remoteEP.ToString().Substring(0, remoteEP.ToString().Length - 2), TimeSpan.MaxValue, TimeSpan.MaxValue);
                else
                    throw e;
            }
            finally
            {
                // Close the socket
                ClientSocket.Close();
                ClientSocket = null;
                // destroy the timer
                PingTimeOut.Change(Timeout.Infinite, Timeout.Infinite);
                PingTimeOut.Dispose();
                PingTimeOut = null;
            }
            // Get the time
            ret = stop.Elapsed;// -StartTime;
            TimeSpan ret1 = stop.Elapsed;// -StartTime;
			return new PingRequestInformation(returnTTL,Host.ToString(),remoteEP.ToString().Substring(0,remoteEP.ToString().Length -2),ret, ret1);
		}
		/// <summary>
		/// Called when the ping method times out.
		/// </summary>
		/// <param name="state">The source of the event. This is an object containing application-specific information relevant to the methods invoked by this delegate, or a null reference (Nothing in Visual Basic).</param>
		protected void PingTimedOut(object state) 
		{
			HasTimedOut = true;
			// Close the socket (this will result in a throw of a SocketException in the Ping method)
			if (ClientSocket != null)
				ClientSocket.Close();
		}												  
		/// <summary>
		/// Gets or sets the address of the remote host to communicate with.
		/// </summary>
		/// <value>An IPAddress instance that specifies the address of the remote host to communicate with.</value>
		/// <exception cref="ArgumentNullException">The specified value is null (Nothing in VB.NET).</exception>
		protected IPAddress Host 
		{
			get 
			{
				return m_Host;
			}
			set 
			{
				if (value == null)
					throw new ArgumentNullException();
				m_Host = value;
			}
		}
		/// <summary>
		/// Gets or sets the Socket that is used to communicate with the remote server.
		/// </summary>
		/// <value>A Socket object that is used to communicate with the remote server.</value>
		protected Socket ClientSocket 
		{
			get 
			{
				return m_ClientSocket;
			}
			set 
			{
				m_ClientSocket = value;
			}
		}
		/// <summary>
		/// Gets or sets the time when the ping began/begins.
		/// </summary>
		/// <value>A TimeSpan object that specifies when the ping began.</value>
		protected TimeSpan StartTime 
		{
			get 
			{
				return m_StartTime;
			}
			set 
			{
				m_StartTime = value;
			}
		}
        protected DateTime StartTime1
        {
            get;
            set;
        }
		/// <summary>
		/// Gets or sets the timer that triggers the PingTimedOut method.
		/// </summary>
		/// <value>A Timer object that triggers the PingTimedOut method.</value>
		protected Timer PingTimeOut 
		{
			get 
			{
				return m_PingTimeOut;
			}
			set 
			{
				m_PingTimeOut = value;
			}
		}
		/// <summary>
		/// Gets or sets a value that indicates whether the ping has timed out or not.
		/// </summary>
		/// <value>A boolean value that indicates whether the ping has timed out or not.</value>
		protected bool HasTimedOut 
		{
			get 
			{
				return m_HasTimedOut;
			}
			set 
			{
				m_HasTimedOut = value;
			}
		}
		/// <summary>
		/// Releases all the resources.
		/// </summary>
		~Icmp() 
		{
			if (ClientSocket != null)
				ClientSocket.Close();
		}
		// Private variables
		/// <summary>Stores the value of the Host property.</summary>
		private IPAddress m_Host;
		/// <summary>Stores the value of the ClientSocket property.</summary>
		private Socket m_ClientSocket;
		/// <summary>Stores the value of the StartTime property.</summary>
		private TimeSpan m_StartTime;
		/// <summary>Stores the value of the PingTimeOut property.</summary>
		private Timer m_PingTimeOut;
		/// <summary>Stores the value of the HasTimedOut property.</summary>
		private bool m_HasTimedOut;
	}
	
	/// <summary>
	/// Defines a base ICMP message
	/// </summary>
	public abstract class IcmpMessage 
	{
		/// <summary>
		/// Initializes a new IcmpMessage instance.
		/// </summary>
		public IcmpMessage() {}
		/// <summary>
		/// Gets or sets the type of the message.
		/// </summary>
		/// <value>A byte that specifies the type of the message.</value>
		public byte Type 
		{
			get 
			{
				return m_Type;
			}
			set 
			{
				m_Type = value;
			}
		}
		/// <summary>
		/// Gets or sets the message code.
		/// </summary>
		/// <value>A byte that specifies the message code.</value>
		public byte Code 
		{
			get 
			{
				return m_Code;
			}
			set 
			{
				m_Code = value;
			}
		}
		/// <summary>
		/// Gets or sets the chacksum for this message.
		/// </summary>
		/// <value>An unsigned short that holds the checksum of this message.</value>
		public ushort CheckSum 
		{
			get 
			{
				return m_CheckSum;
			}
			set 
			{
				m_CheckSum = value;
			}
		}
		/// <summary>
		/// Serializes the object into an array of bytes.
		/// </summary>
		/// <returns>An array of bytes that represents the ICMP message.</returns>
		public virtual byte [] GetObjectBytes() 
		{
			byte [] ret = new byte[4];
			Array.Copy(BitConverter.GetBytes(Type), 0, ret, 0, 1);
			Array.Copy(BitConverter.GetBytes(Code), 0, ret, 1, 1);
			Array.Copy(BitConverter.GetBytes(CheckSum), 0, ret, 2, 2);
			return ret;
		}
		/// <summary>
		/// Calculates the checksum of this message.
		/// </summary>
		/// <returns>An unsigned short that holds the checksum of this ICMP message.</returns>
		public ushort GetChecksum() 
		{
			ulong sum = 0;
			byte [] bytes = GetObjectBytes();
			// Sum all the words together, adding the final byte if size is odd
			int i;
			for (i = 0; i < bytes.Length - 1; i += 2) 
			{
				sum += BitConverter.ToUInt16(bytes, i);
			}
			if (i != bytes.Length)
				sum += bytes[i];
			// Do a little shuffling
			sum = (sum >> 16) + (sum & 0xFFFF);
			sum += (sum >> 16);
			return (ushort)(~sum);
		}
		// Private variables
		/// <summary>Holds the value of the Type property.</summary>
		private byte m_Type = 0;
		/// <summary>Holds the value of the Code property.</summary>
		private byte m_Code = 0;
		/// <summary>Holds the value of the CheckSum property.</summary>
		private ushort m_CheckSum = 0;
	}
	
	/// <summary>
	/// Defines an ICMP message with an ID and a sequence number.
	/// </summary>
	public class InformationMessage : IcmpMessage 
	{
		/// <summary>
		/// Initializes a new InformationMessage instance.
		/// </summary>
		public InformationMessage() {}
		/// <summary>
		/// Gets or sets the identification number.
		/// </summary>
		/// <value>An unsigned short that holds the identification number of this message.</value>
		public ushort Identifier 
		{
			get 
			{
				return m_Identifier;
			}
			set 
			{
				m_Identifier = value;
			}
		}
		/// <summary>
		/// Gets or sets the sequence number.
		/// </summary>
		/// <value>An unsigned short that holds the sequence number of this message.</value>
		public ushort SequenceNumber 
		{
			get 
			{
				return m_SequenceNumber;
			}
			set 
			{
				m_SequenceNumber = value;
			}
		}
		/// <summary>
		/// Serializes the object into an array of bytes.
		/// </summary>
		/// <returns>An array of bytes that represents the ICMP message.</returns>
		public override byte [] GetObjectBytes() 
		{
			byte [] ret = new byte[8];
			Array.Copy(base.GetObjectBytes(), 0, ret, 0, 4);
			Array.Copy(BitConverter.GetBytes(Identifier), 0, ret, 4, 2);
			Array.Copy(BitConverter.GetBytes(SequenceNumber), 0, ret, 6, 2);
			return ret;
		}
		// Private variables
		/// <summary>Holds the value of the Identifier property.</summary>
		private ushort m_Identifier = 0;
		/// <summary>Holds the value of the SequenceNumber property.</summary>
		private ushort m_SequenceNumber = 0;
	}
	
	/// <summary>
	/// Defines an echo ICMP message.
	/// </summary>
	public class EchoMessage : InformationMessage 
	{
		/// <summary>
		/// Initializes a new EchoMessage instance.
		/// </summary>
		public EchoMessage() {}
		/// <summary>
		/// Gets or sets the data of this message.
		/// </summary>
		/// <value>An array of bytes that represents the data of this message.</value>
		public byte[] Data 
		{
			get 
			{
				return m_Data;
			}
			set 
			{
				m_Data = value;
			}
		}
		/// <summary>
		/// Serializes the object into an array of bytes.
		/// </summary>
		/// <returns>An array of bytes that represents the ICMP message.</returns>
		public override byte [] GetObjectBytes() 
		{
			int length = 8;
			if (Data != null)
				length += Data.Length;
			byte [] ret = new byte[length];
			Array.Copy(base.GetObjectBytes(), 0, ret, 0, 8);
			if (Data != null)
				Array.Copy(Data, 0, ret, 8, Data.Length);
			return ret;
		}
		// Private variables
		/// <summary>Holds the value of the Data property.</summary>
		private byte[] m_Data;
	}

	public class PingRequestInformation
	{
		public  int TTL;
		public  string IPFrom;
		public  string IPEndPoint;
		public TimeSpan duration;
        public TimeSpan duration1;
		public PingRequestInformation(int TTL, string IPFrom, string IPEndPoint, TimeSpan duration, TimeSpan duration1)
		{
			this.TTL = TTL;
			this.IPFrom = IPFrom;
			this.IPEndPoint = IPEndPoint;
			this.duration = duration;
            this.duration1 = duration1;
		}
	}

}
