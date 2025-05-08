import { NgClass, NgFor, NgIf } from '@angular/common';
import { AfterViewInit, Component, HostListener, OnInit } from '@angular/core';
import { User } from '../_models/user';
import { AccountService } from '../_services/account.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Color, NgxChartsModule, ScaleType, LegendPosition } from '@swimlane/ngx-charts';
import { TicketService } from '../_services/ticket.service';
import { DashboardService } from '../_services/dashboard.service';
import { GlobalLoaderService } from '../_services/global-loader.service';
import { TicketStatDto } from '../_models/ticket-stat.dto';
import { FormsModule } from '@angular/forms';
import { TicketFilterRequest } from '../_models/TicketFilterRequest';
import { curveBasis } from 'd3';
import { forkJoin } from 'rxjs';
import { PaysModalComponent } from '../PaysFile/pays-modal/pays-modal.component';
import { OverlayModalService } from '../_services/overlay-modal.service';

@Component({
  selector: 'app-tableau-bord',
  standalone: true,
  imports: [NgFor, NgIf, NgxChartsModule, RouterLink, FormsModule],
  templateUrl: './tableau-bord.component.html',
  styleUrls: ['./tableau-bord.component.css']
})
export class TableauBordComponent implements OnInit, AfterViewInit {
  filter = {
    userId: null as number|null,
    clientId: null as number|null,
    personnelId: null as number|null,
    start: null as string|null,
    end: null as string|null,
    granularity: 'daily' as 'daily'|'weekly'|'monthly'|'yearly'|'none'
  };
  
  users: User[] = [];            // Charger tous les utilisateurs si on veut filtrer
  userSeries: any[] = [];        // pour ngx-charts-line-chart
  statusSeries: { name: string; value: number }[] = [];

  LegendPosition = LegendPosition;
  currentUser: User | null = null;
  userInitials = "";

  ticketCounts: any[] = [];
  view: [number, number] = [1400, 500]; // Taille par défaut (sera mise à jour dynamiquement)

  clients: User[] = [];
  personnel: User[] = [];

  colorScheme: Color = {
    name: 'custom-gradient',
    selectable: true,
    group: ScaleType.Ordinal,
    domain: [
      '#ffbb94', 
      '#fb9590', 
      '#dc586d', 
      '#a33757', 
      '#852e4e', 
      '#4c1d3d'
    ]
  };

  noDataScheme: Color = {
    name: 'no-data',
    selectable: false,
    group: ScaleType.Ordinal,
    domain: ['#cccccc']   // un seul gris
  };

  // Propriétés pour les petits containers
  categoriesCount: number = 0;
  paysCount: number = 0;
  projectsCount: number = 0;
  societesCount: number = 0;
  statutsCount: number = 0;
  ticketsCount: number = 0;
  usersCount: number = 0;

  clientsCount: number = 0;
  personnelCount: number = 0;
  curve: any = curveBasis;

  clientUsers: User[] = [];
  personnelUsers: User[] = [];

  myTicketsCount: number = 0;

  constructor(
    private ticketService: TicketService,
    private accountService: AccountService,
    private router: Router,
    private dashboardService: DashboardService,
    private route: ActivatedRoute,
    private globalLoaderService: GlobalLoaderService,
    private overlayModalService: OverlayModalService
  ) { }

  ngOnInit(): void {
    this.globalLoaderService.showGlobalLoader();
    this.currentUser = this.accountService.currentUser();
    if (this.currentUser) {
      this.userInitials = this.currentUser.firstName.charAt(0) + this.currentUser.lastName.charAt(0);
      this.loadTicketCounts();
      this.loadDashboardCounts();
      this.dashboardService.getMyTicketsCount()
        .subscribe(count => this.myTicketsCount = count);
      this.loadAllUsers();
      this.applyFilters();
    }
    // Définition initiale de la taille du graphique
    this.setChartSize();
  }

  ngAfterViewInit() {
    this.route.fragment.subscribe(fragment => {
      if (fragment === 'pie-chart-container') {
        this.scrollToPieChart();
      }
    });
  }

  private scrollToPieChart() {
    const element = document.getElementById('pie-chart-container');
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }

  isPieNoData(): boolean {
    return this.ticketCounts.length === 1
        && this.ticketCounts[0].name === 'Aucune donnée';
  }
  loadTicketCounts() {
    this.ticketService.getTicketCountByStatus().subscribe({
      next: (data: any[]) => {
        this.ticketCounts = data.map(item => ({
          id: item.id,
          name: item.name,
          value: item.count
        }));
      },
      error: (err) => {
        console.error('Erreur lors de la récupération des stats par statut', err);
      },
      complete: () => {
        // Masque le loader pour cet appel
        this.globalLoaderService.hideGlobalLoader();
      }
    });
  }

  // Chargement des autres counts du dashboard
  loadDashboardCounts() {
    this.dashboardService.getDashboardCounts().subscribe({
      next: (data: any) => {
        this.categoriesCount = data.categoriesCount;
        this.paysCount       = data.paysCount;
        this.projectsCount   = data.projectsCount;
        this.societesCount   = data.societesCount;
        this.statutsCount    = data.statutsCount;
        this.ticketsCount    = data.ticketsCount;
        this.usersCount      = data.usersCount;
        this.clientsCount    = data.clientsCount;
        this.personnelCount  = data.personnelCount;
      },
      error: (err) => {
        console.error('Erreur lors de la récupération des dashboard counts', err);
      },
      complete: () => {
        // Masque le loader pour cet appel
        this.globalLoaderService.hideGlobalLoader();
      }
    });
  }

  // Mise à jour de la taille du graphique en fonction de la largeur de l'écran
  @HostListener('window:resize', ['$event'])
  onResize() {
    this.setChartSize();
  }

