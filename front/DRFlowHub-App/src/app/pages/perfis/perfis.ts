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
    return Array.from(groups.entries()).map(([grupo, items]) => ({ grupo, items }));
  });

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
        this.toastr.error('Nao foi possivel salvar o perfil.', 'Erro');
      },
    });
  }
}
