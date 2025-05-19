import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { RoleGuard } from '../_guards/role.guard';
import { ListClientsComponent } from './list-clients/list-clients.component';
import { AjouterClientComponent } from './ajouter-client/ajouter-client.component';
import { DetailsClientComponent } from './details-client/details-client.component';


const routes: Routes = [
  {
    path: '',
    canActivate: [RoleGuard],
    data: { roles: ['Super Admin'] },
    children: [
      { path: '', component: ListClientsComponent },
      { path: 'AjouterClient', component: AjouterClientComponent },
      { path: 'details/:id', component: DetailsClientComponent }
      
    ]
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ListClientsComponent,
    AjouterClientComponent
  ]
})
export class ClientsModule {}