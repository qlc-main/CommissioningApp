using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;

namespace WpfCommApp
{
    public class SerialDisconnectViewModel : ObservableObject
    {
        #region Fields

        private ICommand _closing;
        private CancellationToken _ct;
        private CancellationTokenSource _cts;
        private string _disconnectText;
        private bool _run;
        private Task _task;
        private bool _userClosedWindow;
        private SerialDisconnectView _view;

        #endregion

        #region Properties

        public string DisconnectText
        {
            get { return _disconnectText; }
            set
            {
                if (_disconnectText != value)
                {
                    _disconnectText = value;
                    OnPropertyChanged(nameof(DisconnectText));
                }
            }
        }

        public SerialDisconnectView View
        {
            get { return _view; }
            set { _view = value; OnPropertyChanged(nameof(View)); }
        }

        public bool UserClosedWindow
        {
            get { return _userClosedWindow; }
            set
            {
                if (_userClosedWindow != value)
                {
                    _userClosedWindow = value;
                    OnPropertyChanged(nameof(UserClosedWindow));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand Closing
        {
            get
            {
                if (_closing == null)
                    _closing = new RelayCommand(p => StopPolling());

                return _closing;
            }
        }

        #endregion

        #region Constructors

        public SerialDisconnectViewModel(Task task, CancellationToken ct, CancellationTokenSource cts)
        {
            _task = task;
            _ct = ct;
            _cts = cts;
            _run = true;
            _userClosedWindow = true;
        }

        #endregion
        
        #region Methods

        public async Task<bool> Poll()
        {
            int i = 0;
            try
            {
                do
                {
                    DisconnectText = "Attempting to Reconnect" + new String('.', i++ % 4);
                }
                while (!_task.Wait(1000) && _run);
            }
            finally
            {
                _cts.Dispose();
            }

            if (!_run)
            {
                _run = true;
                return false;
            }
            else
                return true;
        }

        public void StopPolling()
        {
            _cts.Cancel();
            _run = false;
        }

        #endregion
    }
}
