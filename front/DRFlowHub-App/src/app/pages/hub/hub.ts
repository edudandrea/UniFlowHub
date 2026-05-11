import { isPlatformBrowser } from '@angular/common';
import { Component, HostListener, OnInit, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { NgxSpinnerService } from 'ngx-spinner';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/auth.service';
import { ChamadosTIService } from '../../core/chamados-ti.service';
import { ComprasService } from '../../core/compras.service';
import { ChamadoTI, SolicitacaoCompra, SolicitacaoRH, User } from '../../core/models';
import { ProfileFlowService } from '../../core/profile-flow.service';
import { SolicitacoesService } from '../../core/solicitacoes.service';
import { ThemeService } from '../../core/theme.service';

type DashboardArea = 'admin' | 'rh' | 'ti' | 'compras' | 'financeiro' | 'departamento';

interface MetricCard {
  label: string;
  value: number;
  detail: string;
  tone: 'neutral' | 'attention' | 'danger' | 'success';
}

interface AlertItem {
  id: string;
  sector: string;
  title: string;
  detail: string;
  priority: string;
  status: string;
  route: string;
  date: string;
}

interface DashboardAction {
  label: string;
  route: string;
  description: string;
  enabled: boolean;
}

interface BirthdayItem {
  id: number;
  nome: string;
  departamento: string;
  cargo: string;
  day: number;
  isToday: boolean;
}

@Component({
  selector: 'app-hub',
  imports: [],
  templateUrl: './hub.html',
  styleUrl: './hub.scss',
})
export class HubPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);
  private readonly spinner = inject(NgxSpinnerService);
  private readonly profileFlow = inject(ProfileFlowService);
  private readonly rhService = inject(SolicitacoesService);
  private readonly tiService = inject(ChamadosTIService);
  private readonly comprasService = inject(ComprasService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly theme = inject(ThemeService);
  readonly user = computed(() => this.auth.user());
  readonly profileMenuOpen = signal(false);
  readonly loading = signal(false);
  readonly rhItems = signal<SolicitacaoRH[]>([]);
  readonly tiItems = signal<ChamadoTI[]>([]);
  readonly comprasItems = signal<SolicitacaoCompra[]>([]);
  readonly users = signal<User[]>([]);

  readonly currentArea = computed<DashboardArea>(() => {
    const user = this.user();
    const role = user?.role;
    const department = this.normalize(user?.departamento);

    if (role === 'Admin') {
      return 'admin';
    }

    if (role === 'RH' || department.includes('rh') || department.includes('recursos humanos')) {
      return 'rh';
    }

    if (role === 'TI' || department.includes('ti') || department.includes('tecnologia')) {
      return 'ti';
    }

    if (role === 'Compras' || role === 'Diretoria' || department.includes('compra')) {
      return 'compras';
    }

    if (department.includes('financeiro')) {
      return 'financeiro';
    }

    return 'departamento';
  });

  readonly dashboardTitle = computed(() => {
    const area = this.currentArea();
    const user = this.user();

    if (area === 'admin') {
      return 'Dashboard da administração';
    }

    if (area === 'rh') {
      return this.auth.hasAnyRole(['RH']) ? 'Dashboard da administração de RH' : 'Dashboard de Recursos Humanos';
    }

    if (area === 'ti') {
      return this.auth.hasAnyRole(['TI']) ? 'Dashboard da administração de TI' : 'Dashboard de TI';
    }

    if (area === 'compras') {
      return this.auth.hasAnyRole(['Compras', 'Diretoria']) ? 'Dashboard da administração de Compras' : 'Dashboard de Compras';
    }

    return `Dashboard de ${user?.departamento || 'departamento'}`;
  });

  readonly dashboardSubtitle = computed(() => {
    const user = this.user();
    if (this.currentArea() === 'admin') {
      return 'Acompanhe os alertas dos setores ativos e entre nas administrações quando precisar agir.';
    }

    return `${user?.nome || 'Usuario'}, estes sao os alertas priorizados para seu perfil e setor.`;
  });

  readonly isBirthdayToday = computed(() => {
    const birthDate = this.user()?.dataNascimento;
    return !!birthDate && this.matchesToday(birthDate);
  });

  readonly birthdayGreeting = computed(() => {
    const firstName = (this.user()?.nome || 'voce').trim().split(/\s+/)[0];
    return `Feliz aniversario, ${firstName}! Que seu dia seja leve, especial e cheio de boas noticias.`;
  });

  readonly showBirthdays = computed(() => this.currentArea() === 'rh' && this.auth.hasAnyRole(['Admin', 'RH']));

  readonly monthBirthdays = computed<BirthdayItem[]>(() => {
    const today = new Date();
    const currentMonth = today.getMonth();

    return this.users()
      .map((user) => {
        const date = this.dateParts(user.dataNascimento);
        if (!date || date.month !== currentMonth) {
          return null;
        }

        return {
          id: user.id,
          nome: user.nome,
          departamento: user.departamento,
          cargo: user.cargo,
          day: date.day,
          isToday: date.day === today.getDate(),
        };
      })
      .filter((item): item is BirthdayItem => !!item)
      .sort((a, b) => a.day - b.day || a.nome.localeCompare(b.nome));
  });

  readonly metrics = computed<MetricCard[]>(() => {
    const area = this.currentArea();
    if (area === 'admin') {
      return [
        this.metric('Abertas agora', this.openRh().length + this.openTi().length + this.openCompras().length, 'RH, TI e Compras', 'attention'),
        this.metric('Prioridade alta', this.highPriorityAlerts().length, 'Solicitações criticas ou altas', 'danger'),
        this.metric('Reabertas', this.reopenedRh().length + this.reopenedTi().length, 'Itens que voltaram ao fluxo', 'danger'),
        this.metric('Administracao', this.adminQueue().length, 'Pendencias para responsaveis', 'neutral'),
      ];
    }

    if (area === 'rh') {
      const items = this.rhScope();
      return [
        this.metric('Abertas', items.filter((item) => this.isOpenStatus(item.status)).length, 'Solicitacoes em atendimento', 'attention'),
        this.metric('Reabertas', items.filter((item) => this.isReopened(item.status)).length, 'Retornaram para analise', 'danger'),
        this.metric('Alta prioridade', items.filter((item) => this.isHighPriority(item.prioridade)).length, 'Demandas sensiveis', 'danger'),
        this.metric('Sem responsavel', items.filter((item) => !item.responsavel?.trim()).length, 'Precisam de triagem', 'neutral'),
      ];
    }

    if (area === 'ti') {
      const items = this.tiScope();
      return [
        this.metric('Abertos', items.filter((item) => this.isOpenStatus(item.status)).length, 'Chamados ativos', 'attention'),
        this.metric('Reabertos', items.filter((item) => item.reaberto || this.isReopened(item.status)).length, 'Voltaram para suporte', 'danger'),
        this.metric('Alta prioridade', items.filter((item) => this.isHighPriority(item.prioridade)).length, 'Incidentes relevantes', 'danger'),
        this.metric('Sem responsavel', items.filter((item) => !item.responsavel?.trim()).length, 'Aguardando atribuição', 'neutral'),
      ];
    }

    if (area === 'compras') {
      const items = this.comprasScope();
      return [
        this.metric('Aguardando diretoria', items.filter((item) => item.status === 'Aguardando Diretoria').length, 'Pendentes de aprovacao', 'attention'),
        this.metric('Em compras', items.filter((item) => item.status.includes('Compras') || item.status === 'Em compras').length, 'Com comprador ou fila ativa', 'neutral'),
        this.metric('Alta prioridade', items.filter((item) => this.isHighPriority(item.prioridade)).length, 'Compras urgentes', 'danger'),
        this.metric('Concluidas', items.filter((item) => item.status === 'Concluida').length, 'Finalizadas no fluxo', 'success'),
      ];
    }

    return [
      this.metric('Minhas solicitacoes RH', this.myRhItems().filter((item) => this.isOpenStatus(item.status)).length, 'Demandas abertas por voce', 'attention'),
      this.metric('Meus chamados TI', this.myTiItems().filter((item) => this.isOpenStatus(item.status)).length, 'Chamados em aberto', 'attention'),
      this.metric('Minhas compras', this.myCompraItems().filter((item) => !this.isFinalCompra(item)).length, 'Solicitações em andamento', 'neutral'),
      this.metric('Avaliacoes pendentes', this.pendingEvaluations(), 'Atendimentos encerrados a avaliar', 'success'),
    ];
  });

  readonly alerts = computed<AlertItem[]>(() => {
    const area = this.currentArea();
    let alerts: AlertItem[] = [];

    if (area === 'admin') {
      alerts = [
        ...this.rhAlerts(this.rhItems()),
        ...this.tiAlerts(this.tiItems()),
        ...this.comprasAlerts(this.comprasItems()),
      ];
    } else if (area === 'rh') {
      alerts = this.rhAlerts(this.rhScope());
    } else if (area === 'ti') {
      alerts = this.tiAlerts(this.tiScope());
    } else if (area === 'compras') {
      alerts = this.comprasAlerts(this.comprasScope());
    } else {
      alerts = [
        ...this.rhAlerts(this.myRhItems()),
        ...this.tiAlerts(this.myTiItems()),
        ...this.comprasAlerts(this.myCompraItems()),
      ];
    }

    return alerts
      .sort((a, b) => this.priorityWeight(b.priority) - this.priorityWeight(a.priority) || this.dateTime(b.date) - this.dateTime(a.date))
      .slice(0, 8);
  });

  readonly actions = computed<DashboardAction[]>(() => {
    const area = this.currentArea();
    if (area === 'admin') {
      return [
        { label: 'Administrar RH', route: '/rh', description: 'Fila completa de solicitações de RH.', enabled: true },
        { label: 'Administrar TI', route: '/ti', description: 'Chamados, responsaveis e reaberturas.', enabled: true },
        { label: 'Administrar Compras', route: '/compras', description: 'Aprovações e etapa de compras.', enabled: true },
        { label: 'Usuarios', route: '/usuarios', description: 'Crie acessos e ajuste perfis.', enabled: this.auth.hasAnyRole(['Admin', 'TI']) },
      ];
    }

    if (area === 'rh') {
      return [
        { label: 'Abrir solicitacao', route: '/solicitacoes', description: 'Registrar uma nova demanda de RH.', enabled: true },
        { label: 'Painel RH', route: '/rh', description: 'Administrar fila de RH.', enabled: this.auth.hasAnyRole(['Admin', 'RH']) },
      ];
    }

    if (area === 'ti') {
      return [
        { label: 'Abrir chamado', route: '/ti', description: 'Criar ou acompanhar chamado de TI.', enabled: true },
        { label: 'Equipamentos', route: '/ti/equipamentos', description: 'Controle de entregas e retornos.', enabled: this.auth.hasAnyRole(['Admin', 'TI']) },
      ];
    }

    if (area === 'compras') {
      return [
        { label: 'Nova compra', route: '/compras', description: 'Solicitar material, serviço ou contratação.', enabled: true },
        { label: 'Aprovacoes', route: '/compras', description: 'Acompanhar diretoria e comprador.', enabled: this.auth.hasAnyRole(['Admin', 'Diretoria', 'Compras']) },
      ];
    }

    return [
      { label: 'Solicitar RH', route: '/solicitacoes', description: 'Abrir uma demanda administrativa ou de pessoas.', enabled: true },
      { label: 'Chamado TI', route: '/ti', description: 'Acionar suporte técnico.', enabled: true },
      { label: 'Solicitar compra', route: '/compras', description: 'Enviar uma necessidade de compra.', enabled: true },
    ];
  });

  ngOnInit(): void {
    if (!this.isBrowser) {
      return;
    }

    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    void this.spinner.show();
    forkJoin({
      rh: this.rhService.list().pipe(catchError(() => of([] as SolicitacaoRH[]))),
      ti: this.tiService.list().pipe(catchError(() => of([] as ChamadoTI[]))),
      compras: this.comprasService.list().pipe(catchError(() => of([] as SolicitacaoCompra[]))),
      users: this.auth.listUsers().pipe(catchError(() => of([] as User[]))),
    }).subscribe({
      next: ({ rh, ti, compras, users }) => {
        this.rhItems.set(rh);
        this.tiItems.set(ti);
        this.comprasItems.set(compras);
        this.users.set(users);
        this.loading.set(false);
        void this.spinner.hide();
      },
      error: () => {
        this.loading.set(false);
        void this.spinner.hide();
        this.toastr.error('Nao foi possivel carregar os indicadores do dashboard.', 'Dashboard');
      },
    });
  }

  openAction(action: DashboardAction): void {
    if (!action.enabled) {
      this.toastr.warning('Seu perfil nao tem acesso a esta area.', 'Acesso restrito');
      return;
    }

    void this.router.navigateByUrl(action.route);
  }

  openAlert(alert: AlertItem): void {
    void this.router.navigateByUrl(alert.route);
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

  private metric(label: string, value: number, detail: string, tone: MetricCard['tone']): MetricCard {
    return { label, value, detail, tone };
  }

  private rhScope(): SolicitacaoRH[] {
    return this.auth.hasAnyRole(['Admin', 'RH']) ? this.rhItems() : this.myRhItems();
  }

  private tiScope(): ChamadoTI[] {
    return this.auth.hasAnyRole(['Admin', 'TI']) ? this.tiItems() : this.myTiItems();
  }

  private comprasScope(): SolicitacaoCompra[] {
    return this.auth.hasAnyRole(['Admin', 'Compras', 'Diretoria']) ? this.comprasItems() : this.myCompraItems();
  }

  private openRh(): SolicitacaoRH[] {
    return this.rhItems().filter((item) => this.isOpenStatus(item.status));
  }

  private openTi(): ChamadoTI[] {
    return this.tiItems().filter((item) => this.isOpenStatus(item.status));
  }

  private openCompras(): SolicitacaoCompra[] {
    return this.comprasItems().filter((item) => !this.isFinalCompra(item));
  }

  private reopenedRh(): SolicitacaoRH[] {
    return this.rhItems().filter((item) => this.isReopened(item.status));
  }

  private reopenedTi(): ChamadoTI[] {
    return this.tiItems().filter((item) => item.reaberto || this.isReopened(item.status));
  }

  private adminQueue(): unknown[] {
    return [
      ...this.rhItems().filter((item) => !item.responsavel?.trim() && this.isOpenStatus(item.status)),
      ...this.tiItems().filter((item) => !item.responsavel?.trim() && this.isOpenStatus(item.status)),
      ...this.comprasItems().filter((item) => item.status === 'Aguardando Diretoria' || item.status.includes('Compras')),
    ];
  }

  private highPriorityAlerts(): unknown[] {
    return [
      ...this.rhItems().filter((item) => this.isHighPriority(item.prioridade)),
      ...this.tiItems().filter((item) => this.isHighPriority(item.prioridade)),
      ...this.comprasItems().filter((item) => this.isHighPriority(item.prioridade)),
    ];
  }

  private myRhItems(): SolicitacaoRH[] {
    const id = this.user()?.id;
    return this.rhItems().filter((item) => item.userid === id);
  }

  private myTiItems(): ChamadoTI[] {
    const id = this.user()?.id;
    return this.tiItems().filter((item) => item.userid === id);
  }

  private myCompraItems(): SolicitacaoCompra[] {
    const id = this.user()?.id;
    return this.comprasItems().filter((item) => item.userid === id);
  }

  private pendingEvaluations(): number {
    return [
      ...this.myRhItems().filter((item) => item.avaliacaoPendente),
      ...this.myTiItems().filter((item) => item.avaliacaoPendente),
    ].length;
  }

  private rhAlerts(items: SolicitacaoRH[]): AlertItem[] {
    return items
      .filter((item) => this.isOpenStatus(item.status) || this.isHighPriority(item.prioridade) || this.isReopened(item.status))
      .map((item) => ({
        id: `rh-${item.id}`,
        sector: 'RH',
        title: `#${item.id} ${item.titulo}`,
        detail: `${item.solicitante} - ${item.departamento || 'Sem departamento'}`,
        priority: item.prioridade,
        status: item.status,
        route: this.auth.hasAnyRole(['Admin', 'RH']) ? '/rh' : '/solicitacoes',
        date: item.dataSolicitacao,
      }));
  }

  private tiAlerts(items: ChamadoTI[]): AlertItem[] {
    return items
      .filter((item) => this.isOpenStatus(item.status) || this.isHighPriority(item.prioridade) || item.reaberto || this.isReopened(item.status))
      .map((item) => ({
        id: `ti-${item.id}`,
        sector: 'TI',
        title: `#${item.id} ${item.titulo}`,
        detail: `${item.solicitante} - ${item.categoria || 'Chamado'}`,
        priority: item.prioridade,
        status: item.reaberto ? 'Reaberto' : item.status,
        route: '/ti',
        date: item.dataReabertura || item.dataAbertura,
      }));
  }

  private comprasAlerts(items: SolicitacaoCompra[]): AlertItem[] {
    return items
      .filter((item) => !this.isFinalCompra(item) || this.isHighPriority(item.prioridade))
      .map((item) => ({
        id: `compras-${item.id}`,
        sector: 'Compras',
        title: `#${item.id} ${item.titulo}`,
        detail: `${item.solicitante} - ${item.departamento || 'Sem departamento'}`,
        priority: item.prioridade,
        status: item.status,
        route: '/compras',
        date: item.dataSolicitacao,
      }));
  }

  private isOpenStatus(status: string): boolean {
    const normalized = this.normalize(status);
    return !['concluida', 'concluido', 'encerrada', 'encerrado', 'cancelada', 'cancelado', 'reprovada', 'reprovado'].includes(normalized);
  }

  private isFinalCompra(item: SolicitacaoCompra): boolean {
    return !!item.dataConclusao || ['Concluida', 'Cancelada', 'Reprovada'].includes(item.status);
  }

  private isHighPriority(priority: string): boolean {
    return ['alta', 'critica', 'crítica', 'urgente'].includes(this.normalize(priority));
  }

  private isReopened(status: string): boolean {
    return this.normalize(status).includes('reabert');
  }

  private priorityWeight(priority: string): number {
    const normalized = this.normalize(priority);
    if (normalized.includes('critic') || normalized.includes('urgente')) {
      return 4;
    }

    if (normalized.includes('alta')) {
      return 3;
    }

    if (normalized.includes('media')) {
      return 2;
    }

    return 1;
  }

  private dateTime(date: string): number {
    const value = new Date(date).getTime();
    return Number.isNaN(value) ? 0 : value;
  }

  private matchesToday(value: string): boolean {
    const date = this.dateParts(value);
    const today = new Date();
    return !!date && date.month === today.getMonth() && date.day === today.getDate();
  }

  private dateParts(value: string): { month: number; day: number } | null {
    if (!value) {
      return null;
    }

    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) {
      return { month: Number(match[2]) - 1, day: Number(match[3]) };
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return null;
    }

    return { month: date.getMonth(), day: date.getDate() };
  }

  private normalize(value: string | null | undefined): string {
    return String(value ?? '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }
}
