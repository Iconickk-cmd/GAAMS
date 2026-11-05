
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

@Component({
  selector: 'app-leave-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-management.html',
  styleUrl: './leave-management.css'
})
export class LeaveManagementComponent implements OnInit {
  protected readonly leaveRequests = signal<LeaveRequest[]>([]);
  protected readonly employees = signal<Employee[]>([]);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');

  protected readonly apiUrl = 'http://localhost:5081/api/LeaveRequests';
  protected readonly employeeApiUrl = 'http://localhost:5081/api/employees';

  protected selectedStatus = 'All';

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

  protected loadLeaveRequests(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.http.get<LeaveRequest[]>(this.apiUrl).subscribe({
      next: (requests) => {
        this.leaveRequests.set(requests);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Failed to load leave requests:', error);
        this.errorMessage.set('Unable to load leave requests. Check that the API is running.');
        this.loading.set(false);
      }
    });
  }

  protected loadEmployees(): void {
    this.http.get<any>(this.employeeApiUrl).subscribe({
      next: (response) => {
        const employees = Array.isArray(response)
          ? response
          : response.employees ?? [];

        this.employees.set(employees.filter(
          (employee: Employee) => employee.isActive
        ));
      },
      error: (error) => {
        console.error('Failed to load employees:', error);
        this.errorMessage.set('Unable to load employees.');
      }
    });
  }

  protected submitLeaveRequest(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (
      this.newRequest.employeeId <= 0 ||
      !this.newRequest.startDate ||
      !this.newRequest.endDate ||
      !this.newRequest.reason.trim()
    ) {
      this.errorMessage.set('Please complete all leave request fields.');
      return;
    }

    this.http.post(this.apiUrl, this.newRequest).subscribe({
      next: () => {
        this.successMessage.set('Leave request submitted successfully.');

        this.newRequest = {
          employeeId: 0,
          leaveType: 'Annual',
          startDate: '',
          endDate: '',
          reason: ''
        };

        this.loadLeaveRequests();
      },
      error: (error) => {
        this.errorMessage.set(
          error.error?.message ?? 'Failed to submit leave request.'
        );
      }
    });
  }

  protected reviewRequest(id: number, status: 'Approved' | 'Rejected'): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    const reviewer = this.employees().find(
      employee => employee.id === 4
    );

    if (!reviewer) {
      this.errorMessage.set('The configured reviewing employee is unavailable.');
      return;
    }

    const body = {
      reviewedByEmployeeId: reviewer.id,
      status,
      reviewComments: this.reviewComments[id] ?? ''
    };

    this.http.put(`${this.apiUrl}/${id}/review`, body).subscribe({
      next: () => {
        this.successMessage.set(`Leave request ${status.toLowerCase()} successfully.`);
        this.loadLeaveRequests();
      },
      error: (error) => {
        this.errorMessage.set(
          error.error?.message ?? 'Unable to review leave request.'
        );
      }
    });
  }

  protected cancelRequest(id: number): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    this.http.delete(`${this.apiUrl}/${id}`).subscribe({
      next: () => {
        this.successMessage.set('Leave request cancelled successfully.');
        this.loadLeaveRequests();
      },
      error: (error) => {
        this.errorMessage.set(
          error.error?.message ?? 'Unable to cancel leave request.'
        );
      }
    });
  }

  protected filteredRequests(): LeaveRequest[] {
    if (this.selectedStatus === 'All') {
      return this.leaveRequests();
    }

    return this.leaveRequests().filter(
      request => request.status === this.selectedStatus
    );
  }

  protected countByStatus(status: string): number {
    return this.leaveRequests().filter(
      request => request.status === status
    ).length;
  }
}
