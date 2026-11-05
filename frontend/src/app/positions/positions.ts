
import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Department {
  id: number;
  name: string;
  code: string;
}

interface Position {
  id: number;
  positionCode: string;
  title: string;
  description?: string;
  departmentId: number;
  department?: Department;
  employmentType?: string;
  employmentGrade?: string;
  approvedHeadcount: number;
  isActive: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-positions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './positions.html',
  styleUrl: './positions.css'
})
export class PositionsComponent implements OnInit {
  private readonly apiUrl = 'http://localhost:5081/api';

  positions = signal<Position[]>([]);
  departments = signal<Department[]>([]);

  loading = signal(false);
  saving = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  searchTerm = '';
  showForm = false;

  editingId: number | null = null;

  form: {
    positionCode: string;
    title: string;
    description: string;
    departmentId: number | null;
    employmentType: string;
    employmentGrade: string;
    approvedHeadcount: number;
    isActive: boolean;
  } = this.emptyForm();

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.loadDepartments();
    this.loadPositions();
  }

  private emptyForm() {
    return {
      positionCode: '',
      title: '',
      description: '',
      departmentId: null as number | null,
      employmentType: 'Permanent',
      employmentGrade: '',
      approvedHeadcount: 1,
      isActive: true
    };
  }

  loadDepartments(): void {
    this.http.get<Department[]>(`${this.apiUrl}/Departments`)
      .subscribe({
        next: data => this.departments.set(data),
        error: () => this.errorMessage.set(
          'Unable to load departments. Please check the API.'
        )
      });
  }

  loadPositions(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.http.get<Position[]>(`${this.apiUrl}/Positions`)
      .subscribe({
        next: data => {
          this.positions.set(data);
          this.loading.set(false);
        },
        error: () => {
          this.errorMessage.set('Unable to load positions.');
          this.loading.set(false);
        }
      });
  }

  get filteredPositions(): Position[] {
    const term = this.searchTerm.trim().toLowerCase();

    if (!term) {
      return this.positions();
    }

    return this.positions().filter(position =>
      position.title.toLowerCase().includes(term) ||
      position.positionCode.toLowerCase().includes(term) ||
      (position.department?.name ?? '').toLowerCase().includes(term)
    );
  }

  get totalPositions(): number {
    return this.positions().length;
  }

  get activePositions(): number {
    return this.positions().filter(p => p.isActive).length;
  }

  get totalApprovedPosts(): number {
    return this.positions()
      .filter(p => p.isActive)
      .reduce((total, p) => total + p.approvedHeadcount, 0);
  }

  get occupiedPosts(): number {
    return this.positions()
      .filter(p => p.isActive)
      .reduce((total, p) => total + (p as any).currentOccupancy, 0);
  }

  get vacantPosts(): number {
    return this.totalApprovedPosts - this.occupiedPosts;
  }

  openCreateForm(): void {
    this.editingId = null;
    this.form = this.emptyForm();
    this.errorMessage.set('');
    this.successMessage.set('');
    this.showForm = true;
  }

  openEditForm(position: Position): void {
    this.editingId = position.id;

    this.form = {
      positionCode: position.positionCode,
      title: position.title,
      description: position.description ?? '',
      departmentId: position.departmentId,
      employmentType: position.employmentType ?? 'Permanent',
      employmentGrade: position.employmentGrade ?? '',
      approvedHeadcount: position.approvedHeadcount,
      isActive: position.isActive
    };

    this.errorMessage.set('');
    this.successMessage.set('');
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.form = this.emptyForm();
  }

  savePosition(): void {
    if (
      !this.form.positionCode.trim() ||
      !this.form.title.trim() ||
      !this.form.departmentId ||
      this.form.approvedHeadcount < 1
    ) {
      this.errorMessage.set(
        'Enter a position code, title, department and valid headcount.'
      );
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const payload = {
      ...this.form,
      positionCode: this.form.positionCode.trim(),
      title: this.form.title.trim(),
      description: this.form.description.trim() || null
    };

    const request = this.editingId
      ? this.http.put(
        `${this.apiUrl}/Positions/${this.editingId}`,
        payload
      )
      : this.http.post(
        `${this.apiUrl}/Positions`,
        payload
      );

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeForm();
        this.successMessage.set(
          this.editingId
            ? 'Position updated successfully.'
            : 'Position created successfully.'
        );
        this.loadPositions();
      },
      error: error => {
        this.saving.set(false);
        this.errorMessage.set(
          error.error?.message ?? 'Unable to save position.'
        );
      }
    });
  }

  deactivatePosition(position: Position): void {
    if (!confirm(`Deactivate ${position.title}?`)) {
      return;
    }

    this.errorMessage.set('');
    this.successMessage.set('');

    this.http.patch(
      `${this.apiUrl}/Positions/${position.id}/deactivate`,
      {}
    ).subscribe({
      next: () => {
        this.successMessage.set('Position deactivated successfully.');
        this.loadPositions();
      },
      error: error => {
        this.errorMessage.set(
          error.error?.message ?? 'Unable to deactivate position.'
        );
      }
    });
  }
}
