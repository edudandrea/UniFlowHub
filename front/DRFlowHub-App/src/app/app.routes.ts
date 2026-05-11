import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/auth.guard';
import { AdminPage } from './pages/admin/admin';
import { ComprasPage } from './pages/compras/compras';
import { EquipamentosTIPage } from './pages/equipamentos-ti/equipamentos-ti';
import { HubPage } from './pages/hub/hub';
import { LoginPage } from './pages/login/login';
import { PecasBiPage } from './pages/pecas-bi/pecas-bi';
import { SolicitacoesPage } from './pages/solicitacoes/solicitacoes';
import { TiPage } from './pages/ti/ti';
import { UsuariosPage } from './pages/usuarios/usuarios';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  {
    path: 'hub',
    component: HubPage,
    canActivate: [authGuard],
  },
  {
    path: 'solicitacoes',
    component: SolicitacoesPage,
    canActivate: [authGuard],
  },
  {
    path: 'admin',
    component: AdminPage,
    canActivate: [roleGuard(['Admin', 'RH'])],
  },
  {
    path: 'rh',
    component: AdminPage,
    canActivate: [roleGuard(['Admin', 'RH'])],
  },
  {
    path: 'ti',
    component: TiPage,
    canActivate: [authGuard],
  },
  {
    path: 'ti/equipamentos',
    component: EquipamentosTIPage,
    canActivate: [roleGuard(['Admin', 'TI'])],
  },
  {
    path: 'compras',
    component: ComprasPage,
    canActivate: [authGuard],
  },
  {
    path: 'vendas-pecas',
    component: PecasBiPage,
    canActivate: [authGuard],
  },
  {
    path: 'usuarios',
    component: UsuariosPage,
    canActivate: [roleGuard(['Admin', 'TI'])],
  },
  { path: '', redirectTo: 'hub', pathMatch: 'full' },
  { path: '**', redirectTo: '' },
];
