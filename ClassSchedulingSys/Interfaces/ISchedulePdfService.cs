// Interfaces/ISchedulePdfService.cs
using ClassSchedulingSys.Models;

namespace ClassSchedulingSys.Interfaces
{
    public interface ISchedulePdfService
    {
        byte[] GenerateSchedulePdf(List<Schedule> schedules, string pov, string id);
    }
}
