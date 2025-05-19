export interface RegisterClientDto {
  email: string;
  firstName: string;
  lastName: string;
  numTelephone: string;
  pays: number;
  societeId: number;
  actif: boolean;
  projetIds: number[]; 
}
