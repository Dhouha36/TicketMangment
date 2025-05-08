export interface ClientCreate {
  firstName:   string;
  lastName:    string;
  email:       string;
  pays:        number;
  numTelephone: string;
  role:        'Client';
  societeId:   number;
}