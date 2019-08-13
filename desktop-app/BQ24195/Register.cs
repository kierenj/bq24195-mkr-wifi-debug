using System;
using System.Windows.Input;

namespace BQ24195
{
    public class Register : ViewModelBase
    {
        private readonly RegisterFile _file;
        private byte? _lastReadValue;
        private byte _pendingNewValue;
        private int _number;

        public int Number
        {
            get => _number;
            set
            {
                _number = value;
                OnPropertyChanged();
            }
        }

        public byte? LastReadValue
        {
            get => _lastReadValue;
            set
            {
                _lastReadValue = value;
                OnPropertyChanged();
            }
        }

        public byte PendingNewValue
        {
            get => _pendingNewValue;
            set
            {
                _pendingNewValue = value;
                OnPropertyChanged();
            }
        }

        public Register(RegisterFile file, int number)
        {
            _file = file;
            _number = number;
        }

        public void GotUpdate(byte val)
        {
            LastReadValue = val;
            PendingNewValue = val;
        }

        public delegate void ReadRequestedHandler();

        public event ReadRequestedHandler ReadRequested;
        private void OnReadRequested() => ReadRequested?.Invoke();

        public ICommand ReadCommand => new RelayCommand(Read);

        private void Read()
        {
            OnReadRequested();
        }

        public delegate void WriteRequestedHandler(byte val);

        public event WriteRequestedHandler WriteRequested;
        private void OnWriteRequested(byte val) => WriteRequested?.Invoke(val);

        public ICommand WriteCommand => new RelayCommand(Write);

        private void Write()
        {
            OnWriteRequested(PendingNewValue);
        }
    }
}