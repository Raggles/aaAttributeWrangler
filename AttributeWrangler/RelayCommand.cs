using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Input;

//Event Design: http://msdn.microsoft.com/en-us/library/ms229011.aspx

namespace MicroMvvm
{
    public class RelayCommand<T> : ICommand where T : class
    {

        readonly Action<T> _execute;
        readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute) : this(execute, null) { }
        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute; _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {

            return _canExecute == null ? true : _canExecute(parameter as T);
        }

        public void Execute(object parameter) { _execute(parameter as T); }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
