
namespace GAAMS.Application.Interfaces
{
    public interface ILeaveBalanceService
    {
        Task UpdatePendingDaysAsync(
            int employeeId,
            string leaveType,
            int leaveYear,
            decimal days);

        Task ApproveLeaveAsync(
            int employeeId,
            string leaveType,
            int leaveYear,
            decimal days);

        Task ReleasePendingDaysAsync(
            int employeeId,
            string leaveType,
            int leaveYear,
            decimal days);

        Task<decimal> GetRemainingDaysAsync(
            int employeeId,
            string leaveType,
            int leaveYear);

        Task RefreshBalanceAsync(
            int employeeId,
            string leaveType,
            int leaveYear);
    }
}
