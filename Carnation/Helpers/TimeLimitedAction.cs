using System;

namespace Carnation.Helpers
{
    internal class TimeLimitedAction
    {
        private readonly Action _action;
        private readonly TimeSpan _timeBetweenExecution;
        private DateTime _lastExecution = DateTime.MinValue;

        public TimeLimitedAction(Action action, TimeSpan timeBetweenExecution)
        {
            _action = action;
            _timeBetweenExecution = timeBetweenExecution;
        }

        public bool TryToExecute()
        {
            var now = DateTime.Now;
            var timeSinceLastExecution = now - _lastExecution;

            if (timeSinceLastExecution >= _timeBetweenExecution)
            {
                _action();
                _lastExecution = now;
                return true;
            }

            return false;
        }
    }
}
