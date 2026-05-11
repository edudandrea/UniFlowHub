import { DatePipe, isPlatformBrowser } from '@angular/common';
import { Component, HostListener, OnInit, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/auth.service';
import { Role, Unidade, User, UserCreatePayload, UserUpdatePayload } from '../../core/models';
import { ThemeService } from '../../core/theme.service';
import { ProfileFlowService } from '../../core/profile-flow.service';
import { UnidadesService } from '../../core/unidades.service';

type UserModalMode = 'create' | 'edit';
type UnidadeModalMode = 'create' | 'edit';

@Component({
  selector: 'app-usuarios',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './usuarios.html',
  styleUrl: './usuarios.scss',
})
export class UsuariosPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);
  private readonly spinner = inject(NgxSpinnerService);
  private readonly profileFlow = inject(ProfileFlowService);
  private readonly unidadesService = inject(UnidadesService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly theme = inject(ThemeService);
  readonly user = computed(() => this.auth.user());
  readonly users = signal<User[]>([]);
  readonly unidades = signal<Unidade[]>([]);
  readonly selectedUnidade = signal<Unidade | null>(null);
  readonly selected = signal<User | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly modalOpen = signal(false);
  readonly modalMode = signal<UserModalMode>('create');
  readonly unidadeModalOpen = signal(false);
  readonly unidadeModalMode = signal<UnidadeModalMode>('create');
  readonly search = signal('');
  readonly unidadeSearch = signal('');
  readonly profileMenuOpen = signal(false);

  readonly roles: Role[] = ['Admin', 'RH', 'TI', 'Diretoria', 'Compras', 'Gestor', 'Usuario'];
  readonly departamentos = ['Administrativo', 'RH', 'TI', 'Financeiro', 'Compras', 'Operacional', 'Comercial'];

  readonly totalAdmins = computed(() => this.users().filter((item) => item.role === 'Admin').length);
  readonly totalAtivos = computed(() => this.users().filter((item) => item.ativo).length);
  readonly filtered = computed(() => {
    const term = this.normalize(this.search());
    if (!term) {
      return this.users();
    }

    return this.users().filter((item) =>
      [item.nome, item.cpf, item.email, item.role, item.departamento, item.cargo, item.ativo ? 'ativo' : 'inativo']
        .some((value) => this.normalize(value).includes(term)),
    );
  });
  readonly filteredUnidades = computed(() => {
    const term = this.normalize(this.unidadeSearch());
    return this.unidades().filter((item) => !term || [item.nome, item.cnpj, item.endereco].some((value) => this.normalize(value).includes(term)));
  });

  readonly form = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    cpf: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    role: ['Usuario', Validators.required],
    departamento: ['', Validators.required],
    cargo: ['', Validators.required],
    unidadeId: [0],
    ativo: [true],
    dataNascimento: ['1990-01-01', Validators.required],
    senha: [''],
  });

  readonly unidadeForm = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    cnpj: ['', Validators.required],
    endereco: ['', Validators.required],
  });

  ngOnInit(): void {
    if (!this.isBrowser) {
      return;
    }

    this.load();
    this.loadUnidades();
  }

  load(): void {
    this.loading.set(true);
    void this.spinner.show();
    this.auth.listUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
        void this.spinner.hide();
      },
      error: (error) => {
        this.loading.set(false);
        void this.spinner.hide();
        this.toastr.error(this.getErrorMessage('Nao foi possivel carregar os usuarios.', error), 'Erro');
      },
    });
  }

  loadUnidades(): void {
    this.unidadesService.list().subscribe({
      next: (unidades) => this.unidades.set(unidades),
      error: (error) => this.toastr.error(this.getErrorMessage('Nao foi possivel carregar as unidades.', error), 'Erro'),
    });
  }

  openCreate(): void {
    this.modalMode.set('create');
    this.selected.set(null);
    this.form.reset({
      nome: '',
      cpf: '',
      email: '',
      role: 'Usuario',
      departamento: '',
      cargo: '',
      unidadeId: 0,
      ativo: true,
      dataNascimento: '1990-01-01',
      senha: '',
    });
    this.form.controls.senha.setValidators([Validators.required, Validators.minLength(6)]);
    this.form.controls.senha.updateValueAndValidity();
    this.modalOpen.set(true);
  }

  openEdit(item: User): void {
    this.modalMode.set('edit');
    this.selected.set(item);
    this.form.reset({
      nome: item.nome,
      cpf: item.cpf,
      email: item.email,
      role: item.role,
      departamento: item.departamento,
      cargo: item.cargo,
      unidadeId: item.unidadeId ?? 0,
      ativo: item.ativo,
      dataNascimento: this.toDateInputValue(item.dataNascimento),
      senha: '',
    });
    this.form.controls.senha.clearValidators();
    this.form.controls.senha.updateValueAndValidity();
    this.modalOpen.set(true);
  }

  closeModal(): void {
    if (this.saving()) {
      return;
    }

    this.modalOpen.set(false);
  }

  openCreateUnidade(): void {
    this.unidadeModalMode.set('create');
    this.selectedUnidade.set(null);
    this.unidadeForm.reset({ nome: '', cnpj: '', endereco: '' });
    this.unidadeModalOpen.set(true);
  }

  openEditUnidade(item: Unidade): void {
    this.unidadeModalMode.set('edit');
    this.selectedUnidade.set(item);
    this.unidadeForm.reset({
      nome: item.nome,
      cnpj: item.cnpj,
      endereco: item.endereco,
    });
    this.unidadeModalOpen.set(true);
  }

  closeUnidadeModal(): void {
    if (!this.saving()) {
      this.unidadeModalOpen.set(false);
    }
  }

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      if (this.form.invalid) {
        this.toastr.warning('Confira os campos obrigatorios antes de salvar.', 'Atencao');
      }
      return;
    }

    this.saving.set(true);
    const raw = this.form.getRawValue();
    const unidadeId = raw.unidadeId > 0 ? raw.unidadeId : null;

    if (this.modalMode() === 'create') {
      const payload: UserCreatePayload = {
        nome: raw.nome,
        cpf: raw.cpf,
        email: raw.email,
        role: raw.role as Role,
        departamento: raw.departamento,
        cargo: raw.cargo,
        ativo: raw.ativo,
        unidadeId,
        dataNascimento: raw.dataNascimento,
        senha: raw.senha,
      };

      this.auth.createUser(payload).subscribe({
        next: (created) => {
          this.users.set([created, ...this.users()]);
          this.saving.set(false);
          this.modalOpen.set(false);
          this.toastr.success('Usuario criado com sucesso.', 'Usuarios');
        },
        error: (error) => {
          this.saving.set(false);
          this.toastr.error(this.getErrorMessage('Nao foi possivel criar o usuario.', error), 'Erro');
        },
      });
      return;
    }

    const selected = this.selected();
    if (!selected) {
      this.saving.set(false);
      return;
    }

    const payload: UserUpdatePayload = {
      nome: raw.nome,
      cpf: raw.cpf,
      email: raw.email,
      role: raw.role as Role,
      departamento: raw.departamento,
      cargo: raw.cargo,
      ativo: raw.ativo,
      unidadeId,
      dataNascimento: raw.dataNascimento,
      senha: raw.senha,
    };

    this.auth.updateUser(selected.id, payload).subscribe({
      next: (updated) => {
        this.users.set(this.users().map((item) => item.id === updated.id ? updated : item));
        this.selected.set(updated);
        this.saving.set(false);
        this.modalOpen.set(false);
        this.toastr.success('Usuario atualizado com sucesso.', 'Usuarios');
      },
      error: (error) => {
        this.saving.set(false);
        this.toastr.error(this.getErrorMessage('Nao foi possivel atualizar o usuario.', error), 'Erro');
      },
    });
  }

  submitUnidade(): void {
    if (this.unidadeForm.invalid || this.saving()) {
      this.unidadeForm.markAllAsTouched();
      this.toastr.warning('Preencha os dados da unidade.', 'Unidades');
      return;
    }

    this.saving.set(true);
    const payload = this.unidadeForm.getRawValue();
    const selected = this.selectedUnidade();
    const request = selected
      ? this.unidadesService.update(selected.id, payload)
      : this.unidadesService.create(payload);

    request.subscribe({
      next: (saved) => {
        this.unidades.set(selected ? this.unidades().map((item) => item.id === saved.id ? saved : item) : [saved, ...this.unidades()]);
        this.saving.set(false);
        this.unidadeModalOpen.set(false);
        this.toastr.success('Unidade salva com sucesso.', 'Unidades');
      },
      error: (error) => {
        this.saving.set(false);
        this.toastr.error(this.getErrorMessage('Nao foi possivel salvar a unidade.', error), 'Erro');
      },
    });
  }

  goHome(): void {
    void this.router.navigate(['/hub']);
  }

  logout(): void {
    this.auth.logout();
  }

  editProfile(): void {
    this.profileMenuOpen.set(false);
    this.profileFlow.editProfile();
  }

  changePassword(): void {
    this.profileMenuOpen.set(false);
    this.profileFlow.changePassword();
  }

  @HostListener('document:click', ['$event'])
  closeProfileMenuOnDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target?.closest('.profile-area')) {
      this.profileMenuOpen.set(false);
    }
  }

  private toDateInputValue(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '1990-01-01';
    }

    return date.toISOString().slice(0, 10);
  }

  private normalize(value: string | number | null | undefined): string {
    return String(value ?? '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  private getErrorMessage(fallback: string, error?: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (typeof error.error === 'string' && error.error.trim()) {
        return error.error;
      }

      if (error.error?.title) {
        return error.error.title;
      }
    }

    return fallback;
  }
}
