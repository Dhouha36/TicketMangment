// utilisateurs.module.ts
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { RoleGuard } from '../_guards/role.guard';

import { ListUtilisateursComponent } from './list-utilisateurs/list-utilisateurs.component';
import { AjouterUtilisateurComponent } from './ajouter-utilisateur/ajouter-utilisateur.component';
import { DetailsUtilisateurComponent } from './details-utilisateur/details-utilisateur.component';

const routes: Routes = [
  {
    path: '',
    canActivate: [RoleGuard],
    data: { roles: ['Super Admin'] },
    children: [
      { path: '', component: ListUtilisateursComponent },
      { path: 'AjouterUtilisateur', component: AjouterUtilisateurComponent },
      { path: 'details/:id', component: DetailsUtilisateurComponent }
    ]
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ListUtilisateursComponent,
    AjouterUtilisateurComponent,
    DetailsUtilisateurComponent
  ]
})
export class UtilisateursModule {}
