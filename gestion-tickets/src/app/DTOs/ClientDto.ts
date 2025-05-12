export interface ClientDto {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  numTelephone: string;
  actif: boolean;
  pays: number;
  paysNavigation: {
    id: number;
    nom: string;
  };
  societeId: number;
  societe: {
    id: number;
    nom: string;
  };
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
  token?: string;
}