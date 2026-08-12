using System;
using System.Windows.Input;

namespace PlaytimeInsights.ViewModels
{
    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T> execute;
        private readonly Predicate<T> canExecute;

        public RelayCommand(
            Action<T> execute,
            Predicate<T> canExecute = null)
        {
            this.execute = execute ??
                throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            T value;
            return TryGetParameter(parameter, out value) &&
                (canExecute == null || canExecute(value));
        }

        public void Execute(object parameter)
        {
            T value;
            if (!TryGetParameter(parameter, out value))
            {
                throw new ArgumentException(
                    string.Format(
                        "Command parameter must be assignable to {0}.",
                        typeof(T).FullName),
                    nameof(parameter));
            }

            execute(value);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private static bool TryGetParameter(object parameter, out T value)
        {
            if (parameter is T)
            {
                value = (T)parameter;
                return true;
            }

            if (parameter == null &&
                (!typeof(T).IsValueType ||
                 Nullable.GetUnderlyingType(typeof(T)) != null))
            {
                value = default(T);
                return true;
            }

            value = default(T);
            return false;
        }
    }
}
