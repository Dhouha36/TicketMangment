// societes.module.ts (dans le dossier Societes/)
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { RoleGuard } from '../_guards/role.guard';

import { ListeSocietesComponent } from './liste-societes/liste-societes.component';
import { AjouterSocieteComponent } from './ajouter-societe/ajouter-societe.component';
import { ModifierSocieteComponent } from './modifier-societe/modifier-societe.component';

const routes: Routes = [
  {
    path: '',
    canActivate: [RoleGuard],
    data: { roles: ['Super Admin'] },
    children: [
      { path: '', component: ListeSocietesComponent },
      { path: 'ajouterSociete', component: AjouterSocieteComponent },
      { path: 'modifierSociete/:id', component: ModifierSocieteComponent }
    ]
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ListeSocietesComponent,
    AjouterSocieteComponent,
    ModifierSocieteComponent
  ]
})
export class SocietesModule {}
