import { DatePipe, isPlatformBrowser } from '@angular/common';
import { Component, ElementRef, HostListener, OnInit, PLATFORM_ID, computed, inject, signal, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/auth.service';
import { EquipamentosTIService } from '../../core/equipamentos-ti.service';
import { EquipamentoTI, EquipamentoTIPayload } from '../../core/models';
import { ThemeService } from '../../core/theme.service';
import { ProfileFlowService } from '../../core/profile-flow.service';

@Component({
  selector: 'app-equipamentos-ti',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './equipamentos-ti.html',
  styleUrl: './equipamentos-ti.scss',
})
export class EquipamentosTIPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly service = inject(EquipamentosTIService);
  private readonly toastr = inject(ToastrService);
  private readonly spinner = inject(NgxSpinnerService);
  private readonly profileFlow = inject(ProfileFlowService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly theme = inject(ThemeService);
  readonly user = computed(() => this.auth.user());
  readonly itens = signal<EquipamentoTI[]>([]);
  readonly selected = signal<EquipamentoTI | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly modalOpen = signal(false);
  readonly search = signal('');
  readonly selectedFileName = signal('');
  readonly profileMenuOpen = signal(false);
  readonly emTransito = computed(() => this.itens().filter((item) => item.status === 'Enviado' || item.status === 'Em transito').length);
  readonly recebidos = computed(() => this.itens().filter((item) => item.status === 'Recebido').length);
  readonly atrasados = computed(() => this.itens().filter((item) => {
    if (!item.dataPrevistaRetorno || item.status === 'Recebido') {
      return false;
    }
    return new Date(item.dataPrevistaRetorno).getTime() < Date.now();
  }).length);
  readonly filtered = computed(() => {
    const term = this.normalize(this.search());
    return this.itens().filter((item) => !term || [
      item.tipo,
      item.patrimonio,
      item.modelo,
      item.serial,
      item.responsavel,
      item.status,
      item.destino,
    ].some((value) => this.normalize(value).includes(term)));
  });

  private selectedFile: File | null = null;
  @ViewChild('documentoInput') private documentoInput?: ElementRef<HTMLInputElement>;

  readonly form = this.fb.nonNullable.group({
    tipo: ['Notebook', Validators.required],
    patrimonio: ['', Validators.required],
    modelo: [''],
    serial: [''],
    status: ['Enviado', Validators.required],
    origem: ['TI', Validators.required],
    destino: ['', Validators.required],
    responsavel: ['', Validators.required],
    dataPrevistaRetorno: [''],
    observacoes: [''],
  });

  ngOnInit(): void {
    if (!this.isBrowser) {
      return;
    }
    this.load();
  }

  load(): void {
    this.loading.set(true);
    void this.spinner.show();
    this.service.list().subscribe({
      next: (items) => {
        this.itens.set(items);
        this.selected.set(null);
        this.loading.set(false);
        void this.spinner.hide();
      },
      error: () => {
        this.loading.set(false);
        void this.spinner.hide();
        this.toastr.error('Não foi possível carregar os equipamentos.', 'TI');
      },
    });
  }

  select(item: EquipamentoTI): void {
    this.selected.set(item);
    this.patchForm(item);
    this.modalOpen.set(true);
  }

  openNewMovement(): void {
    this.selected.set(null);
    this.form.reset({
      tipo: 'Notebook',
      patrimonio: '',
      modelo: '',
      serial: '',
      status: 'Enviado',
      origem: 'TI',
      destino: '',
      responsavel: '',
      dataPrevistaRetorno: '',
      observacoes: '',
    });
    this.selectedFile = null;
    this.selectedFileName.set('');
    if (this.documentoInput?.nativeElement) {
      this.documentoInput.nativeElement.value = '';
    }
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.selected.set(null);
    this.selectedFile = null;
    this.selectedFileName.set('');
    if (this.documentoInput?.nativeElement) {
      this.documentoInput.nativeElement.value = '';
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile = file;
    this.selectedFileName.set(file?.name ?? '');
  }

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      this.toastr.warning('Preencha os campos obrigatórios.', 'Atenção');
      return;
    }

    this.saving.set(true);
    const payload = this.form.getRawValue() as EquipamentoTIPayload;
    const selected = this.selected();
    const request = selected
      ? this.service.update(selected.id, payload)
      : this.service.create(payload, this.selectedFile);

    request.subscribe({
      next: (saved) => {
        this.itens.set(selected ? this.itens().map((item) => item.id === saved.id ? saved : item) : [saved, ...this.itens()]);
        this.selected.set(null);
        this.modalOpen.set(false);
        this.saving.set(false);
        this.toastr.success('Controle de equipamento salvo.', 'TI');
      },
      error: () => {
        this.saving.set(false);
        this.toastr.error('Não foi possível salvar o equipamento.', 'Erro');
      },
    });
  }

  downloadDocument(item = this.selected()): void {
    if (!item?.documentoUrl) {
      this.toastr.info('Este registro não possui documento.', 'Documento');
      return;
    }

    this.service.downloadDocument(item.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = item.documentoUrl.split('/').pop() || 'documento';
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.toastr.error('Não foi possível baixar o documento.', 'Erro'),
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

  private patchForm(item: EquipamentoTI): void {
    this.form.reset({
      tipo: item.tipo,
      patrimonio: item.patrimonio,
      modelo: item.modelo,
      serial: item.serial,
      status: item.status,
      origem: item.origem,
      destino: item.destino,
      responsavel: item.responsavel,
      dataPrevistaRetorno: item.dataPrevistaRetorno ? item.dataPrevistaRetorno.slice(0, 10) : '',
      observacoes: item.observacoes,
    });
  }

  private normalize(value: string | number | null | undefined): string {
    return String(value ?? '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }
}
