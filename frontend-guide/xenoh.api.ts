// ============================================================
//  XENOH API Client
//  Dependency: axios  →  npm install axios
//
//  Setup trong React:
//    import { xenohApi, authStorage } from './xenoh.api';
// ============================================================

import axios, {
  AxiosInstance,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios';

import type {
  RegisterRequest,
  LoginRequest,
  RefreshTokenRequest,
  AuthResponse,
  CreatePlanRequest,
  CreatePlanForUserRequest,
  UpdatePlanRequest,
  PlanResponse,
  CoachPlanResponse,
  WeeklyWorkoutResponse,
  UpdateWeeklyWorkoutRequest,
  DailyWorkoutResponse,
  ExerciseTemplateResponse,
  CreateExerciseRequest,
  UpdateExerciseRequest,
  MarkSetCompleteRequest,
  ExerciseResponse,
  CoachResponse,
  ClientResponse,
  RequestCoachRequest,
  CoachRelationshipResponse,
  MuscleGroup,
} from './xenoh.types';

// ─────────────────────────────────────────
//  CONFIG
// ─────────────────────────────────────────

const BASE_URL = 'https://localhost:7017'; // đổi sang prod URL khi deploy

// ─────────────────────────────────────────
//  TOKEN STORAGE  (thay bằng secure storage nếu cần)
// ─────────────────────────────────────────

export const authStorage = {
  getAccessToken: () => localStorage.getItem('xenoh_access_token'),
  getRefreshToken: () => localStorage.getItem('xenoh_refresh_token'),

  save: (auth: AuthResponse) => {
    localStorage.setItem('xenoh_access_token', auth.accessToken);
    localStorage.setItem('xenoh_refresh_token', auth.refreshToken);
  },

  clear: () => {
    localStorage.removeItem('xenoh_access_token');
    localStorage.removeItem('xenoh_refresh_token');
  },
};

// ─────────────────────────────────────────
//  AXIOS INSTANCE + INTERCEPTORS
// ─────────────────────────────────────────

const http: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

// Tự động đính kèm Bearer token vào mọi request
http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = authStorage.getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Auto-refresh khi nhận 401
let isRefreshing = false;
let pendingQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

const processPendingQueue = (error: unknown, token: string | null) => {
  pendingQueue.forEach(({ resolve, reject }) =>
    error ? reject(error) : resolve(token!)
  );
  pendingQueue = [];
};

http.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          pendingQueue.push({ resolve, reject });
        }).then((token) => {
          originalRequest.headers = {
            ...originalRequest.headers,
            Authorization: `Bearer ${token}`,
          };
          return http(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const refreshToken = authStorage.getRefreshToken();
        if (!refreshToken) throw new Error('No refresh token');

        const { data } = await http.post<AuthResponse>('/api/auth/refresh-token', {
          refreshToken,
        });

        authStorage.save(data);
        processPendingQueue(null, data.accessToken);

        originalRequest.headers = {
          ...originalRequest.headers,
          Authorization: `Bearer ${data.accessToken}`,
        };
        return http(originalRequest);
      } catch (refreshError) {
        processPendingQueue(refreshError, null);
        authStorage.clear();
        // Redirect về login — tuỳ routing của project
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

// ─────────────────────────────────────────
//  API METHODS
// ─────────────────────────────────────────

export const xenohApi = {

  // ──────────────────────────────────────
  //  AUTH
  // ──────────────────────────────────────

  /**
   * FLOW đăng ký:
   *   POST /api/auth/register
   *   → nhận AuthResponse → lưu token → redirect dashboard
   *
   * Role hợp lệ: 'Individual' | 'Coach'
   * (Admin không tạo qua API public)
   */
  async register(data: RegisterRequest): Promise<AuthResponse> {
    const res = await http.post<AuthResponse>('/api/auth/register', data);
    authStorage.save(res.data);
    return res.data;
  },

  /**
   * FLOW đăng nhập:
   *   POST /api/auth/login
   *   → nhận AuthResponse → lưu token → redirect dashboard
   */
  async login(data: LoginRequest): Promise<AuthResponse> {
    const res = await http.post<AuthResponse>('/api/auth/login', data);
    authStorage.save(res.data);
    return res.data;
  },

  /**
   * Làm mới token (tự động gọi khi 401 — thường không cần gọi thủ công).
   * Nếu refreshToken hết hạn → interceptor sẽ redirect /login.
   */
  async refreshToken(data: RefreshTokenRequest): Promise<AuthResponse> {
    const res = await http.post<AuthResponse>('/api/auth/refresh-token', data);
    authStorage.save(res.data);
    return res.data;
  },

  /**
   * FLOW đăng xuất:
   *   POST /api/auth/logout  (cần Bearer token)
   *   → server thu hồi refreshToken → xóa token local
   */
  async logout(): Promise<void> {
    await http.post('/api/auth/logout');
    authStorage.clear();
  },

  // ──────────────────────────────────────
  //  EXERCISE TEMPLATE  (thư viện bài tập)
  // ──────────────────────────────────────

  /**
   * Lấy toàn bộ exercise templates (47 bài tập được seed sẵn).
   *
   * @param muscleGroup  Optional — filter theo nhóm cơ.
   *   Ví dụ: getExerciseTemplates('Chest')
   *
   * Các giá trị hợp lệ: Chest | Back | Shoulders | Biceps | Triceps |
   *   Quadriceps | Hamstrings | Glutes | Calves | Core | Forearms | FullBody
   */
  async getExerciseTemplates(muscleGroup?: MuscleGroup): Promise<ExerciseTemplateResponse[]> {
    const params = muscleGroup ? { muscleGroup } : {};
    const res = await http.get<ExerciseTemplateResponse[]>('/api/exercise-templates', { params });
    return res.data;
  },

  // ──────────────────────────────────────
  //  PLAN
  // ──────────────────────────────────────

  /**
   * Lấy tất cả plans của user hiện tại (tối đa 3 plans).
   */
  async getMyPlans(): Promise<PlanResponse[]> {
    const res = await http.get<PlanResponse[]>('/api/plans');
    return res.data;
  },

  /**
   * Lấy chi tiết một plan.
   */
  async getPlan(planId: string): Promise<PlanResponse> {
    const res = await http.get<PlanResponse>(`/api/plans/${planId}`);
    return res.data;
  },

  /**
   * FLOW tạo plan:
   *   1. User chọn tên, startDate, endDate
   *   2. POST /api/plans
   *   3. Server tự động tạo WeeklyWorkout + DailyWorkout (7 ngày/tuần)
   *   4. Frontend chuyển vào màn hình plan detail
   *
   * Giới hạn: tối đa 3 plans/user (tổng cả personal + coach-created).
   * startDate phải trước endDate.
   */
  async createPlan(data: CreatePlanRequest): Promise<PlanResponse> {
    const res = await http.post<PlanResponse>('/api/plans', data);
    return res.data;
  },

  /**
   * [Chỉ Coach] Dashboard tổng quan — tất cả plans liên quan đến coach:
   *   GET /api/plans/coach-overview
   *
   * Bao gồm:
   *   - Plans cá nhân của coach (planType = "Self")
   *   - Plans coach đã tạo cho clients (planType = "Coach")
   *
   * Mỗi item có ownerName + ownerEmail để phân biệt plan thuộc về ai.
   * Sort theo createdAt mới nhất trước.
   *
   * @example
   *   const personal   = plans.filter(p => p.planType === 'Self');
   *   const forClients = plans.filter(p => p.planType === 'Coach');
   */
  async getCoachPlans(): Promise<CoachPlanResponse[]> {
    const res = await http.get<CoachPlanResponse[]>('/api/plans/coach-overview');
    return res.data;
  },

  /**
   * [Chỉ Coach] Tạo plan cho client đang có quan hệ Active.
   *   POST /api/plans/for-user
   */
  async createPlanForUser(data: CreatePlanForUserRequest): Promise<PlanResponse> {
    const res = await http.post<PlanResponse>('/api/plans/for-user', data);
    return res.data;
  },

  /**
   * Cập nhật thông tin plan (tên và/hoặc ngày).
   *   PUT /api/plans/{planId}
   *
   * ─── Quyền ───────────────────────────────────────────────────
   *   planType = 'Self'  → chỉ owner (user) được sửa
   *   planType = 'Coach' → chỉ coach tạo plan được sửa
   *   (Client KHÔNG được sửa plan coach tạo cho mình)
   *
   * ─── Đổi tên (name) ──────────────────────────────────────────
   *   Luôn cho phép. Không ảnh hưởng weeks/days.
   *
   * ─── Đổi ngày (startDate / endDate) ─────────────────────────
   *   Chỉ cho phép khi chưa có ngày nào completedDays > 0.
   *   Khi đổi:
   *     → Server XÓA toàn bộ weeks / days / exercises cũ
   *     → Tạo lại structure mới theo ngày mới
   *     → completedDays reset về 0
   *
   * ─── Lỗi thường gặp ──────────────────────────────────────────
   *   400 "Cannot change dates: plan already has completed days."
   *      → plan đã có buổi tập completed, không thể đổi ngày
   *   400 "End date must be after start date."
   *   400 "You do not have permission to update this plan."
   *
   * @example — chỉ đổi tên (an toàn mọi lúc)
   *   await xenohApi.updatePlan(planId, {
   *     name: 'Kế hoạch mùa hè',
   *     startDate: plan.startDate,  // giữ nguyên
   *     endDate:   plan.endDate,    // giữ nguyên
   *   });
   *
   * @example — đổi ngày (cần kiểm tra trước)
   *   if (plan.completedDays > 0) {
   *     alert('Không thể đổi ngày khi đã có buổi tập hoàn thành');
   *     return;
   *   }
   *   await xenohApi.updatePlan(planId, { name, startDate, endDate });
   */
  async updatePlan(planId: string, data: UpdatePlanRequest): Promise<PlanResponse> {
    const res = await http.put<PlanResponse>(`/api/plans/${planId}`, data);
    return res.data;
  },

  /**
   * Xóa plan (chỉ owner hoặc coach tạo plan đó).
   */
  async deletePlan(planId: string): Promise<void> {
    await http.delete(`/api/plans/${planId}`);
  },

  // ──────────────────────────────────────
  //  WEEKLY WORKOUT
  // ──────────────────────────────────────

  /**
   * Lấy danh sách tuần của một plan.
   * Trả về mảng sort theo weekNumber.
   *
   * Mỗi tuần có `name` (mặc định: "Tuần 1 (21/04 - 27/04)") và startDate/endDate.
   *
   * FLOW hiển thị:
   *   getWeeks(planId) → render danh sách tuần → user chọn tuần → getDays(weekId)
   */
  async getWeeks(planId: string): Promise<WeeklyWorkoutResponse[]> {
    const res = await http.get<WeeklyWorkoutResponse[]>(`/api/plans/${planId}/weeks`);
    return res.data;
  },

  /**
   * Đổi tên một tuần tập.
   *   PATCH /api/plans/{planId}/weeks/{weeklyWorkoutId}
   *
   * Tên mặc định khi tạo: "Tuần 1 (21/04 - 27/04)"
   * Response trả về WeeklyWorkoutResponse đã cập nhật (kèm startDate/endDate).
   *
   * @example
   *   await xenohApi.updateWeekName(planId, { weeklyWorkoutId: id, name: 'Ngực & Lưng' });
   */
  async updateWeekName(planId: string, data: UpdateWeeklyWorkoutRequest): Promise<WeeklyWorkoutResponse> {
    const res = await http.patch<WeeklyWorkoutResponse>(
      `/api/plans/${planId}/weeks/${data.weeklyWorkoutId}`,
      data
    );
    return res.data;
  },

  // ──────────────────────────────────────
  //  DAILY WORKOUT
  // ──────────────────────────────────────

  /**
   * Lấy danh sách ngày trong một tuần (luôn có đúng 7 ngày).
   * Mỗi ngày có isCompleted, totalExercises, completedExercises.
   *
   * FLOW hiển thị:
   *   getDays(weekId) → render 7 ngày (Mon-Sun)
   *   → user chọn ngày → getExercisesByDay(dayId)
   */
  async getDays(weekId: string): Promise<DailyWorkoutResponse[]> {
    const res = await http.get<DailyWorkoutResponse[]>(`/api/weeks/${weekId}/days`);
    return res.data;
  },

  // ──────────────────────────────────────
  //  EXERCISE
  // ──────────────────────────────────────

  /**
   * Lấy tất cả exercises trong một ngày tập (kèm toàn bộ sets).
   */
  async getExercisesByDay(dailyWorkoutId: string): Promise<ExerciseResponse[]> {
    const res = await http.get<ExerciseResponse[]>(`/api/exercises/by-day/${dailyWorkoutId}`);
    return res.data;
  },

  /**
   * FLOW tạo exercise:
   *   1. Hiển thị danh sách ExerciseTemplate (có thể filter theo nhóm cơ)
   *   2. User chọn template + nhập plannedSets, plannedReps, plannedWeight?
   *   3. POST /api/exercises
   *   4. Server tự động tạo N ExerciseSet (N = plannedSets)
   *   5. Response trả về Exercise kèm sets[] đầy đủ
   *
   * Sau khi tạo, render từng set trong sets[] để user mark done.
   */
  async createExercise(data: CreateExerciseRequest): Promise<ExerciseResponse> {
    const res = await http.post<ExerciseResponse>('/api/exercises', data);
    return res.data;
  },

  /**
   * Cập nhật thông tin exercise (tên/reps/weight).
   *
   * LƯU Ý về plannedSets:
   *   - Không thể đổi nếu đã có set nào isCompleted = true
   *   - Nếu đổi và chưa có set done: server xóa sets cũ, tạo lại
   */
  async updateExercise(data: UpdateExerciseRequest): Promise<ExerciseResponse> {
    const res = await http.put<ExerciseResponse>(`/api/exercises/${data.exerciseId}`, data);
    return res.data;
  },

  /**
   * FLOW mark done từng set:
   *   PATCH /api/exercises/sets/{setId}/complete
   *
   *   - Gọi lần lượt cho từng set khi user tap "Done"
   *   - actualReps/actualWeight là optional (ghi đè nếu thực tế khác kế hoạch)
   *   - Response trả về Exercise đã cập nhật (với completedSetsCount mới)
   *
   * AUTO-COMPLETE CASCADE:
   *   Khi set cuối cùng done:
   *     → Exercise.isCompleted = true
   *     → Nếu tất cả exercises trong ngày đều done:
   *         DailyWorkout.isCompleted = true
   *
   * ERROR:
   *   Set đã done → 400 "Set is already completed."
   */
  async markSetComplete(data: MarkSetCompleteRequest): Promise<ExerciseResponse> {
    const res = await http.patch<ExerciseResponse>(
      `/api/exercises/sets/${data.setId}/complete`,
      data
    );
    return res.data;
  },

  /**
   * Xóa exercise (kéo theo tất cả sets của nó).
   */
  async deleteExercise(exerciseId: string): Promise<void> {
    await http.delete(`/api/exercises/${exerciseId}`);
  },

  // ──────────────────────────────────────
  //  COACHES  (tìm kiếm)
  // ──────────────────────────────────────

  /**
   * Lấy danh sách tất cả coach, có thể tìm theo tên.
   *   GET /api/coaches
   *   GET /api/coaches?name=binh
   *
   * Tìm kiếm hỗ trợ:
   *   - Không phân biệt hoa thường
   *   - Không phân biệt dấu tiếng Việt: "binh" khớp "Bình", "nguyen" khớp "Nguyễn"
   *   - Khớp first name, last name, hoặc full name
   *
   * FLOW kết nối coach:
   *   1. getCoaches() → hiển thị danh sách
   *   2. getCoaches('binh') → tìm kiếm realtime
   *   3. User chọn coach → requestCoach({ coachId: coach.id })
   */
  async getCoaches(name?: string): Promise<CoachResponse[]> {
    const params = name ? { name } : {};
    const res = await http.get<CoachResponse[]>('/api/coaches', { params });
    return res.data;
  },

  // ──────────────────────────────────────
  //  COACH - CLIENT
  // ──────────────────────────────────────

  /**
   * [Chỉ Individual] Gửi yêu cầu kết nối với một Coach.
   *
   * Ràng buộc:
   *   - Chỉ có 1 coach tại 1 thời điểm
   *   - Nếu đang có quan hệ Active → báo lỗi
   */
  async requestCoach(data: RequestCoachRequest): Promise<CoachRelationshipResponse> {
    const res = await http.post<CoachRelationshipResponse>('/api/coach-client/request', data);
    return res.data;
  },

  /**
   * [Chỉ Coach] Chấp nhận yêu cầu từ client.
   *   PUT /api/coach-client/accept/{relationshipId}
   */
  async acceptRequest(relationshipId: string): Promise<CoachRelationshipResponse> {
    const res = await http.put<CoachRelationshipResponse>(
      `/api/coach-client/accept/${relationshipId}`
    );
    return res.data;
  },

  /**
   * Hủy quan hệ Coach-Client (cả hai phía đều có thể gọi).
   *
   * Hệ quả khi hủy:
   *   - Toàn bộ plans do coach tạo cho client bị XÓA tự động
   *   - Không có thông báo trước
   */
  async terminateRelationship(relationshipId: string): Promise<void> {
    await http.delete(`/api/coach-client/${relationshipId}`);
  },

  /**
   * [Chỉ Coach] Trả về tất cả clients của coach hiện tại (Pending + Accepted).
   *   GET /api/coach-client/my-clients
   *
   * Dùng để render danh sách clients trong dashboard của coach.
   * Sort theo thời gian kết nối mới nhất trước.
   *
   * @example filter Active clients:
   *   const active = clients.filter(c => c.status === 'Accepted');
   */
  async getMyClients(): Promise<ClientResponse[]> {
    const res = await http.get<ClientResponse[]>('/api/coach-client/my-clients');
    return res.data;
  },

  /**
   * [Chỉ Coach] Lấy danh sách yêu cầu đang chờ chấp nhận.
   */
  async getPendingRequests(): Promise<CoachRelationshipResponse[]> {
    const res = await http.get<CoachRelationshipResponse[]>('/api/coach-client/pending-requests');
    return res.data;
  },

  /**
   * [Chỉ Individual] Lấy thông tin coach hiện tại.
   * Trả về null nếu chưa có coach.
   */
  async getMyCoach(): Promise<CoachRelationshipResponse | null> {
    try {
      const res = await http.get<CoachRelationshipResponse>('/api/coach-client/my-coach');
      return res.data;
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response?.status === 404) return null;
      throw err;
    }
  },
};