  setChartSize(): void {
    const width = window.innerWidth;
    // Exemple : on réduit la largeur / hauteur pour mobile ou tout le temps
    const chartWidth  = Math.max(300, Math.min(500, width / 2 - 24));
    const chartHeight = 300; // <–– hauteur fixe plus petite
    this.view = [chartWidth, chartHeight];
  } 
  
  isSuperAdmin(): boolean {
    return this.currentUser?.role === 'Super Admin';
  }
  
  isChefDeProjet(): boolean {
    return this.currentUser?.role === 'Chef de Projet';
  }
  
  isCollaborateur(): boolean {
    return this.currentUser?.role === 'Collaborateur';
  }
  
  isClient(): boolean {
    return this.currentUser?.role === 'Client';
  }

  loadAllUsers() {
    this.accountService.getAllUsers()
      .subscribe(u => {
        this.users = u;
        // Une fois les users chargés, on remplit les listes filtrées
        this.clientUsers = this.users.filter(x => x.role.toLowerCase() === 'client');
        this.personnelUsers = this.users.filter(x => x.role.toLowerCase() !== 'client');
      });
  }
  
  

  applyFilters(): void {
    const currentUser = this.accountService.currentUser();
    if (!currentUser) return;
  
    // Préparer les filtres date et granularité
    const start = this.filter.start ?? undefined;
    const end   = this.filter.end   ?? undefined;
    const gran  = this.filter.granularity;
  
    // Filtres client/personnel sélectionnés (pour non-CP)
    const clientId    = this.filter.clientId ?? undefined;
    const personnelId = this.filter.personnelId ?? undefined;
  
    if (this.isChefDeProjet()) {
      // ─── COURBE “Mes tickets” ───
      const reqUser: TicketFilterRequest = {
        userId:      currentUser.id,
        clientId:    undefined,
        personnelId: undefined,
        start:       start,
        end:         end,
        granularity: gran
      };
      this.dashboardService.getTicketsByUser(reqUser)
        .subscribe({
          next: data => {
            this.userSeries = data.length
              ? [{ name: 'Mes tickets', series: data.map(d => ({ name: d.key, value: d.count })) }]
              : [{ name: 'Aucune donnée', series: [{ name: '', value: 0 }] }];
          },
          error: err => {
            console.error('Erreur getTicketsByUser', err);
            this.userSeries = [{ name: 'Erreur', series: [{ name: '', value: 0 }] }];
          }
        });
  
      // ─── BAR + PIE “Mes tickets” ───
      const reqStatusCP: TicketFilterRequest = {
        userId:      undefined,
        clientId:    undefined,
        personnelId: currentUser.id,
        start:       start,
        end:         end,
        granularity: 'none'
      };
      this.dashboardService.getTicketsByStatus(reqStatusCP)
        .subscribe({
          next: status => {
            if (status.length) {
              this.statusSeries = status.map(s => ({ name: s.key, value: s.count }));
              this.ticketCounts = status.map(s => ({ id: 0, name: s.key, value: s.count }));
            } else {
              this.statusSeries = [{ name: 'Aucune donnée', value: 0 }];
              this.ticketCounts  = [{ id: 0, name: 'Aucune donnée', value: 1 }];
            }
          },
          error: err => {
            console.error('Erreur getTicketsByStatus (CP)', err);
            this.statusSeries = [{ name: 'Erreur', value: 0 }];
            this.ticketCounts  = [{ id: 0, name: 'Erreur', value: 0 }];
          }
        });
  
    } else {
      // ─── COURBE “Tous les tickets” ───
      const reqFilt: TicketFilterRequest = {
        userId:      undefined,
        clientId:    clientId,
        personnelId: personnelId,
        start:       start,
        end:         end,
        granularity: gran
      };
      this.dashboardService.getTicketsFiltered(reqFilt)
        .subscribe({
          next: filtered => {
            this.userSeries = filtered.length
              ? [{ name: 'Tickets filtrés', series: filtered.map(f => ({ name: f.key, value: f.count })) }]
              : [{ name: 'Aucune donnée', series: [{ name: '', value: 0 }] }];
          },
          error: err => {
            console.error('Erreur getTicketsFiltered', err);
            this.userSeries = [{ name: 'Erreur', series: [{ name: '', value: 0 }] }];
          }
        });
  
      // ─── BAR + PIE “Tous les tickets” ───
      const reqStatus: TicketFilterRequest = {
        userId:      undefined,
        clientId:    clientId,
        personnelId: personnelId,
        start:       start,
        end:         end,
        granularity: 'none'
      };
      this.dashboardService.getTicketsByStatus(reqStatus)
        .subscribe({
          next: status => {
            if (status.length) {
              this.statusSeries = status.map(s => ({ name: s.key, value: s.count }));
              this.ticketCounts = status.map(s => ({ id: 0, name: s.key, value: s.count }));
            } else {
              this.statusSeries = [{ name: 'Aucune donnée', value: 0 }];
              this.ticketCounts  = [{ id: 0, name: 'Aucune donnée', value: 1 }];
            }
          },
          error: err => {
            console.error('Erreur getTicketsByStatus', err);
            this.statusSeries = [{ name: 'Erreur', value: 0 }];
            this.ticketCounts  = [{ id: 0, name: 'Erreur', value: 0 }];
          }
        });
    }
  }
  openPaysModal() {
    const modalRef = this.overlayModalService.open(PaysModalComponent);
    // modalRef est ici l'instance de PaysModalComponent
    modalRef.added.subscribe(() => {
      // Recharge le compteur dès qu’un pays est ajouté dans le modal
      this.loadDashboardCounts();
    });
  }
}