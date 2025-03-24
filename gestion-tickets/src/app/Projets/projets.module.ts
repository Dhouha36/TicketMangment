// projets.module.ts (dans le dossier Projets/)
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { RoleGuard } from '../_guards/role.guard';

import { ListeProjetsComponent } from './liste-projets/liste-projets.component';
import { AjouterProjetComponent } from './ajouter-projet/ajouter-projet.component';
import { DetailsProjetComponent } from './details-projet/details-projet.component';

const routes: Routes = [
  {
    path: '',
    canActivate: [RoleGuard],
    data: { roles: ['Super Admin', 'Chef de Projet', 'Collaborateur'] },
    children: [
      { path: '', component: ListeProjetsComponent },
      { path: 'ajouterProjet', component: AjouterProjetComponent },
      { path: 'details/:id', component: DetailsProjetComponent }
    ]
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ListeProjetsComponent,
    AjouterProjetComponent,
    DetailsProjetComponent
  ]
})
export class ProjetsModule {}
