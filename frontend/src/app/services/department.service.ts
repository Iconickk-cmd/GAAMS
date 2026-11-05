
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Department {
  id: number;
  name: string;
  code: string;
  description: string;
  isActive: boolean;
  createdAt: string;
}

export interface EmployeeResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  employees: unknown[];
}

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'http://localhost:5081/api/departments';
  private readonly employeesApiUrl = 'http://localhost:5081/api/employees';

  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(this.apiUrl);
  }

  getEmployees(): Observable<EmployeeResponse> {
    return this.http.get<EmployeeResponse>(this.employeesApiUrl);
  }
}
