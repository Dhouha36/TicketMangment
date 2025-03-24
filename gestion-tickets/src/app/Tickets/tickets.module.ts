import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

import { ListTicketsComponent } from './list-tickets/list-tickets.component';
import { AjouterTicketComponent } from './ajouter-ticket/ajouter-ticket.component';
import { TicketDetailsComponent } from './ticket-details/ticket-details.component';

const routes: Routes = [
  { path: '', component: ListTicketsComponent },
  { path: 'ajouterTicket', component: AjouterTicketComponent },
  { path: 'details/:id', component: TicketDetailsComponent }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ListTicketsComponent,
    AjouterTicketComponent,
    TicketDetailsComponent
  ]
})
export class TicketsModule {}
