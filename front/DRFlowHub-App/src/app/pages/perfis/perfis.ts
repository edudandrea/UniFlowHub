import { Component, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/auth.service';
import { AcessoSistema, PerfilSistema, PerfisService } from '../../core/perfis.service';
import { ProfileFlowService } from '../../core/profile-flow.service';
import { ThemeService } from '../../core/theme.service';

@Component({
  selector: 'app-perfis',
  imports: [FormsModule],
  templateUrl: './perfis.html',
  styleUrl: './perfis.scss',
})
export class PerfisPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly service = inject(PerfisService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);
  private readonly profileFlow = inject(ProfileFlowService);

  readonly theme = inject(ThemeService);
  readonly user = computed(() => this.auth.user());
  readonly perfis = signal<PerfilSistema[]>([]);
  readonly acessos = signal<AcessoSistema[]>([]);
  readonly selected = signal<PerfilSistema | null>(null);
  readonly nome = signal('');
  readonly acessosSelecionados = signal<string[]>([]);
  readonly profileMenuOpen = signal(false);
  readonly modalOpen = signal(false);
  readonly saving = signal(false);
  readonly acessosPorGrupo = computed(() => {
    const groups = new Map<string, AcessoSistema[]>();
    for (const acesso of this.acessos()) {
      groups.set(acesso.grupo, [...(groups.get(acesso.grupo) ?? []), acesso]);
    }
    return Array.from(groups.entries())
      .sort(([a], [b]) => a.localeCompare(b, 'pt-BR'))
      .map(([grupo, items]) => ({
        grupo,
        items: items.sort((left, right) => left.nome.localeCompare(right.nome, 'pt-BR')),
      }));
  });

  readonly openedGroups = signal<Set<string>>(new Set());

  // Group modal animation state
  readonly groupModalOpen = signal(false);
  readonly groupModalGroup = signal<string | null>(null);
  readonly groupModalStyle = signal<{ transform: string; initialTransform: string; width: number; height: number } | null>(null);
  readonly groupModalAnimating = signal(false);
  readonly groupModalItems = computed(() => {
    const g = this.acessosPorGrupo().find((x) => x.grupo === this.groupModalGroup());
    return g ? g.items : [];
  });

  groupExpanded(grupo: string): boolean {
    return this.openedGroups().has(grupo);
  }

  toggleGroup(grupo: string): void {
    const current = new Set(this.openedGroups());
    if (current.has(grupo)) {
      current.delete(grupo);
    } else {
      current.add(grupo);
    }
    this.openedGroups.set(current);
  }

  openGroupModal(event: Event, grupo: string): void {
    event.stopPropagation();
    const target = (event.currentTarget ?? event.target) as HTMLElement;
    if (!target) {
      this.groupModalGroup.set(grupo);
      this.groupModalOpen.set(true);
      return;
    }

    const rect = target.getBoundingClientRect();
    const modalWidth = Math.min(900, Math.round(window.innerWidth * 0.9));
    const modalHeight = Math.min(720, Math.round(window.innerHeight * 0.8));
    const originCenterX = rect.left + rect.width / 2;
    const originCenterY = rect.top + rect.height / 2;
    const centerX = window.innerWidth / 2;
    const centerY = window.innerHeight / 2;
    const tx = Math.round(originCenterX - centerX);
    const ty = Math.round(originCenterY - centerY);
    const scale = Math.max(0.28, rect.width / modalWidth);

    const initial = `translate(calc(-50% + ${tx}px), calc(-50% + ${ty}px)) scale(${scale})`;
    const final = `translate(-50%, -50%) scale(1)`;

    this.groupModalStyle.set({ transform: initial, initialTransform: initial, width: modalWidth, height: modalHeight });
    this.groupModalGroup.set(grupo);
    this.groupModalOpen.set(true);

    requestAnimationFrame(() => {
      this.groupModalAnimating.set(true);
      requestAnimationFrame(() => {
        this.groupModalStyle.update((s) => (s ? { ...s, transform: final } : s));
      });
    });
  }

  closeGroupModal(): void {
    if (!this.groupModalOpen()) return;
    const s = this.groupModalStyle();
    if (!s) {
      this.groupModalOpen.set(false);
      this.groupModalGroup.set(null);
      return;
    }

    this.groupModalStyle.set({ ...s, transform: s.initialTransform });
    this.groupModalAnimating.set(false);
    window.setTimeout(() => {
      this.groupModalOpen.set(false);
      this.groupModalGroup.set(null);
      this.groupModalStyle.set(null);
    }, 360);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.service.listAcessos().subscribe({ next: (items) => this.acessos.set(items) });
    this.service.list().subscribe({ next: (items) => this.perfis.set(items) });
  }

  select(perfil: PerfilSistema): void {
    this.selected.set(perfil);
    this.nome.set(perfil.nome);
    this.acessosSelecionados.set([...(perfil.acessos ?? [])]);
    this.modalOpen.set(true);
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

  novo(): void {
    this.selected.set(null);
    this.nome.set('');
    this.acessosSelecionados.set([]);
    this.modalOpen.set(true);
  }

  closeModal(): void {
    if (!this.saving()) {
      this.modalOpen.set(false);
    }
  }

  toggleAcesso(chave: string, checked: boolean): void {
    const current = this.acessosSelecionados();
    this.acessosSelecionados.set(checked ? Array.from(new Set([...current, chave])) : current.filter((item) => item !== chave));
  }

  hasAcesso(chave: string): boolean {
    return this.acessosSelecionados().includes(chave);
  }

  save(): void {
    this.saving.set(true);
    this.service.save({ nome: this.nome(), acessos: this.acessosSelecionados() }).subscribe({
      next: () => {
        this.saving.set(false);
        this.toastr.success('Perfil salvo com sucesso.', 'Perfis');
        this.modalOpen.set(false);
        this.selected.set(null);
        this.nome.set('');
        this.acessosSelecionados.set([]);
        this.load();
      },
      error: () => {
        this.saving.set(false);
        this.toastr.error('Não foi possível salvar o perfil.', 'Erro');
      },
    });
  }
}
