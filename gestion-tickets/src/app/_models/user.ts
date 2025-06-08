import { ContratUser } from "./contrat-user";
import { ProjetMember } from "./projet-member"; // Assurez-vous du bon chemin
import { Societe } from "./societe";

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  numTelephone: string;
  pays: string;
  email: string;
  role: string;
  actif: boolean;
  contratUser?: ContratUser;
  dateDebut?: Date;
  dateFin?: Date;
  selected?: boolean;
  token: string;
  societeId?: number;
  societe?: Societe;
  projetMembers?: ProjetMember[]; 
  createdAt?: Date;
  photoUrl?: string;
  ticketsTraites?: number;
  ticketsEnCours?: number;
}
