import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './connexion/login/login.component';
import { LayoutComponent } from './layout/layout.component'; 
import { TestErrorsComponent } from './errors/test-errors/test-errors.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { authGuard } from './_guards/auth.guard';
import { TableauBordComponent } from './tableau-bord/tableau-bord.component';
import { ForgotPasswordComponent } from './connexion/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './connexion/reset-password/reset-password.component';
import { NotificationsComponent } from './notifications/notifications.component';

export const routes: Routes = [
  // Page de login
  { path: '', component: LoginComponent },

  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'notifications', component: NotificationsComponent},
  // Layout principal : header + sidenav
  {
    path: 'home',
    component: LayoutComponent,     
    runGuardsAndResolvers: 'always',
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: TableauBordComponent },
      {
        path: 'MesTickets',
        loadChildren: () =>
          import('./Tickets/tickets.module').then(m => m.TicketsModule),
        data: { filterType: 'associated' }
      },
      {
        path: 'Tickets',
        loadChildren: () =>
          import('./Tickets/tickets.module').then(m => m.TicketsModule),
        data: { filterType: 'projetUser' }
      },
      {
        path: 'profile',
        loadChildren: () =>
          import('./user-profile/user-profile.module').then(m => m.UserProfileModule),
      },
      {
        path: 'utilisateurs',
        loadChildren: () =>
          import('./utilisateurs/utilisateurs.module').then(m => m.UtilisateursModule),
      },
      {
        path: 'clients',
        loadChildren: () =>
          import('./Clients/clients.module').then(m => m.ClientsModule),
      },
      {
        path: 'Projets',
        loadChildren: () =>
          import('./Projets/projets.module').then(m => m.ProjetsModule),
      },
      {
        path: 'Societes',
        loadChildren: () =>
          import('./Societes/societes.module').then(m => m.SocietesModule),
      },
      {
        path: 'Categories',
        loadChildren: () =>
          import('./Categoriess/categories.module').then(m => m.CategoriesModule),
      },
      // Par défaut sous /home → dashboard
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

      // wildcard ** uniquement DANS /home → affiche NotFoundComponent
      { path: '**', redirectTo: '/not-found' },
    ]
  },

  { path: 'errors', component: TestErrorsComponent },
  { path: 'not-found', component: NotFoundComponent },
  { path: 'server-error', component: ServerErrorComponent },

  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes, { 
      anchorScrolling: 'enabled',
      scrollOffset: [0, 1000],
      onSameUrlNavigation: 'reload'
    })
  ],
  exports: [RouterModule]
})
export class AppRoutingModule { }