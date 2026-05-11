import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { NgxSpinnerModule } from 'ngx-spinner';
import { filter } from 'rxjs';
import { AuthService } from './core/auth.service';
import { ProfileDialogComponent } from './core/profile-dialog.component';
import { ThemeService } from './core/theme.service';
import { Role } from './core/models';

interface ShellLink {
  label: string;
  description: string;
  route: string;
  enabled: boolean;
  roles?: Role[];
  userRoute?: string;
}

const SPINNER_BACKDROP_BY_ROLE: Record<Role, string> = {
  Admin: 'rgba(8, 20, 55, 0.76)',
  RH: 'rgba(104, 38, 132, 0.72)',
  TI: 'rgba(14, 83, 138, 0.72)',
  Diretoria: 'rgba(74, 58, 24, 0.72)',
  Compras: 'rgba(20, 96, 72, 0.72)',
  Gestor: 'rgba(40, 54, 116, 0.72)',
  Usuario: 'rgba(23, 32, 51, 0.70)',
};

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ProfileDialogComponent, NgxSpinnerModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly theme = inject(ThemeService);

  readonly currentUrl = signal(this.router.url);
  readonly showSidebar = computed(() => this.auth.isLoggedIn() && !this.currentUrl().startsWith('/login'));
  readonly spinnerBackdropColor = computed(() => {
    const role = this.auth.user()?.role;
    return role ? SPINNER_BACKDROP_BY_ROLE[role] : SPINNER_BACKDROP_BY_ROLE.Usuario;
  });

  readonly shellLinks: ShellLink[] = [
    { label: 'Dashboard', description: 'Visao inicial', route: '/hub', enabled: true },
    { label: 'Recursos Humanos', description: 'Solicitacoes de RH', route: '/rh', userRoute: '/solicitacoes', enabled: true, roles: ['Admin', 'RH'] },
    { label: 'TI', description: 'Chamados e suporte', route: '/ti', enabled: true },
    { label: 'Compras', description: 'Solicitacoes e aprovacao', route: '/compras', enabled: true },
    { label: 'Vendas Pecas', description: 'B.I comercial de pecas', route: '/vendas-pecas', enabled: true },
    { label: 'Financeiro', description: 'Fluxo financeiro', route: '/hub', enabled: false },
    { label: 'Administrativo', description: 'Demandas internas', route: '/hub', enabled: false },
    { label: 'Operacional', description: 'Solicitacoes operacionais', route: '/hub', enabled: false },
    { label: 'Comercial', description: 'Demandas comerciais', route: '/hub', enabled: false },
    { label: 'Usuarios', description: 'Administracao de acessos', route: '/usuarios', enabled: true, roles: ['Admin', 'TI'] },
  ];
  readonly visibleShellLinks = computed(() => this.shellLinks.filter((link) => link.label !== 'Usuarios' || this.auth.hasAnyRole(['Admin', 'TI'])));

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => this.currentUrl.set(event.urlAfterRedirects));
  }

  openShellLink(link: ShellLink): void {
    if (!link.enabled) {
      return;
    }

    const route = link.roles && !this.auth.hasAnyRole(link.roles) ? link.userRoute : link.route;
    if (!route) {
      return;
    }

    void this.router.navigateByUrl(route);
  }

  isActive(link: ShellLink): boolean {
    const url = this.currentUrl();
    const route = link.roles && !this.auth.hasAnyRole(link.roles) ? link.userRoute : link.route;

    if (!route) {
      return false;
    }

    return url === route || (route !== '/hub' && url.startsWith(`${route}/`));
  }
}
