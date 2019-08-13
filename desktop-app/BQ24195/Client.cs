using System;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace BQ24195
{
    public class Client
    {
        private readonly Channel<Request> _commands = Channel.CreateUnbounded<Request>();
        private readonly TcpClient _client = new TcpClient();

        private readonly Dispatcher _initialDispatcher;
        private Task _task = null;
        private const int TcpPort = 3360;

        public delegate void ConnectionStateChangedEventHandler(ConnectionState state);

        public delegate void ErrorEventHandler(Exception ex);

        public delegate void GotRegisterHandler(int reg, byte val);

        public delegate void GotBatteryAdcEventHandler(int value);

        public delegate void GotUsbHostEnableEventHandler(bool enabled);

        public delegate void RegisterWriteFinishedEventHandler(int reg, bool ok);

        public event ConnectionStateChangedEventHandler ConnectionStateChanged;
        public event ErrorEventHandler Error;
        public event GotRegisterHandler GotRegister;
        public event GotBatteryAdcEventHandler GotBatteryAdc;
        public event GotUsbHostEnableEventHandler GotUsbHostEnable;
        public event RegisterWriteFinishedEventHandler RegisterWriteFinished;

        private void OnConnectionStateChanged(ConnectionState state) => ConnectionStateChanged?.Invoke(state);
        private void OnError(Exception ex) => _initialDispatcher.Invoke(Error, ex);
        private void OnGotRegister(int reg, byte val) => _initialDispatcher.Invoke(GotRegister, reg, val);
        private void OnGotBatteryAdc(int val) => _initialDispatcher.Invoke(GotBatteryAdc, val);
        private void OnGotUsbHostEnable(bool enabled) => _initialDispatcher.Invoke(GotUsbHostEnable, enabled);

        private void OnRegisterWriteFinished(int reg, bool ok) =>
            _initialDispatcher.Invoke(RegisterWriteFinished, reg, ok);

        abstract class Request
        {
        }

        class GetRegisterRequest : Request
        {
            public int register { get; set; }
        }

        class GetBatteryAdcRequest : Request
        {
        }

        class GetUsbHostEnableRequest : Request
        {
        }

        class WriteRegisterRequest : Request
        {
            public int register { get; set; }
            public byte value { get; set; }
        }

        class SetUsbHostEnableRequest : Request
        {
            public bool high { get; set; }
        }

        class DisconnectRequest : Request
        {
        }

        public Client()
        {
            _initialDispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Connect(string hostname)
        {
            if (_task != null)
            {
                throw new Exception("Already connecting / connected");
            }

            _task = Task.Run(async () => await RunConnectionAsync(hostname));
        }

        public void GetRegister(int reg) => _commands.Writer.TryWrite(new GetRegisterRequest {register = reg});
        public void GetBatteryAdc() => _commands.Writer.TryWrite(new GetBatteryAdcRequest());
        public void GetUsbHostEnable() => _commands.Writer.TryWrite(new GetUsbHostEnableRequest());

        public void WriteRegister(int reg, byte val) =>
            _commands.Writer.TryWrite(new WriteRegisterRequest {register = reg, value = val});

        public void SetUsbHostEnable(bool high) => _commands.Writer.TryWrite(new SetUsbHostEnableRequest {high = high});
        public void Disconnect() => _commands.Writer.TryWrite(new DisconnectRequest());

        private async Task ReadAsync(ArraySegment<byte> buffer)
        {
            int total = buffer.Count;
            int read = 0;
            while (read < total)
            {
                read += await _client.Client.ReceiveAsync(new ArraySegment<byte>(buffer.Array, read, total - read),
                    SocketFlags.None);
            }
        }

        private async Task RunConnectionAsync(string hostname)
        {
            var buffer = new byte[16];
            var oneByteBuf = new ArraySegment<byte>(buffer, 0, 1);
            var twoByteBuf = new ArraySegment<byte>(buffer, 0, 2);
            var threeByteBuf = new ArraySegment<byte>(buffer, 0, 3);

            OnConnectionStateChanged(ConnectionState.Connecting);
            try
            {
                await _client.ConnectAsync(hostname, TcpPort);
                OnConnectionStateChanged(ConnectionState.Connected);

                bool gotStop = false;
                do
                {
                    var request = await _commands.Reader.ReadAsync();
                    switch (request)
                    {
                        case GetRegisterRequest gr:
                            buffer[0] = (byte) PacketType.GetRegisterRequest;
                            buffer[1] = (byte) gr.register;
                            await _client.Client.SendAsync(twoByteBuf, SocketFlags.None);
                            await ReadAsync(twoByteBuf);
                            switch ((PacketType) buffer[0])
                            {
                                case PacketType.GetRegisterResponse:
                                    OnGotRegister(gr.register, buffer[1]);
                                    break;
                                default:
                                    throw new Exception("Unknown response to GetRegisterRequest: " + buffer[0]);
                            }

                            break;
                        case GetBatteryAdcRequest gba:
                            buffer[0] = (byte) PacketType.GetBatteryAdcRequest;
                            await _client.Client.SendAsync(oneByteBuf, SocketFlags.None);
                            await ReadAsync(threeByteBuf);
                            switch ((PacketType) buffer[0])
                            {
                                case PacketType.GetBatteryAdcResponse:
                                    OnGotBatteryAdc(buffer[1] << 8 + buffer[2]);
                                    break;
                                default:
                                    throw new Exception("Unknown response to GetBatteryAdcRequest: " + buffer[0]);
                            }

                            break;
                        case GetUsbHostEnableRequest guhe:
                            buffer[0] = (byte) PacketType.GetUsbHostEnableRequest;
                            await _client.Client.SendAsync(oneByteBuf, SocketFlags.None);
                            await ReadAsync(twoByteBuf);
                            switch ((PacketType) buffer[0])
                            {
                                case PacketType.GetUsbHostEnableResponse:
                                    OnGotUsbHostEnable(buffer[1] != 0);
                                    break;
                                default:
                                    throw new Exception("Unknown response to GetUsbHostEnableRequest: " + buffer[0]);
                            }

                            break;
                        case WriteRegisterRequest wr:
                            buffer[0] = (byte) PacketType.WriteRegisterRequest;
                            buffer[1] = (byte) wr.register;
                            buffer[2] = wr.value;
                            await _client.Client.SendAsync(threeByteBuf, SocketFlags.None);
                            await ReadAsync(oneByteBuf);
                            switch ((PacketType) buffer[0])
                            {
                                case PacketType.WriteRegisterSuccess:
                                    OnRegisterWriteFinished(wr.register, true);
                                    break;
                                case PacketType.WriteRegisterFailure:
                                    OnRegisterWriteFinished(wr.register, false);
                                    break;
                                default:
                                    throw new Exception("Unknown response to WriteRequestRequest: " + buffer[0]);
                            }

                            break;
                        case SetUsbHostEnableRequest suhe:
                            buffer[0] = (byte) PacketType.SetUsbHostEnableRequest;
                            buffer[1] = (byte) (suhe.high ? 1 : 0);
                            await _client.Client.SendAsync(twoByteBuf, SocketFlags.None);
                            await ReadAsync(oneByteBuf);
                            switch ((PacketType) buffer[0])
                            {
                                case PacketType.SetUsbHostEnableAcknowledge:
                                    // nop
                                    break;
                                default:
                                    throw new Exception("Unknown response to WriteRequestRequest: " + buffer[0]);
                            }

                            break;
                        case DisconnectRequest dr:
                            gotStop = true;
                            break;
                    }
                } while (!gotStop);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                if (_client.Connected)
                {
                    OnConnectionStateChanged(ConnectionState.Disconnecting);
                    _client.Close();
                }
            }

            OnConnectionStateChanged(ConnectionState.NotConnected);

            _task = null;
        }
    }
}