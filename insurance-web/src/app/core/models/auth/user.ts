import { UserRole } from "../enums";

export interface User {
  userId: number;
  fullName: string;
  email: string;
  role: UserRole;
}
