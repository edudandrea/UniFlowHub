import { DatePipe, isPlatformBrowser } from '@angular/common';
import { Component, HostListener, OnInit, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/auth.service';
import { PecasBiData, PecaVendaMensal, PecasBiService } from '../../core/pecas-bi.service';
import { ProfileFlowService } from '../../core/profile-flow.service';
import { ThemeService } from '../../core/theme.service';

@Component({
  selector: 'app-pecas-bi',
  imports: [DatePipe],
  templateUrl: './pecas-bi.html',
  styleUrl: './pecas-bi.scss',
})
export class PecasBiPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly service = inject(PecasBiService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);
  private readonly spinner = inject(NgxSpinnerService);
  private readonly profileFlow = inject(ProfileFlowService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly theme = inject(ThemeService);
  readonly user = computed(() => this.auth.user());
  readonly profileMenuOpen = signal(false);
  readonly loading = signal(false);
  readonly data = signal<PecasBiData | null>(null);
  readonly periodo = signal('Semestre atual');
  readonly canal = signal('Todos');
  readonly filial = signal('Todas');

  readonly canais = computed(() => ['Todos', ...(this.data()?.canais.map((item) => item.nome) ?? [])]);
  readonly filiais = ['Todas', 'Matriz', 'Filial Norte', 'Filial Sul'];
  readonly vendas = computed(() => this.data()?.vendasMensais ?? []);
  readonly categorias = computed(() => this.data()?.categorias ?? []);
  readonly pecas = computed(() => this.data()?.pecas ?? []);
  readonly vendedores = computed(() => this.data()?.vendedores ?? []);
  readonly canaisData = computed(() => this.data()?.canais ?? []);
  readonly faturamentoTotal = computed(() => this.vendas().reduce((total, item) => total + item.faturamento, 0));
  readonly margemTotal = computed(() => this.vendas().reduce((total, item) => total + item.margem, 0));
  readonly quantidadeTotal = computed(() => this.vendas().reduce((total, item) => total + item.quantidade, 0));
  readonly ticketMedio = computed(() => this.quantidadeTotal() ? this.faturamentoTotal() / this.quantidadeTotal() : 0);
  readonly margemPercentual = computed(() => this.faturamentoTotal() ? (this.margemTotal() / this.faturamentoTotal()) * 100 : 0);
  readonly crescimento = computed(() => {
    const vendas = this.vendas();
    const atual = vendas.at(-1)?.faturamento ?? 0;
    const anterior = vendas.at(-2)?.faturamento ?? 0;
    return anterior ? ((atual - anterior) / anterior) * 100 : 0;
  });
  readonly maxMensal = computed(() => Math.max(...this.vendas().map((item) => item.faturamento), 1));
  readonly maxCategoria = computed(() => Math.max(...this.categorias().map((item) => item.faturamento), 1));
  readonly maxCanal = computed(() => Math.max(...this.canaisData().map((item) => item.faturamento), 1));
  readonly curvaA = computed(() => this.pecas().filter((_, index) => index < 2));
  readonly curvaB = computed(() => this.pecas().filter((_, index) => index >= 2 && index < 4));
  readonly curvaC = computed(() => this.pecas().filter((_, index) => index >= 4));
  readonly forecast = computed(() => {
    const vendas = this.vendas();
    const media = vendas.slice(-3).reduce((total, item) => total + item.faturamento, 0) / Math.max(vendas.slice(-3).length, 1);
    return media * 1.07;
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
    this.service.load().subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
        void this.spinner.hide();
      },
      error: () => {
        this.loading.set(false);
        void this.spinner.hide();
        this.toastr.error('Nao foi possivel carregar o B.I de venda de pecas.', 'Erro');
      },
    });
  }

  barHeight(item: PecaVendaMensal): number {
    return this.barHeightValue(item.faturamento);
  }

  barHeightValue(value: number): number {
    return Math.max(8, (value / this.maxMensal()) * 100);
  }

  percent(value: number, max: number): number {
    return Math.max(2, (value / max) * 100);
  }

  formatMoney(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(value);
  }

  formatNumber(value: number): string {
    return new Intl.NumberFormat('pt-BR').format(Math.round(value));
  }

  formatPercent(value: number): string {
    return `${value.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`;
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
}
