// ============================================================
//  XENOH API — TypeScript Types
//  Base URL (local): https://localhost:7017
//  Base URL (prod):  https://api.xenoh.com  ← cập nhật sau
// ============================================================

// ─────────────────────────────────────────
//  ENUMS
// ─────────────────────────────────────────

/** Role phải truyền khi Register */
export type UserRole = 'Individual' | 'Coach' | 'Admin';

/** Nhóm cơ — dùng để filter ExerciseTemplate và hiển thị */
export type MuscleGroup =
  | 'Chest'
  | 'Back'
  | 'Shoulders'
  | 'Biceps'
  | 'Triceps'
  | 'Quadriceps'
  | 'Hamstrings'
  | 'Glutes'
  | 'Calves'
  | 'Core'
  | 'Forearms'
  | 'FullBody';

/** Trạng thái quan hệ Coach-Client */
export type RelationshipStatus = 'Pending' | 'Accepted';

// ─────────────────────────────────────────
//  AUTH
// ─────────────────────────────────────────

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  /** Tối thiểu 6 ký tự */
  password: string;
  /** 'Individual' | 'Coach' | 'Admin' */
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

/** Trả về từ Register / Login / RefreshToken */
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  email: string;
  fullName: string;
  /** Ví dụ: ["Individual"] */
  roles: string[];
}

// ─────────────────────────────────────────
//  PLAN
// ─────────────────────────────────────────

export interface CreatePlanRequest {
  /** 2–100 ký tự */
  name: string;
  /** Format: "YYYY-MM-DD" */
  startDate: string;
  /** Format: "YYYY-MM-DD" — phải sau startDate */
  endDate: string;
}

/**
 * Response dành riêng cho Coach overview — bao gồm tên + email của owner (client hoặc chính coach).
 * Dùng cho GET /api/plans/coach-overview
 */
export interface CoachPlanResponse {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  /** "Self" = plan cá nhân của coach | "Coach" = plan tạo cho client */
  planType: 'Self' | 'Coach';
  ownerId: string;
  /** Tên chủ plan: tên client nếu planType=Coach, tên coach nếu planType=Self */
  ownerName: string;
  ownerEmail: string;
  totalWeeks: number;
  createdAt: string;
}

/** Coach tạo plan cho client (chỉ role Coach) */
export interface CreatePlanForUserRequest extends CreatePlanRequest {
  /** ID của client (phải đang có quan hệ Active với coach) */
  userId: string;
}

export interface PlanResponse {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  /** 'Self' = user tự tạo | 'Coach' = coach tạo cho client */
  planType: 'Self' | 'Coach';
  ownerId: string;
  /** Tên client/user sở hữu plan */
  ownerName: string;
  /** null nếu planType = 'Self' */
  createdByCoachId: string | null;
  /** Tên coach tạo plan — null nếu planType = 'Self' */
  coachName: string | null;
  totalWeeks: number;
  /** Tổng số ngày tập trong toàn bộ plan */
  totalDays: number;
  /** Số ngày đã hoàn thành (isCompleted = true) */
  completedDays: number;
  createdAt: string;
}

export interface UpdatePlanRequest {
  /** 2–100 ký tự */
  name: string;
  /** Format: "YYYY-MM-DD" */
  startDate: string;
  /**
   * Format: "YYYY-MM-DD" — phải sau startDate.
   *
   * ⚠️  Nếu thay đổi startDate hoặc endDate:
   *   - Server sẽ XÓA toàn bộ weeks/days/exercises cũ và tạo lại.
   *   - Chỉ được phép nếu chưa có ngày nào `isCompleted = true`.
   *   - Nếu đã có progress → 400 "Cannot change dates: plan already has completed days."
   */
  endDate: string;
}

// ─────────────────────────────────────────
//  WEEKLY WORKOUT
// ─────────────────────────────────────────

export interface WeeklyWorkoutResponse {
  id: string;
  weekNumber: number;
  /**
   * Tên tuần — có thể đổi qua PATCH.
   * Default khi tạo: "Tuần 1 (21/04 - 27/04)"
   */
  name: string;
  /** Format: "YYYY-MM-DD" */
  startDate: string;
  /** Format: "YYYY-MM-DD" */
  endDate: string;
  planId: string;
  totalDays: number;
  completedDays: number;
}

