using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UrbanChallenge.Common.Utility;

namespace UrbanChallenge.Actuation {
	public static class ActuationCommandInterface {
		public static readonly IPEndPoint ControllerEP = new IPEndPoint(IPAddress.Parse("192.168.1.6"), 20);
		public static readonly IPEndPoint MulticastEP = new IPEndPoint(IPAddress.Parse("239.132.1.6"), 30006);

#if CHECK_THROTTLE_BRAKE
		public const byte maxBrake = 50;
		public const ushort maxThrottle = 2500;
#endif

		// feedback message types
		// we fake the ACT_FEEDBACK_CMDRECEIVED message when we send a command (i.e. mirror it to the multicast actuation address)
		public const byte ACT_FEEDBACK_CMDRECEIVED = 11;

		// Bridge command types
		public const byte BRIDGE_CMD_TX = 0x00;
		public const byte BRIDGE_CMD_REG = 0x01;

		// Command types
		public const byte ACT_CMD_MODE = 0x01;
		public const byte ACT_CMD_STEERING = 0x02;
		public const byte ACT_CMD_TRANS = 0x03;
		public const byte ACT_CMD_THROTTLE = 0x04;
		public const byte ACT_CMD_BRAKE = 0x05;
		public const byte ACT_CMD_ALL = 0x06;

		// Actuation modes
		public const byte ACT_MODE_OFF = 0;
		public const byte ACT_MODE_ESTOP = 1;
		public const byte ACT_MODE_PAUSE = 2;
		public const byte ACT_MODE_RUN = 3;
		public const byte ACT_MODE_REMOTE = 4;
		public const byte ACT_MODE_HUMAN = 5;

		// mask values for CMD_ALL
		public const byte ACT_MASK_STEER = 0x01;
		public const byte ACT_MASK_BRAKE = 0x02;
		public const byte ACT_MASK_THROTTLE = 0x04;
		public const byte ACT_MASK_TRANS = 0x08;
		public const byte ACT_MASK_TURN_SIGNAL = 0x10;

		// turn-signal values
		public const byte ACT_SIG_NONE = 0x00;
		public const byte ACT_SIG_LEFT = 0x01;
		public const byte ACT_SIG_RIGHT = 0x02;
		public const byte ACT_SIG_HAZARDS = 0x03;

		// transmission gear values
		public const byte TRANS_GEAR_PARK = 0;
		public const byte TRANS_GEAR_REV = 1;
		public const byte TRANS_GEAR_NEUTRAL = 2;
		public const byte TRANS_GEAR_DRIVE = 3;

		public static void RegisterAsListener(Socket udp, IPAddress listenerAddr, ushort port) {
			// retrieve the address bytes in network (big endian) order
			byte[] addrBytes = listenerAddr.GetAddressBytes();
			// retireve the port bytes
			byte[] portBytes = BitConverter.GetBytes(port);

			// construct the register message
			byte[] message = new byte[7];
			// message format
			//  bridge command - UINT8 (1 = register listener)
			//  payload length - UINT16 (for this message, fixed at 6, big endian order)
			//  ip address - UINT32 (big endian order)
			//  listen port - UINT16 (big endian order)
			message[0] = BRIDGE_CMD_REG;
			message[1] = addrBytes[0];
			message[2] = addrBytes[1];
			message[3] = addrBytes[2];
			message[4] = addrBytes[3];
			message[5] = portBytes[1]; // note that the order is swapped for port bytes
			message[6] = portBytes[0];

			udp.SendTo(message, ControllerEP);
		}

