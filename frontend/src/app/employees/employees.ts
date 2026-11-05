
import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';

interface Department {
  id: number;
  name: string;
}

interface Employee {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  nationalId?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  dateOfBirth: string;
  dateOfEmployment: string;
  jobTitle?: string | null;
  departmentId: number;
  department?: Department | null;
  isActive: boolean;
}

interface EmployeeResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  employees: Employee[];
}

interface EmployeeRequest {
  employeeNumber: string;
  firstName: string;
  lastName: string;
  nationalId: string | null;
  email: string | null;
  phoneNumber: string | null;
  dateOfBirth: string;
  dateOfEmployment: string;
  jobTitle: string | null;
  departmentId: number;
  isActive: boolean;
}

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './employees.html',
  styleUrl: './employees.css'
})
export class EmployeesComponent implements OnInit {

  private readonly apiUrl = 'http://localhost:5081/api/employees';
  private readonly departmentsUrl = 'http://localhost:5081/api/departments';

  employees = signal<Employee[]>([]);
  departments = signal<Department[]>([]);

  totalCount = signal(0);
  totalPages = signal(0);
  page = signal(1);
  pageSize = signal(10);

  loading = signal(false);
  error = signal('');
  successMessage = signal('');
  saveError = signal('');

  showForm = signal(false);
  saving = signal(false);
  isEditing = signal(false);
  editingEmployeeId = signal<number | null>(null);

  viewingEmployee = signal<Employee | null>(null);

  searchTerm = signal('');
  selectedDepartment = signal<number | null>(null);
  selectedStatus = signal('');
  sortBy = signal('lastName');

  today = new Date().toISOString().split('T')[0];

