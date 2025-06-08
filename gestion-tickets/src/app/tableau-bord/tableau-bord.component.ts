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
import { forkJoin, map } from 'rxjs';
import { PaysModalComponent } from '../PaysFile/pays-modal/pays-modal.component';
import { OverlayModalService } from '../_services/overlay-modal.service';
import { ClientDto } from '../DTOs/ClientDto';
import { ClientService } from '../_services/client.service';
import { Projet } from '../_models/Projet';
import { ProjetService } from '../_services/projet.service';

@Component({
  selector: 'app-tableau-bord',
  standalone: true,
  imports: [NgFor, NgIf, NgxChartsModule, RouterLink, FormsModule],
  templateUrl: './tableau-bord.component.html',
  styleUrls: ['./tableau-bord.component.css']
})
export class TableauBordComponent implements OnInit, AfterViewInit {
  filter = {
    userId: null as number | null,
    clientId: null as number | null,
    personnelId: null as number | null,
    projectId: null as number | null,
    start: null as string | null,
    end: null as string | null,
    granularity: 'daily' as 'daily' | 'weekly' | 'monthly' | 'yearly' | 'none'
  };

  users: User[] = [];            // Charger tous les utilisateurs si on veut filtrer
  userSeries: any[] = [];        // pour ngx-charts-line-chart
  statusSeries: { name: string; value: number }[] = [];

  LegendPosition = LegendPosition;
  currentUser: User | ClientDto | null = null;
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

  clientUsers: ClientDto[] = [];
  personnelUsers: User[] = [];

  responsibleTicketsCount: number = 0;
  projectMemberTicketsCount: number = 0;

  myTicketsCount: number = 0;

  projects: Projet[] = [];

  constructor(
    private ticketService: TicketService,
    private accountService: AccountService,
    private clientService: ClientService,
    private projetService: ProjetService,
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
      if (this.isClient()) {
        // 1) On force le filtre clientId sur l’ID du client connecté
        this.filter.clientId = (this.currentUser as ClientDto).id;
        // 2) On appelle directement updateProjectList() pour charger SES projets
        this.updateProjectList();
      }

      this.loadTicketCounts();
      this.loadDashboardCounts();
      this.dashboardService.getMyTicketsCount()
        .subscribe(count => this.myTicketsCount = count);
      this.loadClientUsers();
      this.loadPersonnelUsers();

      if (!this.isClient()) {
        this.updateProjectList();
      }
      this.applyFilters();
      forkJoin({
        asResponsible: this.dashboardService.getTicketsCountAsResponsible(this.currentUser.id),
        asProjectMember: this.dashboardService.getTicketsCountAsProjectMember(this.currentUser.id)
      }).subscribe({
        next: results => {
          this.responsibleTicketsCount = results.asResponsible;
          this.projectMemberTicketsCount = results.asProjectMember;
        },
        error: err => {
          console.error('Erreur lors du chargement des compteurs de tickets', err);
          // en cas d’erreur, on laisse 0 par défaut
          this.responsibleTicketsCount = 0;
          this.projectMemberTicketsCount = 0;
        },
        complete: () => {
          this.globalLoaderService.hideGlobalLoader();
        }
      });
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
        this.paysCount = data.paysCount;
        this.projectsCount = data.projectsCount;
        this.societesCount = data.societesCount;
        this.statutsCount = data.statutsCount;
        this.ticketsCount = data.ticketsCount;
        this.usersCount = data.usersCount;
        this.clientsCount = data.clientsCount;
        this.personnelCount = data.personnelCount;
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
    const chartWidth = Math.max(300, Math.min(500, width / 2 - 24));
    const chartHeight = 300; // <–– hauteur fixe plus petite
    this.view = [chartWidth, chartHeight];
  }

  private loadClientUsers() {
    this.clientService.getAll().subscribe(clients => this.clientUsers = clients);
  }

  private loadPersonnelUsers() {
    this.accountService.getAllUsers().subscribe(users => {
      // Exclure les clients
      this.personnelUsers = users.filter(u => u.role && u.role.toLowerCase() !== 'client');
    });
  }

  isClient(): boolean {
    return (this.currentUser as ClientDto)?.paysId !== undefined;
  }

  isUser(u: any): u is User {
    return u && (u as User).role !== undefined;
  }

  isSuperAdmin(): boolean { return this.isUser(this.currentUser) && this.currentUser.role.toLowerCase() === 'super admin'; }
  isChefDeProjet(): boolean { return this.isUser(this.currentUser) && this.currentUser.role.toLowerCase() === 'chef de projet'; }
  isCollaborateur(): boolean { return this.isUser(this.currentUser) && this.currentUser.role.toLowerCase() === 'collaborateur'; }

