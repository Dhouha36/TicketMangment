// categories.module.ts (dans le dossier Categoriess/)
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { RoleGuard } from '../_guards/role.guard';

import { CategoriesComponent } from './categories/categories.component';

const routes: Routes = [
  {
    path: '',
    canActivate: [RoleGuard],
    data: { roles: ['Super Admin'] },
    component: CategoriesComponent
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    CategoriesComponent
  ]
})
export class CategoriesModule {}
