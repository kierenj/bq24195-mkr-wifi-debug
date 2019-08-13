using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BQ24195
{
    public class RegisterFile : ViewModelBase
    {
        private readonly Core _core;
        private ObservableCollection<Register> _registers = new ObservableCollection<Register>();

        internal Core Core => _core;

        public ObservableCollection<Register> Registers
        {
            get => _registers;
            set
            {
                _registers = value;
                OnPropertyChanged();
            }
        }

        public delegate void ReadRequestedHandler(int num);

        public event ReadRequestedHandler ReadRequested;
        private void OnReadRequested(int num) => ReadRequested?.Invoke(num);


        public delegate void WriteRequestedHandler(int num, byte val);

        public event WriteRequestedHandler WriteRequested;
        private void OnWriteRequested(int num, byte val) => WriteRequested?.Invoke(num, val);

        public RegisterFile(Core core)
        {
            _core = core;
            for (var i = 0; i <= 10; i++)
            {
                var reg = new Register(this, i);
                var i1 = i;
                reg.ReadRequested += () => OnReadRequested(i1);
                reg.WriteRequested += (v) => OnWriteRequested(i1, v);
                _registers.Add(reg);
            }
        }

        public ICommand ReadAllCommand => new RelayCommand(ReadAll);

        private void ReadAll()
        {
            for (var i = 0; i <= 10; i++)
            {
                OnReadRequested(i);
            }
        }

        public void GotUpdate(int reg, byte val)
        {
            _registers[reg].GotUpdate(val);
        }
    }
}