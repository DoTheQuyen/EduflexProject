export interface Department {
  id: string;
  name: string;
  description?: string;
  parentDepartmentId?: string;
  headUserId?: string;
  memberUserIds: string[];
  createdAt: string;
}

export interface CreateDepartmentRequest {
  name: string;
  description?: string;
  parentDepartmentId?: string;
  headUserId?: string;
  memberUserIds: string[];
}

export interface DepartmentFilter {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
}

export interface DepartmentBadge {
  id: string;
  name: string;
  isHead: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}
