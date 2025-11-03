using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace C2.Services
{
    public class UpdateDispatcherService
    {
        private static UpdateDispatcherService _instance;
        public static UpdateDispatcherService Instance => _instance ??= new UpdateDispatcherService();

        private readonly DispatcherTimer _timer;
        private readonly List<Action> _subscribers = new();

        private UpdateDispatcherService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 일단 100ms 주기로 실행
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
