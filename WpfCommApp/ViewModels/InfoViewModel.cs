using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;

namespace WpfCommApp
{
    public class InfoViewModel : ObservableObject
    {
        #region Fields

        private bool _run;
        private string _startText;
        private bool _userClosedWindow;
        private string _windowText;

        private ICommand _closing;

        private CancellationToken _ct;
        private CancellationTokenSource _cts;
        private Task _task;
        private InfoView _view;

        #endregion

        #region Properties

        public string Title { get; }

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

        public InfoView View
        {
            get { return _view; }
            set { _view = value; OnPropertyChanged(nameof(View)); }
        }

        public string WindowText
        {
            get { return _windowText; }
            set
            {
                if (_windowText != value)
                {
                    _windowText = value;
                    OnPropertyChanged(nameof(WindowText));
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

        public InfoViewModel(Task task, CancellationToken ct, CancellationTokenSource cts, string title, string text)
        {
            _ct = ct;
            _cts = cts;
            _run = true;
            _startText = text;
            _task = task;
            Title = title;
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
                    WindowText = _startText + new String('.', i++ % 4);
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
