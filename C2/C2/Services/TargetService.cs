using C2.Messages;
using C2.Models;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2.Services
{
    public class TargetService
    {
        private static TargetService? _instance;
        public static TargetService Instance => _instance ??= new TargetService();

        private readonly Dictionary<char, Target> _targets = new();

        public Target? SelectedTarget {get; private set;}  

        private readonly object _lock = new();

        public TargetService() { 
        }

        // 0.1초마다 UI 갱신 시 이 리스트를 가져감
        public List<Target> GetAllTargets()
        {
            lock (_lock)
                return new List<Target>(_targets.Values);
        }
        public Target? GetTarget(char id)
        {
            _targets.TryGetValue(id, out var missile);
            return missile;
        }
        // 외부 통신 모듈이 호출 (표적 데이터 수신)
        public void ReceiveTargetData(Target newTarget)
        {
            lock (_lock)
            {
                if (_targets.TryGetValue(newTarget.Id, out var existing))
                {
                    existing.Update(newTarget);
                }
                else
                {
                    _targets[newTarget.Id] = newTarget;
                }
            }
        }

        // (선택) 특정 표적 제거
        public void RemoveTarget(char id)
        {
            lock (_lock)
            {
                _targets.Remove(id);
            }
        }

        //public void SelectTarget(char id)
        //{
        //    SelectedTarget = _targets[id];
        //    WeakReferenceMessenger.Default.Send(new TargetSelectedMessage(true));
        //}

        public bool SelectTarget(char id)
        {
            if (!_targets.TryGetValue(id, out var target))
                return false;

            SelectedTarget = target;
            WeakReferenceMessenger.Default.Send(new TargetSelectedMessage(id));
            return true;
        }
        public void ClearTarget()
        {
            SelectedTarget = null;
            WeakReferenceMessenger.Default.Send(new TargetSelectedMessage(null));
        }
    }


}