  newEmployee: EmployeeRequest = this.emptyForm();

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadDepartments();
    this.loadEmployees();
  }

  private emptyForm(): EmployeeRequest {
    return {
      employeeNumber: '',
      firstName: '',
      lastName: '',
      nationalId: null,
      email: null,
      phoneNumber: null,
      dateOfBirth: '',
      dateOfEmployment: '',
      jobTitle: null,
      departmentId: 0,
      isActive: true
    };
  }

  // LOAD EMPLOYEES
  loadEmployees(): void {
    this.loading.set(true);
    this.error.set('');

    let params = new HttpParams()
      .set('page', this.page())
      .set('pageSize', this.pageSize())
      .set('sortBy', this.sortBy());

    if (this.searchTerm().trim()) {
      params = params.set('search', this.searchTerm().trim());
    }

    const departmentId = this.selectedDepartment();

    if (departmentId !== null) {
      params = params.set('departmentId', departmentId);
    }

    if (this.selectedStatus() !== '') {
      params = params.set('isActive', this.selectedStatus());
    }

    this.http.get<EmployeeResponse>(this.apiUrl, { params }).subscribe({
      next: (response) => {
        this.employees.set(response.employees);
        this.totalCount.set(response.totalCount);
        this.totalPages.set(response.totalPages);
        this.page.set(response.page);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading employees:', err);
        this.error.set('Unable to load employees. Please check that the API is running.');
        this.loading.set(false);
      }
    });
  }

  // LOAD DEPARTMENTS
  loadDepartments(): void {
    this.http.get<Department[]>(this.departmentsUrl).subscribe({
      next: (departments) => {
        this.departments.set(departments);
      },
      error: (err) => {
        console.error('Error loading departments:', err);
      }
    });
  }

  // SEARCH
  searchEmployees(): void {
    this.page.set(1);
    this.loadEmployees();
  }

  // FILTER
  filterEmployees(): void {
    this.page.set(1);
    this.loadEmployees();
  }

  // SORT
  changeSort(value?: string): void {
    if (value !== undefined) {
      this.sortBy.set(value);
    }

    this.page.set(1);
    this.loadEmployees();
  }

  // CLEAR FILTERS
  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedDepartment.set(null);
    this.selectedStatus.set('');
    this.sortBy.set('lastName');
    this.page.set(1);

    this.loadEmployees();
  }

  // PREVIOUS PAGE
  previousPage(): void {
    if (this.page() > 1) {
      this.page.update(value => value - 1);
      this.loadEmployees();
    }
  }

  // NEXT PAGE
  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update(value => value + 1);
      this.loadEmployees();
    }
  }

  // OPEN ADD EMPLOYEE FORM
  openForm(): void {
    this.closeView();

    this.isEditing.set(false);
    this.editingEmployeeId.set(null);
    this.newEmployee = this.emptyForm();
    this.saveError.set('');

    this.showForm.set(true);
  }

  // CLOSE FORM
  closeForm(): void {
    this.showForm.set(false);
    this.saveError.set('');
    this.newEmployee = this.emptyForm();
    this.isEditing.set(false);
    this.editingEmployeeId.set(null);
  }

  // VIEW EMPLOYEE
  viewEmployee(employee: Employee): void {
    this.closeForm();
    this.viewingEmployee.set(employee);
  }

  closeView(): void {
    this.viewingEmployee.set(null);
  }

  // EDIT EMPLOYEE
  editEmployee(employee: Employee): void {
    this.closeView();

    this.isEditing.set(true);
    this.editingEmployeeId.set(employee.id);

    this.newEmployee = {
      employeeNumber: employee.employeeNumber,
      firstName: employee.firstName,
      lastName: employee.lastName,
      nationalId: employee.nationalId ?? null,
      email: employee.email ?? null,
      phoneNumber: employee.phoneNumber ?? null,
      dateOfBirth: employee.dateOfBirth?.substring(0, 10) ?? '',
      dateOfEmployment: employee.dateOfEmployment?.substring(0, 10) ?? '',
      jobTitle: employee.jobTitle ?? null,
      departmentId: employee.departmentId,
      isActive: employee.isActive
    };

    this.saveError.set('');
    this.showForm.set(true);
  }

  // SAVE EMPLOYEE
  saveEmployee(): void {
    this.saveError.set('');
    this.successMessage.set('');

    if (
      !this.newEmployee.employeeNumber.trim() ||
      !this.newEmployee.firstName.trim() ||
      !this.newEmployee.lastName.trim() ||
      !this.newEmployee.dateOfBirth ||
      !this.newEmployee.dateOfEmployment ||
      this.newEmployee.departmentId <= 0
    ) {
      this.saveError.set('Please fill in all required fields.');
      return;
    }

    this.saving.set(true);

    const request: EmployeeRequest = {
      ...this.newEmployee,
      employeeNumber: this.newEmployee.employeeNumber.trim(),
      firstName: this.newEmployee.firstName.trim(),
      lastName: this.newEmployee.lastName.trim(),
      nationalId: this.newEmployee.nationalId?.trim() || null,
      email: this.newEmployee.email?.trim() || null,
      phoneNumber: this.newEmployee.phoneNumber?.trim() || null,
      jobTitle: this.newEmployee.jobTitle?.trim() || null,
      departmentId: Number(this.newEmployee.departmentId)
    };

    const employeeId = this.editingEmployeeId();

    if (this.isEditing() && employeeId !== null) {
      this.http.put(`${this.apiUrl}/${employeeId}`, request).subscribe({
        next: () => {
          this.saving.set(false);
          this.closeForm();
          this.successMessage.set('Employee updated successfully.');
          this.loadEmployees();
        },
        error: (err) => {
          console.error('Error updating employee:', err);
          this.saveError.set(
            err.error?.message || 'Unable to update employee. Please check the information and try again.'
          );
          this.saving.set(false);
        }
      });
    } else {
      const { isActive, ...createRequest } = request;

      this.http.post(this.apiUrl, createRequest).subscribe({
        next: () => {
          this.saving.set(false);
          this.closeForm();
          this.successMessage.set('Employee added successfully.');
          this.page.set(1);
          this.loadEmployees();
        },
        error: (err) => {
          console.error('Error creating employee:', err);
          this.saveError.set(
            err.error?.message || 'Unable to add employee. Please check the information and try again.'
          );
          this.saving.set(false);
        }
      });
    }
  }

  // DELETE EMPLOYEE
  deleteEmployee(employee: Employee): void {
    const confirmed = confirm(
      `Are you sure you want to delete ${employee.firstName} ${employee.lastName}?`
    );

    if (!confirmed) {
      return;
    }

    this.http.delete(`${this.apiUrl}/${employee.id}`).subscribe({
      next: () => {
        this.successMessage.set('Employee deleted successfully.');

        if (this.employees().length === 1 && this.page() > 1) {
          this.page.update(value => value - 1);
        }

        this.closeView();
        this.loadEmployees();
      },
      error: (err) => {
        console.error('Error deleting employee:', err);
        this.error.set('Unable to delete employee. Please try again.');
      }
    });
  }
}
