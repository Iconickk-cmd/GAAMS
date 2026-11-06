
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: string;
  reviewedByEmployeeId: number | null;
  reviewerName: string | null;
  reviewedAt: string | null;
  reviewComments: string | null;
  createdAt: string;
}

interface Employee {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
}

interface LeaveBalance {
  id: number;
  employeeId: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  leaveType: string;
  leaveYear: number;
  entitledDays: number;
  accruedDays: number;
  takenDays: number;
  pendingDays: number;
  remainingDays: number;
}

@Component({
  selector: 'app-leave-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-management.html',
  styleUrl: './leave-management.css'
})
export class LeaveManagementComponent implements OnInit {

  // ----------------------------------
  // LEAVE REQUEST DATA
  // ----------------------------------

  protected readonly leaveRequests = signal<LeaveRequest[]>([]);
  protected readonly employees = signal<Employee[]>([]);

  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);

  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');

  protected readonly apiUrl =
    'http://localhost:5081/api/LeaveRequests';

  protected readonly employeeApiUrl =
    'http://localhost:5081/api/employees';

  // ----------------------------------
  // LEAVE BALANCE DATA
  // ----------------------------------

  protected readonly leaveBalances = signal<LeaveBalance[]>([]);

  protected readonly balanceLoading = signal(false);

  protected selectedBalanceEmployeeId = 0;

  protected balanceYear = new Date().getFullYear();

  protected readonly balanceApiUrl =
    'http://localhost:5081/api/LeaveBalances';

  // ----------------------------------
  // FILTERS AND FORM DATA
  // ----------------------------------

  protected selectedStatus = 'All';
  protected searchTerm = '';

  protected reviewerEmployeeId = 0;

  protected newRequest = {
    employeeId: 0,
    leaveType: 'Annual',
    startDate: '',
    endDate: '',
    reason: ''
  };

  protected reviewComments: Record<number, string> = {};

  constructor(private readonly http: HttpClient) { }

  ngOnInit(): void {
    this.loadLeaveRequests();
    this.loadEmployees();
  }

  // ----------------------------------
  // LOAD LEAVE REQUESTS
  // ----------------------------------

  protected loadLeaveRequests(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.http.get<LeaveRequest[]>(this.apiUrl).subscribe({
      next: (requests) => {
        this.leaveRequests.set(
          Array.isArray(requests) ? requests : []
        );

        this.loading.set(false);
      },

      error: (error) => {
        console.error('Failed to load leave requests:', error);

        this.errorMessage.set(
          'Unable to load leave requests. Check that the API is running.'
        );

        this.loading.set(false);
      }
    });
  }

  // ----------------------------------
  // LOAD EMPLOYEES
  // ----------------------------------

  protected loadEmployees(): void {
    this.http.get<any>(this.employeeApiUrl).subscribe({
      next: (response) => {
        const employees: Employee[] = Array.isArray(response)
          ? response
          : response.employees ?? [];

        const activeEmployees = employees.filter(
          employee => employee.isActive
        );

        this.employees.set(activeEmployees);

        // Select the first active employee by default.
        if (
          this.selectedBalanceEmployeeId === 0 &&
          activeEmployees.length > 0
        ) {
          this.selectedBalanceEmployeeId = activeEmployees[0].id;

          this.loadLeaveBalances();
        }
      },

      error: (error) => {
        console.error('Failed to load employees:', error);

        this.errorMessage.set('Unable to load employees.');
      }
    });
  }

  // ----------------------------------
  // LOAD EMPLOYEE LEAVE BALANCES
  // ----------------------------------

  protected loadLeaveBalances(): void {
    if (
      this.selectedBalanceEmployeeId <= 0 ||
      this.balanceYear < 2000
    ) {
      this.leaveBalances.set([]);
      return;
    }

    this.balanceLoading.set(true);

    this.http.get<LeaveBalance[]>(
      `${this.balanceApiUrl}/employee/${this.selectedBalanceEmployeeId}/year/${this.balanceYear}`
    ).subscribe({

      next: (balances) => {
        this.leaveBalances.set(
          Array.isArray(balances) ? balances : []
        );

        this.balanceLoading.set(false);
      },

      error: (error) => {
        console.error('Failed to load leave balances:', error);

        this.leaveBalances.set([]);

        this.balanceLoading.set(false);
      }
    });
  }

  // Called when the selected employee changes.
  protected onBalanceEmployeeChange(): void {
    this.loadLeaveBalances();
  }

  // Called when the selected leave year changes.
  protected onBalanceYearChange(): void {
    this.loadLeaveBalances();
  }

  // ----------------------------------
  // LEAVE BALANCE STATISTICS
  // ----------------------------------

  protected get totalEntitledDays(): number {
    return this.leaveBalances().reduce(
      (total, balance) => total + balance.entitledDays,
      0
    );
  }

  protected get totalAccruedDays(): number {
    return this.leaveBalances().reduce(
      (total, balance) => total + balance.accruedDays,
      0
    );
  }

  protected get totalTakenDays(): number {
    return this.leaveBalances().reduce(
      (total, balance) => total + balance.takenDays,
      0
    );
  }

  protected get totalPendingDays(): number {
    return this.leaveBalances().reduce(
      (total, balance) => total + balance.pendingDays,
      0
    );
  }

  protected get totalRemainingDays(): number {
    return this.leaveBalances().reduce(
      (total, balance) => total + balance.remainingDays,
      0
    );
  }

  // ----------------------------------
  // DASHBOARD STATISTICS
  // ----------------------------------

  protected get totalRequests(): number {
    return this.leaveRequests().length;
  }

  protected get pendingRequests(): number {
    return this.countByStatus('Pending');
  }

  protected get approvedRequests(): number {
    return this.countByStatus('Approved');
  }

  protected get rejectedRequests(): number {
    return this.countByStatus('Rejected');
  }

  protected get cancelledRequests(): number {
    return this.countByStatus('Cancelled');
  }

  protected countByStatus(status: string): number {
    return this.leaveRequests().filter(
      request =>
        request.status?.toLowerCase() === status.toLowerCase()
    ).length;
  }

  // ----------------------------------
  // EMPLOYEE INFORMATION
  // ----------------------------------

  protected employeeName(employeeId: number): string {
    const employee = this.employees().find(
      item => item.id === employeeId
    );

    if (!employee) {
      return 'Employee #' + employeeId;
    }

    return `${employee.firstName} ${employee.lastName}`;
  }

  protected employeeNumber(employeeId: number): string {
    const employee = this.employees().find(
      item => item.id === employeeId
    );

    return employee?.employeeNumber ?? 'N/A';
  }

  protected get selectedBalanceEmployee(): Employee | undefined {
    return this.employees().find(
      employee => employee.id === this.selectedBalanceEmployeeId
    );
  }

  // ----------------------------------
  // LEAVE REQUEST SUBMISSION
  // ----------------------------------

  protected submitLeaveRequest(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (
      this.newRequest.employeeId <= 0 ||
      !this.newRequest.startDate ||
      !this.newRequest.endDate ||
      !this.newRequest.reason.trim()
    ) {
      this.errorMessage.set(
        'Please complete all leave request fields.'
      );
      return;
    }

    const start = new Date(this.newRequest.startDate);
    const end = new Date(this.newRequest.endDate);

    if (isNaN(start.getTime()) || isNaN(end.getTime())) {
      this.errorMessage.set('Please enter valid leave dates.');
      return;
    }

    if (end < start) {
      this.errorMessage.set(
        'The end date cannot be earlier than the start date.'
      );
      return;
    }

    this.submitting.set(true);

    this.http.post(this.apiUrl, this.newRequest).subscribe({

      next: () => {
        this.successMessage.set(
          'Leave request submitted successfully.'
        );

        const requestedEmployeeId = this.newRequest.employeeId;

        this.newRequest = {
          employeeId: 0,
          leaveType: 'Annual',
          startDate: '',
          endDate: '',
          reason: ''
        };

        this.submitting.set(false);

        this.loadLeaveRequests();

        if (
          requestedEmployeeId === this.selectedBalanceEmployeeId
        ) {
          this.loadLeaveBalances();
        }
      },

      error: (error) => {
        this.errorMessage.set(
          error.error?.message ??
          'Failed to submit leave request.'
        );

        this.submitting.set(false);
      }
    });
  }

  // ----------------------------------
  // APPROVE / REJECT REQUESTS
  // ----------------------------------

  protected reviewRequest(
    id: number,
    status: 'Approved' | 'Rejected'
  ): void {

    this.errorMessage.set('');
    this.successMessage.set('');

    if (this.reviewerEmployeeId <= 0) {
      this.errorMessage.set(
        'Please select the reviewing employee.'
      );
      return;
    }

    const reviewer = this.employees().find(
      employee => employee.id === this.reviewerEmployeeId
    );

    if (!reviewer) {
      this.errorMessage.set(
        'The selected reviewing employee is unavailable.'
      );
      return;
    }

    const request = this.leaveRequests().find(
      item => item.id === id
    );

    const body = {
      reviewedByEmployeeId: reviewer.id,
      status,
      reviewComments: this.reviewComments[id] ?? ''
    };

    this.http.put(
      `${this.apiUrl}/${id}/review`,
      body
    ).subscribe({

      next: () => {
        this.successMessage.set(
          `Leave request ${status.toLowerCase()} successfully.`
        );

        this.loadLeaveRequests();

        if (
          request?.employeeId === this.selectedBalanceEmployeeId
        ) {
          this.loadLeaveBalances();
        }
      },

      error: (error) => {
        this.errorMessage.set(
          error.error?.message ??
          'Unable to review leave request.'
        );
      }
    });
  }

  // ----------------------------------
  // CANCEL LEAVE REQUEST
  // ----------------------------------

  protected cancelRequest(id: number): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (!confirm('Are you sure you want to cancel this leave request?')) {
      return;
    }

    const request = this.leaveRequests().find(
      item => item.id === id
    );

    this.http.delete(`${this.apiUrl}/${id}`).subscribe({

      next: () => {
        this.successMessage.set(
          'Leave request cancelled successfully.'
        );

        this.loadLeaveRequests();

        if (
          request?.employeeId === this.selectedBalanceEmployeeId
        ) {
          this.loadLeaveBalances();
        }
      },

      error: (error) => {
        this.errorMessage.set(
          error.error?.message ??
          'Unable to cancel leave request.'
        );
      }
    });
  }

  // ----------------------------------
  // SEARCH AND FILTER
  // ----------------------------------

  protected filteredRequests(): LeaveRequest[] {
    let requests = this.leaveRequests();

    if (this.selectedStatus !== 'All') {
      requests = requests.filter(
        request =>
          request.status?.toLowerCase() ===
          this.selectedStatus.toLowerCase()
      );
    }

    const search = this.searchTerm.trim().toLowerCase();

    if (search) {
      requests = requests.filter(request => {
        const employee = this.employeeName(request.employeeId);

        return (
          employee.toLowerCase().includes(search) ||
          request.leaveType.toLowerCase().includes(search) ||
          request.status.toLowerCase().includes(search) ||
          String(request.id).includes(search)
        );
      });
    }

    return requests;
  }

  // ----------------------------------
  // DISPLAY HELPERS
  // ----------------------------------

  protected statusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'approved':
        return 'approved';

      case 'rejected':
        return 'rejected';

      case 'pending':
        return 'pending';

      case 'cancelled':
        return 'cancelled';

      default:
        return 'default';
    }
  }

  protected clearMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }
}
