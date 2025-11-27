using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace C2
{
    public class UpdateDispatcher
    {
        private static UpdateDispatcher _instance;
        public static UpdateDispatcher Instance => _instance ??= new UpdateDispatcher();

        private readonly DispatcherTimer _timer;
        private readonly List<Action> _subscribers = new();

        private UpdateDispatcher()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)// 최소 100ms
            };
            _timer.Tick += (s, e) =>
            {
                foreach (var action in _subscribers.ToArray())
                    action.Invoke(); // 뷰모델의 Invoke 함수 호출 
            };
            _timer.Start();
        }

        public void Register(Action updateAction)
        {
            if (!_subscribers.Contains(updateAction))
                _subscribers.Add(updateAction);
        }

        public void Unregister(Action updateAction)
        {
            _subscribers.Remove(updateAction);
        }
    }
}
