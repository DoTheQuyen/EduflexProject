export interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role?: string;
  createdAt?: Date;
  lastLogin?: Date;
  // Add any other properties that AuthService expects
}

export interface LoginRequest {
  email: string;
  password: string;
}

