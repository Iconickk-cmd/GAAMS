
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import {
  Department,
  DepartmentService,
  CreateDepartmentRequest,
  UpdateDepartmentRequest
} from './department.service';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './departments.html',
  styleUrl: './departments.css'
})
export class DepartmentsComponent implements OnInit {
  departments = signal<Department[]>([]);
  loading = signal(false);
  saving = signal(false);
  error = signal('');
  successMessage = signal('');
  searchTerm = signal('');
  selectedStatus = signal('all');
  showForm = signal(false);
  isEditing = signal(false);
  editingDepartmentId = signal<number | null>(null);
  viewingDepartment = signal<Department | null>(null);

  newDepartment: CreateDepartmentRequest = this.emptyForm();

  constructor(private departmentService: DepartmentService) {}

  ngOnInit(): void {
    this.loadDepartments();
  }

  emptyForm(): CreateDepartmentRequest {
    return { name: '', code: '', description: '' };
  }

  loadDepartments(): void {
    this.loading.set(true);
    this.error.set('');

    this.departmentService.getDepartments().subscribe({
      next: data => {
        this.departments.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(
          'Could not load departments. Check that the backend API is running.'
        );
        this.loading.set(false);
      }
    });
  }

  filteredDepartments(): Department[] {
    const term = this.searchTerm().trim().toLowerCase();
    const status = this.selectedStatus();

    return this.departments().filter(department => {
      const matchesSearch =
        department.name.toLowerCase().includes(term) ||
        department.code.toLowerCase().includes(term) ||
        (department.description ?? '').toLowerCase().includes(term);

      const matchesStatus =
        status === 'all' ||
        (status === 'active' && department.isActive) ||
        (status === 'inactive' && !department.isActive);

      return matchesSearch && matchesStatus;
    });
  }

  openAddForm(): void {
    this.newDepartment = this.emptyForm();
    this.isEditing.set(false);
    this.editingDepartmentId.set(null);
    this.showForm.set(true);
    this.error.set('');
    this.successMessage.set('');
  }

  openEditForm(department: Department): void {
    this.newDepartment = {
      name: department.name,
      code: department.code,
      description: department.description ?? ''
    };

    this.isEditing.set(true);
    this.editingDepartmentId.set(department.id);
    this.showForm.set(true);
    this.error.set('');
    this.successMessage.set('');
  }

  closeForm(): void {
    this.showForm.set(false);
    this.newDepartment = this.emptyForm();
    this.editingDepartmentId.set(null);
  }

  viewDepartment(department: Department): void {
    this.viewingDepartment.set(department);
  }

  closeView(): void {
    this.viewingDepartment.set(null);
  }

  saveDepartment(): void {
    const name = this.newDepartment.name.trim();
    const code = this.newDepartment.code.trim();

    if (!name || !code) {
      this.error.set('Department name and code are required.');
      return;
    }

    this.saving.set(true);
    this.error.set('');
    this.successMessage.set('');

    if (this.isEditing()) {
      const id = this.editingDepartmentId();

      if (id === null) {
        this.saving.set(false);
        this.error.set('Could not identify the department to update.');
        return;
      }

      const request: UpdateDepartmentRequest = {
        name,
        code,
        description: this.newDepartment.description?.trim() || null,
        isActive:
          this.departments().find(d => d.id === id)?.isActive ?? true
      };

      this.departmentService.updateDepartment(id, request).subscribe({
        next: () => {
          this.saving.set(false);
          this.showForm.set(false);
          this.successMessage.set('Department updated successfully.');
          this.loadDepartments();
        },
        error: err => {
          this.saving.set(false);
          this.error.set(
            err.error?.message ?? 'Failed to update department.'
          );
        }
      });
    } else {
      const request: CreateDepartmentRequest = {
        name,
        code,
        description: this.newDepartment.description?.trim() || null
      };

      this.departmentService.createDepartment(request).subscribe({
        next: () => {
          this.saving.set(false);
          this.showForm.set(false);
          this.successMessage.set('Department created successfully.');
          this.loadDepartments();
        },
        error: err => {
          this.saving.set(false);
          this.error.set(
            err.error?.message ?? 'Failed to create department.'
          );
        }
      });
    }
  }

  deleteDepartment(department: Department): void {
    const confirmed = window.confirm(
      `Are you sure you want to delete "${department.name}"?`
    );

    if (!confirmed) return;

    this.error.set('');
    this.successMessage.set('');

    this.departmentService.deleteDepartment(department.id).subscribe({
      next: () => {
        this.successMessage.set('Department deleted successfully.');
        this.loadDepartments();
      },
      error: err => {
        this.error.set(
          err.error?.message ?? 'Failed to delete department.'
        );
      }
    });
  }

  toggleStatus(department: Department): void {
    const request: UpdateDepartmentRequest = {
      name: department.name,
      code: department.code,
      description: department.description ?? null,
      isActive: !department.isActive
    };

    this.departmentService.updateDepartment(department.id, request).subscribe({
      next: () => {
        this.successMessage.set('Department status updated successfully.');
        this.loadDepartments();
      },
      error: err => {
        this.error.set(
          err.error?.message ?? 'Failed to update department status.'
        );
      }
    });
  }
}
