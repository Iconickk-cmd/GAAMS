
import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Department {
  id: number;
  name: string;
  code: string;
}

interface Employee {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  jobTitle: string;
  departmentId: number;
  isActive: boolean;
  department?: Department;
}

interface EmployeeResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  employees: Employee[];
}

interface Position {
  id: number;
  positionCode: string;
  title: string;
  departmentId: number;
  department?: Department;
  approvedHeadcount: number;
  currentOccupancy: number;
  isActive: boolean;
}

interface PositionAssignment {
  id: number;
  employeeId: number;
  employee?: Employee;
  positionId: number;
  position?: Position;
  startDate: string;
  endDate?: string | null;
  changeReason?: string;
  isCurrent: boolean;
}

@Component({
  selector: 'app-position-assignments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './position-assignments.html',
  styleUrl: './position-assignments.css'
})
export class PositionAssignmentsComponent implements OnInit {

  private readonly apiUrl = 'http://localhost:5081/api';

  assignments = signal<PositionAssignment[]>([]);
  employees = signal<Employee[]>([]);
  positions = signal<Position[]>([]);
  departments = signal<Department[]>([]);

  loading = signal(false);
  saving = signal(false);

  errorMessage = signal('');
  successMessage = signal('');

  searchTerm = '';

  showForm = false;
  showTransferForm = false;

