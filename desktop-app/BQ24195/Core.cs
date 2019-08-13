using System;
using System.Windows;
using System.Windows.Input;

namespace BQ24195
{
    public class Core : ViewModelBase
    {
        private readonly Client _client;

        private string _hostname = "192.168.1.252";
        private ConnectionState _state;
        private string _statusMessage;

        private RegisterFile _registerFile;
        private int? _batteryAdc;
        private bool? _usbHostEnable;

        public int? BatteryAdc
        {
            get => _batteryAdc;
            set
            {
                _batteryAdc = value;
                OnPropertyChanged();
            }
        }

        public RegisterFile RegisterFile
        {
            get => _registerFile;
            set
            {
                _registerFile = value;
                OnPropertyChanged();
            }
        }

        public bool? UsbHostEnable
        {
            get => _usbHostEnable;
            set
            {
                _usbHostEnable = value;
                OnPropertyChanged();
            }
        }

        public string Hostname
        {
            get => _hostname;
            set
            {
                _hostname = value;
                OnPropertyChanged();
            }
        }

        public ConnectionState State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public Core()
        {
            _registerFile = new RegisterFile(this);
            _registerFile.ReadRequested += num => _client.GetRegister(num);
            _registerFile.WriteRequested += (num, val) => _client.WriteRegister(num, val);

            _client = new Client();
            _client.ConnectionStateChanged += cs => State = cs;
            _client.Error += e => StatusMessage = e.Message;
            _client.GotBatteryAdc += v => BatteryAdc = v;
            _client.GotRegister += (r, v) => RegisterFile.GotUpdate(r, v);
            _client.GotUsbHostEnable += e => UsbHostEnable = e;
            _client.RegisterWriteFinished += (n, ok) => { };
        }

        public ICommand ConnectCommand => new RelayCommand(Connect);

        private void Connect()
        {
            StatusMessage = "";
            _client.Connect(_hostname);
        }

        public ICommand DisconnectCommand => new RelayCommand(Disconnect);

        private void Disconnect()
        {
            _client.Disconnect();
        }

        public ICommand GetBatteryAdcCommand => new RelayCommand(GetBatteryAdc);

        private void GetBatteryAdc()
        {
            _client.GetBatteryAdc();
        }

        public ICommand GetUsbHostEnableCommand => new RelayCommand(GetUsbHostEnable);

        private void GetUsbHostEnable()
        {
            _client.GetUsbHostEnable();
        }

        public ICommand EnableUsbHostEnableCommand => new RelayCommand(EnableUsbHostEnable);

        private void EnableUsbHostEnable()
        {
            _client.SetUsbHostEnable(true);
            GetUsbHostEnable();
        }

        public ICommand DisableUsbHostEnableCommand => new RelayCommand(DisableUsbHostEnable);

        private void DisableUsbHostEnable()
        {
            _client.SetUsbHostEnable(false);
            GetUsbHostEnable();
        }
    }
}