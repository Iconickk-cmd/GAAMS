
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Department {
  id: number;
  name: string;
  code: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateDepartmentRequest {
  name: string;
  code: string;
  description?: string | null;
}

export interface UpdateDepartmentRequest {
  name: string;
  code: string;
  description?: string | null;
  isActive: boolean;
}

export interface Employee {
  id: number;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  nationalId?: string;
  email?: string | null;
  phoneNumber?: string | null;
  dateOfBirth?: string;
  dateOfEmployment?: string;
  jobTitle: string;
  departmentId: number | null;
  isActive: boolean;
  createdAt: string;
  department: Department | null;
}

export interface EmployeeListResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  employees: Employee[];
}

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private readonly apiUrl = 'http://localhost:5081/api/departments';
  private readonly employeeApiUrl = 'http://localhost:5081/api/employees';

  constructor(private http: HttpClient) {}

  // Get all departments
  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(this.apiUrl);
  }

  // Get one department
  getDepartment(id: number): Observable<Department> {
    return this.http.get<Department>(`${this.apiUrl}/${id}`);
  }

  // Create a department
  createDepartment(
    request: CreateDepartmentRequest
  ): Observable<Department> {
    return this.http.post<Department>(this.apiUrl, request);
  }

  // Update a department
  updateDepartment(
    id: number,
    request: UpdateDepartmentRequest
  ): Observable<Department> {
    return this.http.put<Department>(`${this.apiUrl}/${id}`, request);
  }

  // Delete a department
  deleteDepartment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // Get employees, including their department information
  getEmployees(): Observable<EmployeeListResponse> {
    return this.http.get<EmployeeListResponse>(this.employeeApiUrl);
  }
}
