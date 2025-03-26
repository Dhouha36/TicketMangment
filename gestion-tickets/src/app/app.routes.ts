import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './connexion/login/login.component';
import { LayoutComponent } from './layout/layout.component'; // Composant qui contient header + sidenav
import { TestErrorsComponent } from './errors/test-errors/test-errors.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { authGuard } from './_guards/auth.guard';
import { TableauBordComponent } from './tableau-bord/tableau-bord.component';
import { ForgotPasswordComponent } from './connexion/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './connexion/reset-password/reset-password.component';

export const routes: Routes = [
  // Page de login
  { path: '', component: LoginComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },

  // Layout principal : header + sidenav
  {
    path: 'home',
    component: LayoutComponent,       // <-- Le composant parent (layout)
    runGuardsAndResolvers: 'always',
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: TableauBordComponent },
      {
        path: 'Tickets',
        loadChildren: () =>
          import('./Tickets/tickets.module').then(m => m.TicketsModule),
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
        path: 'Pays',
        loadChildren: () =>
          import('./PaysFile/pays.module').then(m => m.PaysModule),
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
      // Route par défaut quand on tape /home
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  // Routes d'erreur
  { path: 'errors', component: TestErrorsComponent },
  { path: 'not-found', component: NotFoundComponent },
  { path: 'server-error', component: ServerErrorComponent },

  // Wildcard : tout chemin inconnu redirige vers login ou vers 'not-found'
  { path: '**', redirectTo: '' }
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