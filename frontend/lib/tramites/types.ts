export type TramiteIndexItem = {
  id: string;
  reference: string;
  procedureTypeName: string;
  procedureTypeCode: string;
  otDisplayName: string;
  otDivipolCode: string;
  vehicleQueryValue: string;
  status: string;
  createdAt: string;
  createdBy: string;
};

export type TramitesIndexResponse = {
  items: TramiteIndexItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type TramitesIndexSearchParams = {
  page?: string;
  pageSize?: string;
};
