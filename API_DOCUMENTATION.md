# Xenoh API Documentation

Base URL: `http://localhost:{port}/api`

> **Auth**: Tất cả endpoint (trừ `auth/register`, `auth/login`, `auth/refresh-token`) đều yêu cầu header:
> `Authorization: Bearer {accessToken}`

---

## MuscleGroup Enum
| Value | Số |
|---|---|
| Chest | 0 |
| Back | 1 |
| Shoulders | 2 |
| Biceps | 3 |
| Triceps | 4 |
| Forearms | 5 |
| Core | 6 |
| Quadriceps | 7 |
| Hamstrings | 8 |
| Glutes | 9 |
| Calves | 10 |
| FullBody | 11 |

---

## 1. Auth

### POST `/api/auth/register`
Đăng ký tài khoản mới.

**Input (Body)**
```json
{
  "email": "nguyenvana@gmail.com",
  "password": "123456",
  "firstName": "Nguyen",
  "lastName": "Van A",
  "role": "Individual"
}
```
> `role`: `"Individual"` hoặc `"Coach"`

**Output `200 OK`**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhMWIyYzNkNC1lNWY2LTc4OTAtYWJjZC1lZjEyMzQ1Njc4OTAiLCJlbWFpbCI6Im5ndXllbnZhbmFAZ21haWwuY29tIiwicm9sZSI6IkluZGl2aWR1YWwiLCJleHAiOjE3MzYwMDAwMDB9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
  "refreshToken": "d9f3a1b2c4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1",
  "email": "nguyenvana@gmail.com",
  "fullName": "Nguyen Van A",
  "roles": ["Individual"]
}
```

**Output `400 Bad Request`** – Email đã tồn tại
```json
{
  "message": "Email is already registered."
}
```

**Output `400 Bad Request`** – Role không hợp lệ
```json
{
  "message": "Role 'Admin' is not valid. Allowed: Individual, Coach."
}
```

---

### POST `/api/auth/login`
Đăng nhập.

**Input (Body)**
```json
{
  "email": "nguyenvana@gmail.com",
  "password": "123456"
}
```

**Output `200 OK`**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhMWIyYzNkNC1lNWY2LTc4OTAtYWJjZC1lZjEyMzQ1Njc4OTAiLCJlbWFpbCI6Im5ndXllbnZhbmFAZ21haWwuY29tIiwicm9sZSI6IkluZGl2aWR1YWwiLCJleHAiOjE3MzYwMDAwMDB9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
  "refreshToken": "d9f3a1b2c4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1",
  "email": "nguyenvana@gmail.com",
  "fullName": "Nguyen Van A",
  "roles": ["Individual"]
}
```

**Output `400 Bad Request`** – Sai thông tin
```json
{
  "message": "Invalid email or password."
}
```

---

### POST `/api/auth/refresh-token`
Lấy access token mới bằng refresh token.

**Input (Body)**
```json
{
  "refreshToken": "d9f3a1b2c4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1"
}
```

**Output `200 OK`**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhMWIyYzNkNC1lNWY2LTc4OTAtYWJjZC1lZjEyMzQ1Njc4OTAiLCJlbWFpbCI6Im5ndXllbnZhbmFAZ21haWwuY29tIiwicm9sZSI6IkluZGl2aWR1YWwiLCJleHAiOjE3MzYwODY0MDB9.newTokenSignature_xyz",
  "refreshToken": "f1e2d3c4b5a6978869504132106a5b4c3d2e1f0a9b8c7d6e5f4a3b2c1d0e9f8a7",
  "email": "nguyenvana@gmail.com",
  "fullName": "Nguyen Van A",
  "roles": ["Individual"]
}
```

**Output `400 Bad Request`** – Token hết hạn hoặc không hợp lệ
```json
{
  "message": "Invalid or expired refresh token."
}
```

---

### POST `/api/auth/logout` 🔒
Đăng xuất – blacklist access token hiện tại.

**Headers**: `Authorization: Bearer eyJhbGci...`

**Input Body**: Không có.

**Output `204 No Content`**

---

## 2. Plans

### GET `/api/plans` 🔒
Lấy tất cả plans của user đang đăng nhập.

**Input**: Không có.

**Output `200 OK`**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Bulk Season 2025",
    "startDate": "2025-01-06",
    "endDate": "2025-04-06",
    "planType": "Personal",
    "ownerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "createdByCoachId": null,
    "totalWeeks": 13,
    "createdAt": "2025-01-01T08:30:00Z"
  },
  {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "name": "Cut Phase",
    "startDate": "2025-05-01",
    "endDate": "2025-07-31",
    "planType": "CoachAssigned",
    "ownerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "createdByCoachId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "totalWeeks": 13,
    "createdAt": "2025-04-20T10:00:00Z"
  }
]
```

