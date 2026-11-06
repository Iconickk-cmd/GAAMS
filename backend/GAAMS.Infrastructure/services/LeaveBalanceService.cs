
using GAAMS.Application.Interfaces;
using GAAMS.Domain.Entities;
using GAAMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.Infrastructure.Services
{
    public class LeaveBalanceService : ILeaveBalanceService
    {
        private readonly GAAMSDbContext _context;

        public LeaveBalanceService(GAAMSDbContext context)
        {
            _context = context;
        }

        // Calculate the number of completed months between two dates.
        private static int GetCompletedMonths(
            DateTime startDate,
            DateTime endDate)
        {
            if (endDate < startDate)
                return 0;

            int months =
                (endDate.Year - startDate.Year) * 12
                + endDate.Month - startDate.Month;

            if (startDate.AddMonths(months) > endDate)
                months--;

            return Math.Max(0, months);
        }

        // Calculate accrued leave using completed months.
        private static decimal CalculateAccruedDays(
            LeaveEntitlement entitlement,
            DateTime employmentDate)
        {
            DateTime yearStart =
                new DateTime(entitlement.LeaveYear, 1, 1);

            DateTime yearEnd =
                new DateTime(entitlement.LeaveYear, 12, 31);

            DateTime startDate = new[]
            {
                employmentDate.Date,
                entitlement.EffectiveFrom.Date,
                yearStart
            }.Max();

            DateTime endDate = new[]
            {
                DateTime.UtcNow.Date,
                entitlement.EffectiveTo?.Date ?? yearEnd,
                yearEnd
            }.Min();

            int completedMonths =
                GetCompletedMonths(startDate, endDate);

            decimal monthlyRate =
                entitlement.EntitlementDays / 12m;

            decimal accruedDays =
                completedMonths * monthlyRate;

            return Math.Min(
                accruedDays,
                entitlement.EntitlementDays);
        }

        // Find or create an employee's leave balance.
        private async Task<LeaveBalance> GetOrCreateBalanceAsync(
            int employeeId,
            string leaveType,
            int leaveYear)
        {
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b =>
                    b.EmployeeId == employeeId &&
                    b.LeaveType == leaveType &&
                    b.LeaveYear == leaveYear);

            if (balance != null)
                return balance;

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                throw new InvalidOperationException(
                    "Employee not found.");

            var entitlement = await _context.LeaveEntitlements
                .FirstOrDefaultAsync(e =>
                    e.EmployeeId == employeeId &&
                    e.LeaveType == leaveType &&
                    e.LeaveYear == leaveYear &&
                    e.IsActive);

            if (entitlement == null)
                throw new InvalidOperationException(
                    "No active leave entitlement exists for this employee, leave type and year.");

            decimal accruedDays = CalculateAccruedDays(
                entitlement,
                employee.DateOfEmployment);

            balance = new LeaveBalance
            {
                EmployeeId = employeeId,
                LeaveType = leaveType,
                LeaveYear = leaveYear,
                EntitledDays = entitlement.EntitlementDays,
                AccruedDays = accruedDays,
                TakenDays = 0,
                PendingDays = 0,
                RemainingDays = accruedDays,
                CreatedAt = DateTime.UtcNow
            };

            _context.LeaveBalances.Add(balance);

            await _context.SaveChangesAsync();

            return balance;
        }

        // Reserve days when a leave request is submitted.
        public async Task UpdatePendingDaysAsync(
            int employeeId,
            string leaveType,
            int leaveYear,
            decimal days)
        {
            if (days <= 0)
                throw new ArgumentException(
                    "Leave days must be greater than zero.");

            // Refresh the balance before checking availability.
            await RefreshBalanceAsync(
                employeeId, leaveType, leaveYear);

            var balance = await GetOrCreateBalanceAsync(
                employeeId, leaveType, leaveYear);

            decimal availableDays =
                balance.AccruedDays
                - balance.TakenDays
                - balance.PendingDays;

            if (availableDays < days)
                throw new InvalidOperationException(
                    $"Insufficient available leave balance. " +
                    $"Available: {Math.Max(0m, availableDays):0.00} days. " +
                    $"Requested: {days:0.00} days.");

            balance.PendingDays += days;

            RecalculateRemainingDays(balance);

            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Move pending days into taken days after approval.
        public async Task ApproveLeaveAsync(
            int employeeId,
            string leaveType,
            int leaveYear,
            decimal days)
        {
            if (days <= 0)
                throw new InvalidOperationException(
                    "Leave days must be greater than zero.");

            // Refresh the balance before approval.
            await RefreshBalanceAsync(
                employeeId, leaveType, leaveYear);

            var balance = await GetOrCreateBalanceAsync(
                employeeId, leaveType, leaveYear);

            if (balance.PendingDays < days)
                throw new InvalidOperationException(
                    "The pending leave balance is insufficient.");

            // Prevent approval when total committed leave
            // exceeds the employee's accrued entitlement.
            decimal totalCommittedDays =
                balance.TakenDays + balance.PendingDays;

            if (totalCommittedDays > balance.AccruedDays)
                throw new InvalidOperationException(
                    $"Leave approval cannot proceed because total committed leave " +
                    $"({totalCommittedDays:0.00} days) exceeds accrued leave " +
                    $"({balance.AccruedDays:0.00} days).");

            balance.PendingDays -= days;
            balance.TakenDays += days;

            RecalculateRemainingDays(balance);

            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Release reserved days after rejection or cancellation.
        public async Task ReleasePendingDaysAsync(
            int employeeId,
            string leaveType,
            int leaveYear,
            decimal days)
        {
            if (days <= 0)
                throw new InvalidOperationException(
                    "Leave days must be greater than zero.");

            // Refresh the balance before releasing the reservation.
            await RefreshBalanceAsync(
                employeeId, leaveType, leaveYear);

            var balance = await GetOrCreateBalanceAsync(
                employeeId, leaveType, leaveYear);

            if (balance.PendingDays < days)
                throw new InvalidOperationException(
                    "The pending leave balance is insufficient.");

            balance.PendingDays -= days;

            RecalculateRemainingDays(balance);

            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Retrieve the employee's current remaining leave.
        public async Task<decimal> GetRemainingDaysAsync(
            int employeeId,
            string leaveType,
            int leaveYear)
        {
            await RefreshBalanceAsync(
                employeeId, leaveType, leaveYear);

            var balance = await _context.LeaveBalances
                .FirstAsync(b =>
                    b.EmployeeId == employeeId &&
                    b.LeaveType == leaveType &&
                    b.LeaveYear == leaveYear);

            return balance.RemainingDays;
        }

        // Refresh annual entitlement and accrued leave.
        public async Task RefreshBalanceAsync(
            int employeeId,
            string leaveType,
            int leaveYear)
        {
            var balance = await GetOrCreateBalanceAsync(
                employeeId, leaveType, leaveYear);

            var entitlement = await _context.LeaveEntitlements
                .FirstOrDefaultAsync(e =>
                    e.EmployeeId == employeeId &&
                    e.LeaveType == leaveType &&
                    e.LeaveYear == leaveYear &&
                    e.IsActive);

            if (entitlement == null)
                throw new InvalidOperationException(
                    "No active leave entitlement exists.");

            var employee = await _context.Employees
                .FirstAsync(e => e.Id == employeeId);

            balance.EntitledDays = entitlement.EntitlementDays;

            balance.AccruedDays = CalculateAccruedDays(
                entitlement,
                employee.DateOfEmployment);

            RecalculateRemainingDays(balance);

            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // Recalculate the remaining leave balance.
        private static void RecalculateRemainingDays(
            LeaveBalance balance)
        {
            decimal remaining =
                balance.AccruedDays
                - balance.TakenDays
                - balance.PendingDays;

            balance.RemainingDays = Math.Max(0m, remaining);
        }
    }
}
