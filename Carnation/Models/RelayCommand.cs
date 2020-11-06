using System;
using System.Windows.Input;

namespace Carnation
{
    internal class RelayCommand : ICommand
    {
        private readonly Action _commandAction;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action commandAction, Func<bool> canExecute = null)
        {
            _commandAction = commandAction;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute is null)
            {
                return true;
            }

            return _canExecute.Invoke();
        }

        public void Execute(object parameter)
        {
            _commandAction.Invoke();
        }
    }

    internal class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _commandAction;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<T> commandAction, Func<T, bool> canExecute = null)
        {
            _commandAction = commandAction;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute is null)
            {
                return true;
            }

            return _canExecute.Invoke((T)parameter);
        }

        public void Execute(object parameter)
        {
            _commandAction.Invoke((T)parameter);
        }
    }
}