---

### GET `/api/plans/{planId}` 🔒
Lấy chi tiết một plan.

**Input (Route)**: `planId = 3fa85f64-5717-4562-b3fc-2c963f66afa6`

**Output `200 OK`**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Bulk Season 2025",
  "startDate": "2025-01-06",
  "endDate": "2025-04-06",
  "planType": "Personal",
  "ownerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "createdByCoachId": null,
  "totalWeeks": 13,
  "createdAt": "2025-01-01T08:30:00Z"
}
```

**Output `404 Not Found`**
```json
{
  "message": "Plan not found."
}
```

---

### POST `/api/plans` 🔒
Tạo plan mới cho chính user hiện tại.

**Input (Body)**
```json
{
  "name": "Bulk Season 2025",
  "startDate": "2025-01-06",
  "endDate": "2025-04-06"
}
```

**Output `201 Created`**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Bulk Season 2025",
  "startDate": "2025-01-06",
  "endDate": "2025-04-06",
  "planType": "Personal",
  "ownerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "createdByCoachId": null,
  "totalWeeks": 13,
  "createdAt": "2025-01-01T08:30:00Z"
}
```

**Output `400 Bad Request`**
```json
{
  "message": "End date must be after start date."
}
```

---

### POST `/api/plans/for-user` 🔒 (Coach only)
Coach tạo plan cho một client.

**Input (Body)**
```json
{
  "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "Cut Phase for Van A",
  "startDate": "2025-05-01",
  "endDate": "2025-07-31"
}
```

