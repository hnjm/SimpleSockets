using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleSockets.Helpers;
using SimpleSockets.Helpers.Compression;
using SimpleSockets.Helpers.Cryptography;

namespace SimpleSockets.Messaging {

    public class PacketReceiver {

		internal byte[] Buffer { get; private set; }

		internal int BufferSize { get; private set; }

		private byte[] _received;

		internal byte[] Received => _received;

        private LogHelper _logger;

        internal PacketReceiver(LogHelper logger, int bufferSize) {
			BufferSize = bufferSize;
            Buffer = new byte[BufferSize];
            _received = new byte[0];
            _logger = logger;
        }

		internal void ClearBuffer() {
			Buffer = null;
			Buffer = new byte[BufferSize];
		}

		/// <summary>
		/// Returns true if delimiter was found.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		internal bool AppendByteToReceived(byte readByte) {
			_received = PacketHelper.MergeByteArrays(_received, new byte[1] { readByte });

			if (_received.Length >= PacketHelper.PacketDelimiter.Length) {
				var end = _received.Skip(_received.Length - PacketHelper.PacketDelimiter.Length).Take(PacketHelper.PacketDelimiter.Length).ToArray();
				if (end.SequenceEqual(PacketHelper.PacketDelimiter))
				{
					_logger?.Log("Packet delimiter found.", LogLevel.Trace);
					_received = _received.Take(_received.Length - PacketHelper.PacketDelimiter.Length).ToArray();
					return true;
				}
			}
			return false;
		}

		internal Packet BuildMessageFromPayload(byte[] encryptionPassphrase, byte[] presharedKey) {
			try
			{
				return PacketReceiverBuilder.InitializeReceiver(_logger, _received[0], out var headerLength)
					.AddPassphrase(encryptionPassphrase)
					.AppendHeaderBytes(_received.Skip(1).Take(headerLength).ToArray())
					.AppendContentBytes(_received.Skip(1 + headerLength).Take(_received.Length - 1 + headerLength).ToArray())
					.Build(presharedKey);
			}
			catch (Exception ex) {
				_logger?.Log("An error occurred receiving a message from a connected socket.", ex, LogLevel.Error);
				return null;
			}
		}

    }

}