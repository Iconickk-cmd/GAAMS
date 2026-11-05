
import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DepartmentService, Department } from '../services/department.service';

interface EmployeeRecord {
  id: number;
  departmentId: number | null;
  department?: Department | null;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: '../app.css'
})
export class DashboardComponent implements OnInit {
  protected readonly departmentCount = signal<number | null>(null);
  protected readonly activeDepartmentCount = signal<number | null>(null);
  protected readonly inactiveDepartmentCount = signal<number | null>(null);
  protected readonly departmentError = signal(false);

  protected readonly employeeCount = signal<number | null>(null);
  protected readonly employeeError = signal(false);

  protected readonly departmentEmployeeCounts = signal<
    {
      departmentId: number;
      departmentName: string;
      departmentCode: string;
      employeeCount: number;
    }[]
  >([]);

  protected readonly refreshing = signal(false);
  protected readonly lastUpdated = signal<Date | null>(null);

  constructor(private readonly departmentService: DepartmentService) {}

  ngOnInit(): void {
    this.refreshStatistics();
  }

  protected refreshStatistics(): void {
    if (this.refreshing()) {
      return;
    }

    this.refreshing.set(true);
    this.departmentError.set(false);
    this.employeeError.set(false);

    let pendingRequests = 2;
    let loadedDepartments: Department[] | null = null;
    let loadedEmployees: EmployeeRecord[] | null = null;

    const requestFinished = (): void => {
      pendingRequests--;

      if (pendingRequests === 0) {
        this.refreshing.set(false);
        this.lastUpdated.set(new Date());
      }
    };

    const updateDepartmentEmployeeCounts = (): void => {
      if (loadedDepartments === null || loadedEmployees === null) {
        return;
      }

      this.departmentEmployeeCounts.set(
        loadedDepartments.map(department => ({
          departmentId: department.id,
          departmentName: department.name,
          departmentCode: department.code,
          employeeCount: loadedEmployees!.filter(
            employee => employee.departmentId === department.id
          ).length
        }))
      );
    };

    this.departmentService.getDepartments().subscribe({
      next: departments => {
        loadedDepartments = departments;

        this.departmentCount.set(departments.length);
        this.activeDepartmentCount.set(
          departments.filter(department => department.isActive).length
        );
        this.inactiveDepartmentCount.set(
          departments.filter(department => !department.isActive).length
        );

        updateDepartmentEmployeeCounts();
        requestFinished();
      },
      error: error => {
        console.error('Failed to load departments:', error);
        this.departmentError.set(true);
        this.departmentEmployeeCounts.set([]);
        requestFinished();
      }
    });

    this.departmentService.getEmployees().subscribe({
      next: response => {
        loadedEmployees = response.employees as EmployeeRecord[];
        this.employeeCount.set(response.totalCount);
        this.employeeError.set(false);

        updateDepartmentEmployeeCounts();
        requestFinished();
      },
      error: error => {
        console.error('Failed to load employees:', error);
        this.employeeError.set(true);
        this.departmentEmployeeCounts.set([]);
        requestFinished();
      }
    });
  }
}