**Output `201 Created`**
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Cut Phase for Van A",
  "startDate": "2025-05-01",
  "endDate": "2025-07-31",
  "planType": "CoachAssigned",
  "ownerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "createdByCoachId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "totalWeeks": 13,
  "createdAt": "2025-04-20T10:00:00Z"
}
```

**Output `400 Bad Request`** – Không phải client của coach
```json
{
  "message": "User is not your client."
}
```

---

### DELETE `/api/plans/{planId}` 🔒
Xoá plan.

**Input (Route)**: `planId = 3fa85f64-5717-4562-b3fc-2c963f66afa6`

**Output `204 No Content`**

**Output `404 Not Found`**
```json
{
  "message": "Plan not found."
}
```

---

## 3. Weekly Workouts

### GET `/api/plans/{planId}/weeks` 🔒
Lấy danh sách tuần của một plan.

**Input (Route)**: `planId = 3fa85f64-5717-4562-b3fc-2c963f66afa6`

**Output `200 OK`**
```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "weekNumber": 1,
    "startDate": "2025-01-06",
    "endDate": "2025-01-12",
    "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "totalDays": 7,
    "completedDays": 3
  },
  {
    "id": "22222222-2222-2222-2222-222222222222",
    "weekNumber": 2,
    "startDate": "2025-01-13",
    "endDate": "2025-01-19",
    "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "totalDays": 7,
    "completedDays": 0
  }
]
```

**Output `404 Not Found`**
```json
{
  "message": "Plan not found."
}
```

---

## 4. Daily Workouts

### GET `/api/weeks/{weeklyWorkoutId}/days` 🔒
Lấy danh sách ngày tập của một tuần.

**Input (Route)**: `weeklyWorkoutId = 11111111-1111-1111-1111-111111111111`

**Output `200 OK`**
```json
[
  {
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "date": "2025-01-06",
    "dayOfWeek": "Monday",
    "isCompleted": true,
    "weeklyWorkoutId": "11111111-1111-1111-1111-111111111111",
    "totalExercises": 4,
    "completedExercises": 4
  },
  {
    "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "date": "2025-01-07",
    "dayOfWeek": "Tuesday",
    "isCompleted": false,
    "weeklyWorkoutId": "11111111-1111-1111-1111-111111111111",
    "totalExercises": 3,
    "completedExercises": 1
  },
  {
    "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
    "date": "2025-01-08",
    "dayOfWeek": "Wednesday",
    "isCompleted": false,
    "weeklyWorkoutId": "11111111-1111-1111-1111-111111111111",
    "totalExercises": 0,
    "completedExercises": 0
  }
]
```

**Output `404 Not Found`**
```json
{
  "message": "Weekly workout not found."
}
```

---

## 5. Exercises

### GET `/api/exercises/by-day/{dailyWorkoutId}` 🔒
Lấy danh sách bài tập trong một ngày.

**Input (Route)**: `dailyWorkoutId = aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`

**Output `200 OK`**
```json
[
  {
    "id": "e1111111-1111-1111-1111-111111111111",
    "exerciseTemplateId": "t1111111-1111-1111-1111-111111111111",
    "name": "Bench Press",
    "primaryMuscleGroup": "Chest",
    "secondaryMuscleGroups": ["Triceps", "Shoulders"],
    "plannedSets": 4,
    "plannedReps": 10,
    "actualSets": 4,
    "actualReps": 10,
    "completedSets": 4,
    "isCompleted": true,
    "notes": "Tăng tạ lên 80kg",
    "dailyWorkoutId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
  },
  {
    "id": "e2222222-2222-2222-2222-222222222222",
    "exerciseTemplateId": "t2222222-2222-2222-2222-222222222222",
    "name": "Incline Dumbbell Press",
    "primaryMuscleGroup": "Chest",
    "secondaryMuscleGroups": ["Shoulders"],
    "plannedSets": 3,
    "plannedReps": 12,
    "actualSets": null,
    "actualReps": null,
    "completedSets": 0,
    "isCompleted": false,
    "notes": null,
    "dailyWorkoutId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
  }
]
```

**Output `404 Not Found`**
```json
{
  "message": "Daily workout not found."
}
```

---

### POST `/api/exercises` 🔒
Thêm bài tập vào một ngày tập.

**Input (Body)**
```json
{
  "dailyWorkoutId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "exerciseTemplateId": "t1111111-1111-1111-1111-111111111111",
  "plannedSets": 4,
  "plannedReps": 10,
  "notes": "Tăng tạ lên 80kg"
}
```
> `plannedSets`: 1–100 | `plannedReps`: 1–1000

**Output `200 OK`**
```json
{
  "id": "e1111111-1111-1111-1111-111111111111",
  "exerciseTemplateId": "t1111111-1111-1111-1111-111111111111",
  "name": "Bench Press",
  "primaryMuscleGroup": "Chest",
  "secondaryMuscleGroups": ["Triceps", "Shoulders"],
  "plannedSets": 4,
  "plannedReps": 10,
  "actualSets": null,
  "actualReps": null,
  "completedSets": 0,
  "isCompleted": false,
  "notes": "Tăng tạ lên 80kg",
  "dailyWorkoutId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
}
```

**Output `400 Bad Request`** – Template không tồn tại
```json
{
  "message": "Exercise template not found."
}
```

---

### PUT `/api/exercises/{exerciseId}` 🔒
Cập nhật kế hoạch bài tập (sets/reps/notes).

**Input (Route)**: `exerciseId = e1111111-1111-1111-1111-111111111111`

**Input (Body)**
```json
{
  "exerciseId": "e1111111-1111-1111-1111-111111111111",
  "plannedSets": 5,
  "plannedReps": 8,
  "notes": "Heavy day – 85kg"
}
```

**Output `200 OK`**
```json
{
  "id": "e1111111-1111-1111-1111-111111111111",
  "exerciseTemplateId": "t1111111-1111-1111-1111-111111111111",
  "name": "Bench Press",
  "primaryMuscleGroup": "Chest",
  "secondaryMuscleGroups": ["Triceps", "Shoulders"],
  "plannedSets": 5,
  "plannedReps": 8,
  "actualSets": null,
  "actualReps": null,
  "completedSets": 0,
  "isCompleted": false,
  "notes": "Heavy day – 85kg",
  "dailyWorkoutId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
}
```

**Output `400 Bad Request`** – ExerciseId trong route và body không khớp
```json
{
  "message": "ExerciseId mismatch."
}
```

---

### PATCH `/api/exercises/{exerciseId}/progress` 🔒
Cập nhật tiến độ thực tế khi đang tập.

**Input (Route)**: `exerciseId = e1111111-1111-1111-1111-111111111111`

**Input (Body)**
```json
{
  "exerciseId": "e1111111-1111-1111-1111-111111111111",
  "actualSets": 4,
  "actualReps": 10,
  "completedSets": 4
}
```
> `actualSets`: 0–100 | `actualReps`: 0–1000 | `completedSets`: 0–100

**Output `200 OK`**
```json
{
  "id": "e1111111-1111-1111-1111-111111111111",
  "exerciseTemplateId": "t1111111-1111-1111-1111-111111111111",
  "name": "Bench Press",
  "primaryMuscleGroup": "Chest",
  "secondaryMuscleGroups": ["Triceps", "Shoulders"],
  "plannedSets": 4,
  "plannedReps": 10,
  "actualSets": 4,
  "actualReps": 10,
  "completedSets": 4,
  "isCompleted": true,
  "notes": "Tăng tạ lên 80kg",
  "dailyWorkoutId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
}
```

---

### DELETE `/api/exercises/{exerciseId}` 🔒
Xoá bài tập.

**Input (Route)**: `exerciseId = e1111111-1111-1111-1111-111111111111`

**Output `204 No Content`**

**Output `404 Not Found`**
```json
{
  "message": "Exercise not found."
}
```

---

## 6. Exercise Templates

### GET `/api/exercise-templates` 🔒
Lấy danh sách tất cả template bài tập, có thể lọc theo nhóm cơ.

**Input (Query – tuỳ chọn)**
| Param | Type | Mô tả |
|---|---|---|
| `muscleGroup` | `int` | Lọc theo MuscleGroup enum (vd: `0` = Chest) |

**Ví dụ**: `GET /api/exercise-templates?muscleGroup=0`

**Output `200 OK`** – Không lọc (trả về tất cả, sắp xếp theo nhóm cơ rồi tên)
```json
[
  {
    "id": "t1111111-1111-1111-1111-111111111111",
    "name": "Bench Press",
    "description": "Flat barbell bench press targeting the chest.",
    "primaryMuscleGroup": "Chest",
    "secondaryMuscleGroups": ["Triceps", "Shoulders"]
  },
  {
    "id": "t2222222-2222-2222-2222-222222222222",
    "name": "Incline Dumbbell Press",
    "description": "Upper chest emphasis with dumbbells.",
    "primaryMuscleGroup": "Chest",
    "secondaryMuscleGroups": ["Shoulders"]
  },
  {
    "id": "t3333333-3333-3333-3333-333333333333",
    "name": "Pull-Up",
    "description": "Bodyweight pull-up for back width.",
    "primaryMuscleGroup": "Back",
    "secondaryMuscleGroups": ["Biceps"]
  },
  {
    "id": "t4444444-4444-4444-4444-444444444444",
    "name": "Squat",
    "description": "Barbell back squat.",
    "primaryMuscleGroup": "Quadriceps",
    "secondaryMuscleGroups": ["Glutes", "Hamstrings", "Core"]
  }
]
```

**Output `200 OK`** – Lọc `muscleGroup=0` (Chest)
```json
[
  {
    "id": "t1111111-1111-1111-1111-111111111111",
    "name": "Bench Press",
    "description": "Flat barbell bench press targeting the chest.",
    "primaryMuscleGroup": "Chest",
    "secondaryMuscleGroups": ["Triceps", "Shoulders"]
  },
  {
    "id": "t2222222-2222-2222-2222-222222222222",
    "name": "Incline Dumbbell Press",
    "description": "Upper chest emphasis with dumbbells.",
    "primaryMuscleGroup": "Chest",
    "secondaryMuscleGroups": ["Shoulders"]
  }
]
```

---

## 7. Coach-Client

### POST `/api/coach-client/request` 🔒 (Individual only)
Client gửi yêu cầu kết nối với coach.

**Input (Body)**
```json
{
  "coachId": "b2c3d4e5-f6a7-8901-bcde-f12345678901"
}
```

**Output `200 OK`**
```json
{
  "id": "r1111111-1111-1111-1111-111111111111",
  "clientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "clientName": "Nguyen Van A",
  "coachId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "coachName": "Tran Van B",
  "status": "Pending",
  "createdAt": "2025-01-10T09:00:00Z"
}
```

**Output `400 Bad Request`** – Coach không tồn tại
```json
{
  "message": "Coach not found."
}
```

**Output `400 Bad Request`** – Đã có yêu cầu pending
```json
{
  "message": "You already have a pending or active coach relationship."
}
```

---

### PUT `/api/coach-client/accept/{relationshipId}` 🔒 (Coach only)
Coach chấp nhận yêu cầu từ client.

**Input (Route)**: `relationshipId = r1111111-1111-1111-1111-111111111111`

**Output `200 OK`**
```json
{
  "id": "r1111111-1111-1111-1111-111111111111",
  "clientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "clientName": "Nguyen Van A",
  "coachId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "coachName": "Tran Van B",
  "status": "Accepted",
  "createdAt": "2025-01-10T09:00:00Z"
}
```

**Output `400 Bad Request`** – Không phải yêu cầu gửi tới coach này
```json
{
  "message": "Relationship not found or not pending."
}
```

---

### DELETE `/api/coach-client/{relationshipId}` 🔒
Kết thúc quan hệ coach-client (cả hai phía đều có thể terminate).

**Input (Route)**: `relationshipId = r1111111-1111-1111-1111-111111111111`

**Output `204 No Content`**

**Output `400 Bad Request`**
```json
{
  "message": "Relationship not found."
}
```

---

### GET `/api/coach-client/pending-requests` 🔒 (Coach only)
Lấy danh sách yêu cầu đang chờ xử lý gửi đến coach.

**Input**: Không có.

**Output `200 OK`**
```json
[
  {
    "id": "r1111111-1111-1111-1111-111111111111",
    "clientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "clientName": "Nguyen Van A",
    "coachId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "coachName": "Tran Van B",
    "status": "Pending",
    "createdAt": "2025-01-10T09:00:00Z"
  },
  {
    "id": "r2222222-2222-2222-2222-222222222222",
    "clientId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "clientName": "Le Thi C",
    "coachId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "coachName": "Tran Van B",
    "status": "Pending",
    "createdAt": "2025-01-11T14:30:00Z"
  }
]
```

---

### GET `/api/coach-client/my-coach` 🔒 (Individual only)
Client xem thông tin coach hiện tại.

**Input**: Không có.

**Output `200 OK`**
```json
{
  "id": "r1111111-1111-1111-1111-111111111111",
  "clientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "clientName": "Nguyen Van A",
  "coachId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "coachName": "Tran Van B",
  "status": "Accepted",
  "createdAt": "2025-01-10T09:00:00Z"
}
```

**Output `404 Not Found`** – Chưa có coach
```json
{}
```

---

## Error Response Format

```json
{ "message": "Mô tả lỗi" }
```

| Status | Ý nghĩa |
|---|---|
| 400 | Input không hợp lệ hoặc lỗi nghiệp vụ |
| 401 | Chưa đăng nhập / token hết hạn |
| 403 | Không đủ quyền (sai Role) |
| 404 | Không tìm thấy resource |
| 204 | Thành công, không có data trả về |
