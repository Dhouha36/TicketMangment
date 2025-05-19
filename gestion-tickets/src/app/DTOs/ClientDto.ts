import { ProjetMini } from "./ProjetMini";

export interface ClientDto {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  numTelephone: string;
  actif: boolean;
  initialPassword: string;
  paysId: number;
  pays: string; 
  societeId: number;
  societe: {
    id: number;
    nom: string;
  };
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
  token?: string;
  projets: ProjetMini[];
}