  form = this.emptyForm();
  transferForm = this.emptyTransferForm();

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.refreshData();
  }

  private emptyForm() {
    return {
      employeeId: null as number | null,
      positionId: null as number | null,
      startDate: new Date().toISOString().slice(0, 10),
      changeReason: 'Initial appointment'
    };
  }

  private emptyTransferForm() {
    return {
      employeeId: null as number | null,
      newPositionId: null as number | null,
      effectiveDate: new Date().toISOString().slice(0, 10),
      changeReason: 'Position transfer'
    };
  }

  loadEmployees(): void {
    this.http.get<EmployeeResponse>(
      `${this.apiUrl}/Employees?page=1&pageSize=100`
    ).subscribe({
      next: response => {
        this.employees.set(
          response.employees.filter(employee => employee.isActive)
        );
      },
      error: () => {
        this.errorMessage.set('Unable to load employees.');
      }
    });
  }

  loadPositions(): void {
    this.http.get<Position[]>(
      `${this.apiUrl}/Positions`
    ).subscribe({
      next: data => {
        this.positions.set(
          data.filter(position => position.isActive)
        );
      },
      error: () => {
        this.errorMessage.set('Unable to load positions.');
      }
    });
  }

  loadDepartments(): void {
    this.http.get<Department[]>(
      `${this.apiUrl}/Departments`
    ).subscribe({
      next: data => {
        this.departments.set(data);
      },
      error: () => {
        this.errorMessage.set('Unable to load departments.');
      }
    });
  }

  loadAssignments(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.http.get<PositionAssignment[]>(
      `${this.apiUrl}/EmployeePositionHistories`
    ).subscribe({
      next: data => {
        this.assignments.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set(
          'Unable to load employee position assignments.'
        );
        this.loading.set(false);
      }
    });
  }

  get currentAssignments(): PositionAssignment[] {
    return this.assignments().filter(a => a.isCurrent);
  }

  get previousAssignments(): PositionAssignment[] {
    return this.assignments().filter(a => !a.isCurrent);
  }

  get filteredAssignments(): PositionAssignment[] {
    const term = this.searchTerm.trim().toLowerCase();

    if (!term) {
      return this.currentAssignments;
    }

    return this.currentAssignments.filter(a => {
      const employeeName =
        `${a.employee?.firstName ?? ''} ${a.employee?.lastName ?? ''}`
          .toLowerCase();

      const positionTitle =
        (a.position?.title ?? '').toLowerCase();

      const employeeNumber =
        (a.employee?.employeeNumber ?? '').toLowerCase();

      return employeeName.includes(term) ||
        positionTitle.includes(term) ||
        employeeNumber.includes(term);
    });
  }

  get totalCurrentAssignments(): number {
    return this.currentAssignments.length;
  }

  get totalPreviousAssignments(): number {
    return this.previousAssignments.length;
  }

  get availableEmployees(): Employee[] {
    const assignedEmployeeIds = new Set(
      this.currentAssignments.map(a => a.employeeId)
    );

    return this.employees().filter(employee =>
      employee.isActive &&
      !assignedEmployeeIds.has(employee.id)
    );
  }

  get assignedEmployees(): Employee[] {
    return this.currentAssignments
      .filter(a => !!a.employee)
      .map(a => a.employee!);
  }

  get availablePositions(): Position[] {
    return this.positions().filter(position =>
      position.currentOccupancy < position.approvedHeadcount
    );
  }

  getDepartmentName(departmentId: number): string {
    return this.departments().find(
      department => department.id === departmentId
    )?.name ?? 'Department not found';
  }

  openForm(): void {
    this.form = this.emptyForm();
    this.errorMessage.set('');
    this.successMessage.set('');
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.form = this.emptyForm();
  }

  assignEmployee(): void {
    if (
      !this.form.employeeId ||
      !this.form.positionId ||
      !this.form.startDate
    ) {
      this.errorMessage.set(
        'Select an employee, position and start date.'
      );
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const payload = {
      employeeId: this.form.employeeId,
      positionId: this.form.positionId,
      startDate: this.form.startDate,
      changeReason: this.form.changeReason.trim() ||
        'Initial appointment'
    };

    this.http.post(
      `${this.apiUrl}/EmployeePositionHistories`,
      payload
    ).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeForm();

        this.successMessage.set(
          'Employee assigned successfully.'
        );

        this.refreshData();
      },
      error: error => {
        this.saving.set(false);

        this.errorMessage.set(
          error.error?.message ??
          'Unable to assign employee. Check the employee and position.'
        );
      }
    });
  }

  openTransferForm(): void {
    this.transferForm = this.emptyTransferForm();
    this.errorMessage.set('');
    this.successMessage.set('');
    this.showTransferForm = true;
  }

  closeTransferForm(): void {
    this.showTransferForm = false;
    this.transferForm = this.emptyTransferForm();
  }

  transferEmployee(): void {
    if (
      !this.transferForm.employeeId ||
      !this.transferForm.newPositionId ||
      !this.transferForm.effectiveDate
    ) {
      this.errorMessage.set(
        'Select an employee, destination position and effective date.'
      );
      return;
    }

    const employee = this.assignedEmployees.find(
      e => e.id === this.transferForm.employeeId
    );

    const destination = this.availablePositions.find(
      p => p.id === this.transferForm.newPositionId
    );

    if (!employee || !destination) {
      this.errorMessage.set(
        'The selected employee or destination position is no longer available.'
      );
      return;
    }

    const confirmed = confirm(
      `Transfer ${employee.firstName} ${employee.lastName} to ${destination.title}?`
    );

    if (!confirmed) {
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const payload = {
      employeeId: this.transferForm.employeeId,
      newPositionId: this.transferForm.newPositionId,
      effectiveDate: this.transferForm.effectiveDate,
      changeReason: this.transferForm.changeReason.trim() ||
        'Position transfer'
    };

    this.http.post(
      `${this.apiUrl}/EmployeePositionHistories/transfer`,
      payload
    ).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeTransferForm();

        this.successMessage.set(
          'Employee transfer completed successfully.'
        );

        this.refreshData();
      },
      error: error => {
        this.saving.set(false);

        this.errorMessage.set(
          error.error?.message ??
          'Unable to complete employee transfer.'
        );
      }
    });
  }

  endAssignment(assignment: PositionAssignment): void {
    const employeeName =
      `${assignment.employee?.firstName ?? ''} ${assignment.employee?.lastName ?? ''}`
        .trim();

    if (!confirm(
      `End the current assignment for ${employeeName}?`
    )) {
      return;
    }

    const endDate = prompt(
      'Enter the assignment end date (YYYY-MM-DD):',
      new Date().toISOString().slice(0, 10)
    );

    if (!endDate) {
      return;
    }

    if (!/^\d{4}-\d{2}-\d{2}$/.test(endDate)) {
      this.errorMessage.set(
        'Enter the date in YYYY-MM-DD format.'
      );
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');

    this.http.put(
      `${this.apiUrl}/EmployeePositionHistories/${assignment.id}/end`,
      { endDate }
    ).subscribe({
      next: () => {
        this.successMessage.set(
          'Employee assignment ended successfully.'
        );

        this.refreshData();
      },
      error: error => {
        this.errorMessage.set(
          error.error?.message ??
          'Unable to end employee assignment.'
        );
      }
    });
  }

  refreshData(): void {
    this.loadEmployees();
    this.loadPositions();
    this.loadDepartments();
    this.loadAssignments();
  }
}
