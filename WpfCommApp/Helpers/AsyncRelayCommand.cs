using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfCommApp.Helpers;

namespace WpfCommApp
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }

    public class AsyncRelayCommand : IAsyncCommand
    {
        #region Fields
        public event EventHandler CanExecuteChanged;
        //{
        //    add { CommandManager.RequerySuggested += value; }
        //    remove { CommandManager.RequerySuggested -= value; }
        //}

        private bool _isExecuting;
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        #endregion

        #region Constructors
        public AsyncRelayCommand(Func<Task> execute)
            : this(execute, null)
        {
        }

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;
        }

        #endregion

        #region IAsyncCommand Members
        [DebuggerStepThrough]
        public bool CanExecute()
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    _isExecuting = true;
                    RaiseCanExecuteChanged();
                    await Globals.Tasker.Run(_execute);
                }
                finally
                {
                    _isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Explicit Implemtation
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAsync();
        }

        #endregion

        #endregion

    }
}
