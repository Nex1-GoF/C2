using C2.Models;
using C2.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace C2.ViewModels
{
    public class TargetViewModel
    {
        public ObservableCollection<Target> Targets { get; } = new();
        private readonly TargetService _service;
        public TargetViewModel()
        {
            _service = TargetService.Instance;
            for (int i = 1; i <= 4; i++)
            {
                var target = new Target
                    (
                        (char)('A' + i - 1),
                        100,
                        200,
                        0,
                        (37, 105),
                        DateTime.Now,
                        (38, 100)
                    );
                Targets.Add (target);
            }
        }
        private void UpdateTargets(object? sender, EventArgs e)
        {
            var latest = _service.GetAllTargets();

            // 기존 Target 객체 재사용
            foreach (var updated in latest)
            {
                var existing = Targets.FirstOrDefault(t => t.Id == updated.Id);
                if (existing == null)
                    Targets.Add(updated);
                else
                    existing.Update(updated);
            }

            // UI에 남아 있지만 실제 서비스엔 없는 표적 제거
            for (int i = Targets.Count - 1; i >= 0; i--)
            {
                if (!latest.Any(t => t.Id == Targets[i].Id))
                    Targets.RemoveAt(i);
            }
        }
    }
}