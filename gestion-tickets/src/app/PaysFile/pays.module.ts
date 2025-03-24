// pays.module.ts (dans le dossier PaysFile/)
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { RoleGuard } from '../_guards/role.guard';

import { PaysComponent } from './pays/pays.component';
import { AjouterPaysComponent } from './ajouter-pays/ajouter-pays.component';
import { ModifierPaysComponent } from './modifier-pays/modifier-pays.component';

const routes: Routes = [
  {
    path: '',
    canActivate: [RoleGuard],
    data: { roles: ['Super Admin'] },
    children: [
      { path: '', component: PaysComponent },
      { path: 'ajouterPays', component: AjouterPaysComponent },
      { path: 'ModifierPays/:id', component: ModifierPaysComponent }
    ]
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    PaysComponent,
    AjouterPaysComponent,
    ModifierPaysComponent
  ]
})
export class PaysModule {}