  applyFilters(): void {
    const currentUser = this.accountService.currentUser();
    if (!currentUser) return;

    // Préparer les filtres date et granularité
    const start = this.filter.start ?? undefined;
    const end = this.filter.end ?? undefined;
    const gran = this.filter.granularity;

    // Filtres client/personnel sélectionnés (pour non-CP)
    const clientId = this.filter.clientId ?? undefined;
    const personnelId = this.filter.personnelId ?? undefined;

    if (this.isChefDeProjet()) {
      // ─── COURBE “Mes tickets” ───
      const reqUser: TicketFilterRequest = {
        userId: currentUser.id,
        ownerId: undefined,
        personnelId: undefined,
        start: start,
        end: end,
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
        userId: undefined,
        ownerId: undefined,
        personnelId: currentUser.id,
        start: start,
        end: end,
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
              this.ticketCounts = [{ id: 0, name: 'Aucune donnée', value: 1 }];
            }
          },
          error: err => {
            console.error('Erreur getTicketsByStatus (CP)', err);
            this.statusSeries = [{ name: 'Erreur', value: 0 }];
            this.ticketCounts = [{ id: 0, name: 'Erreur', value: 0 }];
          }
        });

    } else {
      // ─── COURBE “Tous les tickets” ───
      const reqFilt: TicketFilterRequest = {
        userId: undefined,
        ownerId: clientId,
        personnelId: personnelId,
        projetId: this.filter.projectId ?? undefined,
        start: start,
        end: end,
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
        userId: undefined,
        ownerId: clientId,
        personnelId: personnelId,
        projetId: this.filter.projectId ?? undefined,
        start: start,
        end: end,
        granularity: 'none'
      };
      this.dashboardService.getTicketsByStatus(reqStatus)
        .subscribe({
          next: status => {
            if (status.length) {
            // 1) Séries bar en brut
          this.statusSeries = status.map(s => ({ name: s.key, value: s.count }));

          this.ticketCounts = status.map(s => ({
            id: 0,
            name: s.key,     
            value: s.count   
          }));
            } else {
              this.statusSeries = [{ name: 'Aucune donnée', value: 0 }];
              this.ticketCounts = [{ id: 0, name: 'Aucune donnée', value: 1 }];
            }
          },
          error: err => {
            console.error('Erreur getTicketsByStatus', err);
            this.statusSeries = [{ name: 'Erreur', value: 0 }];
            this.ticketCounts = [{ id: 0, name: 'Erreur', value: 0 }];
          }
        });
    }
  }

  public percentTooltip = (entry: any): string => {
    // ngx-charts vous passe un objet { data: { name, value }, label, value, ... }
    const name  = entry.data.name;
    const value = entry.data.value;
    const total = this.ticketCounts.reduce((sum, d) => sum + d.value, 0) || 1;
    const pct   = (value / total) * 100;
    return `${name} : ${pct.toFixed(1)} %`;
  };    
  
  openPaysModal() {
    const modalRef = this.overlayModalService.open(PaysModalComponent);
    // modalRef est ici l'instance de PaysModalComponent
    modalRef.added.subscribe(() => {
      // Recharge le compteur dès qu’un pays est ajouté dans le modal
      this.loadDashboardCounts();
    });
  }
  updateProjectList(): void {
    const currentUser = this.accountService.currentUser();
    if (!currentUser) return;

    // ── NOUVEAU : Si l’utilisateur connecté est un Client, on charge ses propres projets et on sort
    if (this.isClient()) {
      const clientId = (currentUser as ClientDto).id;
      this.clientService.getClientProjects(clientId)
        .subscribe(plist => {
          this.projects = plist ?? [];
        });
      return;
    }

    // ── CAS “classique” : l’utilisateur n’est pas client  
    const cId = this.filter.clientId;
    const pId = this.filter.personnelId;

    // 1) Aucun filtre → tous les projets
    if (cId == null && pId == null) {
      this.projetService.getProjets({}).subscribe(all => this.projects = all ?? []);
      return;
    }

    // 2) Seulement client (filtré depuis le select “Client” → superadmin)
    if (cId != null && pId == null) {
      this.clientService.getClientProjects(cId)
        .subscribe(plist => this.projects = plist ?? []);
      return;
    }

    // 3) Seulement personnel
    if (cId == null && pId != null) {
      this.accountService.getUserProjects(pId, 1, 100).pipe(
        map(pag => pag.items ?? [])
      ).subscribe(plist => this.projects = plist);
      return;
    }

    // 4) Les deux à la fois → intersection
    forkJoin({
      byClient: this.clientService.getClientProjects(cId!),
      byUser: this.accountService
        .getUserProjects(pId!, 1, 100)
        .pipe(map(pag => pag.items ?? []))
    }).subscribe(({ byClient, byUser }) => {
      const userProjectIds = new Set(byUser.map(p => p.id));
      this.projects = (byClient ?? []).filter(p => userProjectIds.has(p.id));
    });
  }
}