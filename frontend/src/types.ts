// Datu tipi atbilst aizmugursistēmas DTO (GroupMate.Api/Dtos/Dtos.cs).

export type Role = "Dalibnieks" | "Organizators" | "Administrators";

export interface Session {
  id: number;
  fullName: string;
  role: Role;
}

export interface DemoUser {
  id: number;
  fullName: string;
  email: string;
  role: Role;
  preferredRole: string | null;
}

export interface Skill {
  id: number;
  name: string;
  category: string;
}

export interface UserSkill {
  skillId: number;
  name: string;
  category: string;
  level: number;
}

export interface AvailabilitySlot {
  dayOfWeek: number;
  startHour: number;
  endHour: number;
}

export interface Profile {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: Role;
  preferredRole: string | null;
  interests: string[];
  skills: UserSkill[];
  availability: AvailabilitySlot[];
}

export interface RequiredSkill {
  skillId: number;
  name: string;
  minLevel: number;
}

export interface Project {
  id: number;
  title: string;
  description: string;
  teamSize: number;
  applicationDeadline: string;
  endDate: string;
  status: string;
  isOpenForApplications: boolean;
  canRunMatching: boolean;
  organizerName: string;
  requiredSkills: RequiredSkill[];
  requiredRoles: string[];
  applicantCount: number;
  myApplicationStatus: "Aktivs" | "Atsaukts" | null;
}

export interface Applicant {
  userId: number;
  fullName: string;
  preferredRole: string | null;
  profileComplete: boolean;
  skills: UserSkill[];
  appliedAt: string;
}

export interface MemberSkill {
  name: string;
  level: number;
}

export interface TeamMember {
  userId: number;
  fullName: string;
  role: string;
  email: string | null;
  skills: MemberSkill[];
}

export interface Team {
  id: number;
  name: string;
  score: number;
  skillScore: number;
  diversityScore: number;
  availabilityScore: number;
  roleScore: number;
  explanation: string[];
  isApproved: boolean;
  members: TeamMember[];
}

export interface Unassigned {
  userId: number;
  fullName: string;
  preferredRole: string | null;
  reason: string;
}

export interface MatchingView {
  projectId: number;
  projectTitle: string;
  teamSize: number;
  isApproved: boolean;
  teams: Team[];
  unassigned: Unassigned[];
  skillGapReport: string[];
}

export interface MyTeam {
  teamId: number;
  teamName: string;
  projectId: number;
  projectTitle: string;
  endDate: string;
  projectEnded: boolean;
  myRole: string;
  score: number;
  approvedAt: string | null;
  members: TeamMember[];
}

export interface Notification {
  id: number;
  title: string;
  message: string;
  isRead: boolean;
  emailStatus: string;
  createdAt: string;
}

export interface AdminUser {
  id: number;
  fullName: string;
  email: string;
  role: Role;
  isBlocked: boolean;
  createdAt: string;
}

export interface Stats {
  users: number;
  participants: number;
  organizers: number;
  projects: number;
  openProjects: number;
  approvedTeams: number;
  applications: number;
}
