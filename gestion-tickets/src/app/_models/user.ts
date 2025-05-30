import { Contrat } from "./contrat";
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
  contrat?: Contrat;
  dateDebut?: Date;
  dateFin?: Date;
  selected?: boolean;
  token: string;
  societeId?: number;
  societe?: Societe;
  projetMembers?: ProjetMember[]; 
  createdAt?: Date;
  photoUrl?: string;
}
