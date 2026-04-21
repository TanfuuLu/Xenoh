# Xenoh API — Frontend Flow Guide

Base URL (local): `https://localhost:7017`  
API Docs (Scalar): `https://localhost:7017/scalar/v1`

---

## Mục lục

1. [Auth Flow](#1-auth-flow)
2. [Workout Flow (Individual)](#2-workout-flow-individual)
3. [Exercise Flow trong một ngày](#3-exercise-flow-trong-một-ngày)
4. [Set Tracking Flow](#4-set-tracking-flow)
5. [Coach–Client Flow](#5-coachclient-flow)
6. [Business Rules cần biết](#6-business-rules-cần-biết)
7. [Error Handling Pattern](#7-error-handling-pattern)
8. [Data Hierarchy](#8-data-hierarchy)

---

## 1. Auth Flow

```
┌──────────────────────────────────────────────────────────────┐
│  ĐĂNG KÝ                                                     │
│                                                              │
│  POST /api/auth/register                                     │
│  { firstName, lastName, email, password, role }              │
│            ↓                                                 │
│       AuthResponse                                           │
│  { accessToken, refreshToken, email, fullName, roles }       │
│            ↓                                                 │
│  Lưu vào localStorage → redirect Dashboard                   │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  ĐĂNG NHẬP                                                   │
│                                                              │
│  POST /api/auth/login                                        │
│  { email, password }                                         │
│            ↓                                                 │
│       AuthResponse                                           │
│            ↓                                                 │
│  Lưu token → redirect Dashboard                              │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  TOKEN LIFECYCLE                                             │
│                                                              │
│  accessToken:  60 phút                                       │
│  refreshToken: dài hạn (cho đến khi logout)                  │
│                                                              │
│  Request → 401 → auto POST /api/auth/refresh-token           │
│                       { refreshToken }                       │
│                            ↓                                 │
│                  accessToken mới → retry request             │
│                                                              │
│  refresh-token hết hạn → clear storage → redirect /login    │
└──────────────────────────────────────────────────────────────┘
```

**Token trong header:**
```
Authorization: Bearer {accessToken}
```

---

## 2. Workout Flow (Individual)

```
GET /api/plans
    ↓ Danh sách plans (tối đa 3)
    ↓
Chọn plan (hoặc tạo mới)
    ↓
POST /api/plans
{ name, startDate, endDate }
    ↓ Server tự sinh WeeklyWorkout + DailyWorkout
    ↓
GET /api/plans/{planId}/weeks
    ↓ Danh sách tuần [Week 1, Week 2, ...]
    ↓
Chọn tuần
    ↓
GET /api/weeks/{weekId}/days
    ↓ 7 ngày [Mon, Tue, ..., Sun]
    ↓
Chọn ngày
    ↓
GET /api/exercises/by-day/{dayId}
    ↓ Danh sách exercises trong ngày
```

**Plan tự động generate:**
- Plan 2026-04-21 → 2026-05-18 (4 tuần)
- Mỗi tuần có đúng 7 DailyWorkout
- Ngày đầu tiên là ngày trong `startDate`

---

## 3. Exercise Flow trong một ngày

```
Màn hình chi tiết ngày
    ↓
GET /api/exercise-templates?muscleGroup=Chest   ← optional filter
    ↓ 47 templates phân loại theo nhóm cơ
    ↓
User chọn template + nhập sets/reps/weight
    ↓
POST /api/exercises
{ dailyWorkoutId, exerciseTemplateId, plannedSets: 3, plannedReps: 10, plannedWeight: 60 }
    ↓
ExerciseResponse {
    id, name,
    plannedSets: 3,
    completedSetsCount: 0,
    isCompleted: false,
    sets: [
        { id, setNumber: 1, plannedReps: 10, isCompleted: false },
        { id, setNumber: 2, plannedReps: 10, isCompleted: false },
        { id, setNumber: 3, plannedReps: 10, isCompleted: false },
    ]
}
    ↓
Render từng set → user tap Done trên từng set
```

**Nhóm cơ có thể filter:**
Chest · Back · Shoulders · Biceps · Triceps · Quadriceps · Hamstrings · Glutes · Calves · Core · Forearms · FullBody

---

## 4. Set Tracking Flow

```
Màn hình tập một exercise

  Exercise: Bench Press  [0/3 sets done]
  ├─ Set 1: 10 reps × 60kg  [  Done  ]  ←── user tap
  ├─ Set 2: 10 reps × 60kg  [  Done  ]
  └─ Set 3: 10 reps × 60kg  [  Done  ]

PATCH /api/exercises/sets/{setId}/complete
{ setId, actualReps?: 9, actualWeight?: 62.5 }

Response: ExerciseResponse đầy đủ (cập nhật completedSetsCount)
```

**Cascade auto-complete:**
```
Set 1 done → completedSetsCount: 1 | Exercise.isCompleted: false
Set 2 done → completedSetsCount: 2 | Exercise.isCompleted: false
Set 3 done → completedSetsCount: 3 | Exercise.isCompleted: true  ✅
                                   → Nếu tất cả exercise trong ngày done:
                                     DailyWorkout.isCompleted: true  ✅
```

**actualReps / actualWeight:**
- Để trống → giữ nguyên planned
- Nhập → lưu lại thực tế (ví dụ: kế hoạch 10 reps nhưng chỉ làm được 8)

**Không thể undo set đã done** (cần UX xác nhận trước khi tap Done).  
Nếu muốn reset: `PUT /api/exercises/{id}` với `plannedSets` mới (chỉ khi chưa có set nào done).

---

## 5. Coach–Client Flow

```
┌─────────── PHÍA CLIENT (Individual) ───────────┐

1. Tìm coach theo email/id (tính năng search chưa có → hardcode ID)

2. POST /api/coach-client/request
   { coachId: "..." }
   → Status: "Pending"

3. Chờ coach chấp nhận

4. Sau khi Active → coach có thể tạo plan:
   POST /api/plans/for-user
   { userId, name, startDate, endDate }

5. Hủy quan hệ:
   DELETE /api/coach-client/{relationshipId}
   ⚠️  Toàn bộ plans do coach tạo sẽ bị XÓA ngay lập tức

└─────────────────────────────────────────────────┘

┌─────────── PHÍA COACH ──────────────────────────┐

1. Xem yêu cầu chờ:
   GET /api/coach-client/pending-requests

2. Chấp nhận:
   PUT /api/coach-client/accept/{relationshipId}
   → Status: "Active"

3. Tạo plan cho client:
   POST /api/plans/for-user
   { userId: clientId, name, startDate, endDate }

4. Hủy quan hệ (giống phía client):
   DELETE /api/coach-client/{relationshipId}

└─────────────────────────────────────────────────┘
```

---

## 6. Business Rules cần biết

| Rule | Chi tiết |
|---|---|
| Tối đa 3 plans | Tổng cả personal + coach-created, lỗi 400 nếu vượt |
| 1 coach tại 1 thời điểm | Không thể request coach khi đang có quan hệ Active |
| Hủy coach → xóa plans | Plans do coach tạo bị xóa ngay, không recover |
| Set đã done không undo | Gọi lại sẽ nhận 400 "Set is already completed." |
| Đổi PlannedSets | Chỉ được khi chưa có set nào done |
| Auto-complete day | Chỉ khi TẤT CẢ exercises trong ngày isCompleted = true |

---

## 7. Error Handling Pattern

Mọi lỗi từ BE đều có shape:
```json
{ "message": "Mô tả lỗi cụ thể" }
```

**HTTP Status mapping:**

| Status | Ý nghĩa |
|---|---|
| 200 / 201 | Thành công |
| 204 | Thành công, không có data (Delete, Logout) |
| 400 | Dữ liệu sai hoặc vi phạm business rule |
| 401 | Token hết hạn hoặc không hợp lệ |
| 403 | Không có quyền (sai role) |
| 404 | Resource không tồn tại |

**React example:**
```tsx
try {
  const exercise = await xenohApi.createExercise(data);
  // success
} catch (err) {
  if (axios.isAxiosError(err)) {
    const msg = err.response?.data?.message ?? 'Đã có lỗi xảy ra';
    toast.error(msg);
  }
}
```

---

## 8. Data Hierarchy

```
User
├── Plan (tối đa 3)
│   ├── WeeklyWorkout (tuần 1)
│   │   ├── DailyWorkout (Thứ 2)
│   │   │   ├── Exercise (Bench Press)
│   │   │   │   ├── ExerciseSet #1 → isCompleted, actualReps, actualWeight
│   │   │   │   ├── ExerciseSet #2
│   │   │   │   └── ExerciseSet #3
│   │   │   └── Exercise (Squat)
│   │   │       ├── ExerciseSet #1
│   │   │       └── ExerciseSet #2
│   │   └── DailyWorkout (Thứ 3) ...
│   └── WeeklyWorkout (tuần 2) ...
│
├── CoachClientRelationship
│   ├── status: Pending | Active
│   └── (khi Active) Coach có thể tạo Plan cho User
│
└── (tham chiếu) ExerciseTemplate (47 bài tập, seed sẵn, không sửa được)
```

**Key IDs cần lưu trong state:**
```
planId → weekId → dayId → exerciseId → setId
```
Khi navigate giữa các màn hình, truyền ID qua route params hoặc global state.
