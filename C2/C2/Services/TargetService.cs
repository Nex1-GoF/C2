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
        private readonly CancellationTokenSource _cts = new();
        private LogService _logService = LogService.Instance;
        public Target? SelectedTarget {get; private set;}

        private readonly TimeSpan _removeThreshold = TimeSpan.FromSeconds(2.0);  // 2초동안 타겟 정보가 변경되지않으면 타겟 소실 처리함
        private readonly TimeSpan _checkInterval = TimeSpan.FromMilliseconds(500);

        private readonly object _lock = new();

        public TargetService() {
            Task.Run(() => MonitorTargetsAsync(_cts.Token));
        }
        private async Task MonitorTargetsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    List<char> toRemove = new();

                    var now = DateTime.UtcNow;
                    foreach (var kvp in _targets)
                    {
                        var t = kvp.Value;
                        if ((now - t.DetectTime) > _removeThreshold)
                            toRemove.Add(kvp.Key);
                    }

                    foreach (var id in toRemove)
                    {
                        RemoveTarget(id);
                    }

                    // 선택된 타겟이 사라졌다면 초기화
                    if (SelectedTarget != null && !_targets.ContainsKey(SelectedTarget.Id))
                        SelectedTarget = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TargetService] 표적 감시 모니터 오류: {ex.Message}");
                }

                await Task.Delay(_checkInterval, token);
            }
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
            //lock (_lock)
            {
                if (_targets.TryGetValue(newTarget.Id, out var existing))
                {
                    existing.Update(newTarget);
                }
                else
                {
                    _targets[newTarget.Id] = newTarget;
                    WeakReferenceMessenger.Default.Send(new TargetCreatedMessage(newTarget.Id));

                }
            }
        }

        // (선택) 특정 표적 제거
        public void RemoveTarget(char id)
        {
            _logService.AddLog(MessageType.System, "표적 소실이 발생했습니다.");
            _targets.Remove(id);
            WeakReferenceMessenger.Default.Send(new TargetRemovedMessage(id));

            if (SelectedTarget?.Id == id)
                SelectedTarget = null;
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

