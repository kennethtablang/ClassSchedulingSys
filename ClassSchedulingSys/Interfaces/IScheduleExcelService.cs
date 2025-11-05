using ClassSchedulingSys.Models;

namespace ClassSchedulingSys.Interfaces
{
    public interface IScheduleExcelService
    {
        byte[] GenerateScheduleExcel(
            List<Schedule> schedules,
            string pov,
            string id,
            string semesterLabel,
            string schoolYearLabel);

        byte[] GenerateRoomUtilizationExcel(
            List<Schedule> schedules,
            List<Room> allRooms,
            string semesterLabel,
            string schoolYearLabel);
    }
}