		public static void SendMessage(Socket udp, byte id, params byte[] payload) {
			byte[] msg = new byte[payload.Length + 4];

			uint innerLen = (uint)payload.Length;

			// bridge message format (all numbers big endian)
			//  bridge command - UINT8 (0 = transmit)
			// --start payload--
			//  inner command - UINT8
			//  inner payload length - UINT16
			//  payload
			msg[0] = BRIDGE_CMD_TX;
			msg[1] = id;
			msg[2] = (byte)((innerLen >> 8) & 0xff);
			msg[3] = (byte)(innerLen & 0xff);

			// copy over the payload
			Buffer.BlockCopy(payload, 0, msg, 4, payload.Length);

			// send that ish out
			udp.SendTo(msg, ControllerEP);
			if (payload.Length == 8) {
				msg = new byte[payload.Length + 9];

				BigEndianBinaryWriter writer = new BigEndianBinaryWriter(msg);
				// write a slot for the timestamp (zero as we don't have any timestamp)
				writer.WriteUInt16(0);
				writer.WriteInt32(0);

				// write message type
				writer.WriteByte(ACT_FEEDBACK_CMDRECEIVED);

				// write the length as bullstuff
				writer.WriteInt16(0);
				
				// only send for all message
				// change the message ID, only send a subset of the stuffs
				writer.WriteBytes(payload);

				// set the stuff stuff
				udp.SendTo(msg, MulticastEP);
			}
		}

		public static void SendAllCommand(Socket udp, short? steering, byte? brake, ushort? throttle, byte? trans, byte? lights) {
#if CHECK_THROTTLE_BRAKE
			// check if both are above zero
			if (brake.GetValueOrDefault() > 22 && throttle.GetValueOrDefault() > 1041) {
				throttle = 1041;
			}

			// check if brake is out of bounds
			if (brake.GetValueOrDefault() > maxBrake) {
				brake = maxBrake;
			}

			// check if throttle is out of bounds
			if (throttle.GetValueOrDefault() > maxThrottle) {
				throttle = maxThrottle;
			}
#endif

			// send all command message
			byte[] msg = new byte[8];

			// mask byte (UINT8)
			// will be set per field
			msg[0] = 0;

#if !DISABLE_STEERING
			// steering (INT16, big endian)
			if (steering.HasValue) {
				msg[0] |= ACT_MASK_STEER;
				msg[1] = (byte)((steering.Value >> 8) & 0xff);
				msg[2] = (byte)(steering.Value & 0xff);
			}
			else {
				msg[1] = 0xff;
				msg[2] = 0xff;
			}
#endif

#if !DISABLE_BRAKE
			// brake (UINT8)
			if (brake.HasValue) {
				msg[0] |= ACT_MASK_BRAKE;
				msg[3] = brake.Value;
			}
			else {
				msg[3] = 0xff;
			}
#endif

#if !DISABLE_THROTTLE
			// throttle (UINT16, big endian)
			if (throttle.HasValue) {
				msg[0] |= ACT_MASK_THROTTLE;
				msg[4] = (byte)((throttle.Value >> 8) & 0xff);
				msg[5] = (byte)(throttle.Value & 0xff);
			}
			else {
				msg[4] = 0xff;
				msg[5] = 0xff;
			}
#endif

			// tranmission (UINT8)
#if !DISABLE_TRANS
			if (trans.HasValue) {
				msg[0] |= ACT_MASK_TRANS;
				msg[6] = trans.Value;
			}
			else {
				msg[6] = 0xff;
			}
#endif

#if !DISABLE_TURN_SIG
			// turn-signal (UINT8)
			if (lights.HasValue) {
				msg[0] |= ACT_MASK_TURN_SIGNAL;
				msg[7] = lights.Value;
			}
			else {
				msg[7] = 0;
			}
#endif

			// send the message
			SendMessage(udp, ACT_CMD_ALL, msg);
		}

		public static void SendSteeringCommand(Socket udp, short steering) {
#if !DISABLE_STEERING
			byte[] message = new byte[3];

			// message payload format
			//  command type - uint8 (0xFE = commanded value)
			//  angle command - uint16
			message[0] = 0xFE;
			message[1] = (byte)((steering >> 8) & 0xFF);
			message[2] = (byte)(steering & 0xFF);

			SendMessage(udp, ACT_CMD_STEERING, message);
#endif
		}

		public static void SendModeCommand(Socket udp, byte cmd) {
			SendMessage(udp, ACT_CMD_MODE, cmd);
		}

	}
}