export interface UpdateWeeklyWorkoutRequest {
  weeklyWorkoutId: string;
  /** 1–100 ký tự */
  name: string;
}

// ─────────────────────────────────────────
//  DAILY WORKOUT
// ─────────────────────────────────────────

export interface DailyWorkoutResponse {
  id: string;
  /** Format: "YYYY-MM-DD" */
  date: string;
  /** "Monday" | "Tuesday" | ... */
  dayOfWeek: string;
  isCompleted: boolean;
  weeklyWorkoutId: string;
  totalExercises: number;
  completedExercises: number;
}

// ─────────────────────────────────────────
//  EXERCISE TEMPLATE  (thư viện bài tập seed sẵn)
// ─────────────────────────────────────────

export interface ExerciseTemplateResponse {
  id: string;
  name: string;
  description: string | null;
  primaryMuscleGroup: MuscleGroup;
  secondaryMuscleGroups: MuscleGroup[];
}

// ─────────────────────────────────────────
//  EXERCISE  (bài tập trong một ngày tập)
// ─────────────────────────────────────────

export interface CreateExerciseRequest {
  dailyWorkoutId: string;
  exerciseTemplateId: string;
  /** Số set kế hoạch — server sẽ tự tạo N set tương ứng */
  plannedSets: number;
  plannedReps: number;
  plannedWeight?: number;
  notes?: string;
}

export interface UpdateExerciseRequest {
  exerciseId: string;
  /**
   * Nếu thay đổi plannedSets:
   * - Không được thay đổi nếu đã có set done
   * - Server sẽ xóa toàn bộ sets cũ và tạo lại
   */
  plannedSets?: number;
  plannedReps?: number;
  plannedWeight?: number;
  notes?: string;
}

/** Một set cụ thể bên trong Exercise */
export interface ExerciseSetResponse {
  id: string;
  setNumber: number;
  plannedReps: number;
  plannedWeight: number | null;
  /** Ghi đè nếu user tập khác kế hoạch */
  actualReps: number | null;
  actualWeight: number | null;
  isCompleted: boolean;
  /** ISO string, null nếu chưa done */
  completedAt: string | null;
}

export interface MarkSetCompleteRequest {
  setId: string;
  /** Optional — ghi đè plannedReps */
  actualReps?: number;
  /** Optional — ghi đè plannedWeight */
  actualWeight?: number;
}

/** Response đầy đủ của một Exercise (bao gồm toàn bộ sets) */
export interface ExerciseResponse {
  id: string;
  exerciseTemplateId: string;
  name: string;
  primaryMuscleGroup: MuscleGroup;
  secondaryMuscleGroups: MuscleGroup[];
  plannedSets: number;
  plannedReps: number;
  plannedWeight: number | null;
  /** Số set đã done (derived từ sets[].isCompleted) */
  completedSetsCount: number;
  /** true khi tất cả sets đều isCompleted */
  isCompleted: boolean;
  notes: string | null;
  dailyWorkoutId: string;
  /** Luôn trả về đầy đủ, sort theo setNumber */
  sets: ExerciseSetResponse[];
}

// ─────────────────────────────────────────
//  COACH - CLIENT
// ─────────────────────────────────────────

// ─────────────────────────────────────────
//  COACHES  (tìm kiếm coach để kết nối)
// ─────────────────────────────────────────

export interface CoachResponse {
  id: string;
  fullName: string;
  email: string;
}

/** Response trả về từ GET /api/coach-client/my-clients (chỉ Coach) */
export interface ClientResponse {
  relationshipId: string;
  clientId: string;
  fullName: string;
  email: string;
  /** 'Pending' | 'Accepted' */
  status: RelationshipStatus;
  connectedAt: string;
}

export interface RequestCoachRequest {
  /** ID của coach muốn kết nối */
  coachId: string;
}

export interface CoachRelationshipResponse {
  id: string;
  clientId: string;
  clientName: string;
  coachId: string;
  coachName: string;
  /** 'Pending' → chờ coach chấp nhận | 'Accepted' → đang hoạt động */
  status: RelationshipStatus;
  createdAt: string;
}

// ─────────────────────────────────────────
//  ERROR RESPONSE  (cấu trúc lỗi chung từ BE)
// ─────────────────────────────────────────

export interface ApiErrorResponse {
  message: string;
}
