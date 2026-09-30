# 1. Authentication
## POST /auth/register

Đăng ký tài khoản.

Request
{
  "email": "user@gmail.com",
  "password": "123456",
  "fullName": "Nguyen Van A"
}
Response 200 OK
{
  "message": "Register successful"
}
## POST /auth/login

Đăng nhập.

Request
{
  "email": "user@gmail.com",
  "password": "123456"
}
Response 200 OK
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 900
}

expiresIn là thời gian sống của Access Token, tính bằng giây.

## POST /auth/refresh

Làm mới Access Token bằng Refresh Token.

Request
{
  "refreshToken": "..."
}
Response 200 OK
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 900
}
## POST /auth/logout

Thu hồi Refresh Token hiện tại.

Request
{
  "refreshToken": "..."
}
Response
204 No Content
## GET /auth/me

Lấy thông tin user hiện tại.

Header
Authorization: Bearer <access_token>
Response 200 OK
{
  "id": 15,
  "email": "user@gmail.com",
  "fullName": "Nguyen Van A",
  "role": "User"
}
# 2. Conversation
## POST /conversations

Tạo Conversation.

Request
{
  "title": "REST API"
}
Response
{
  "id": 1,
  "title": "REST API",
  "createdAt": "2026-09-27T10:00:00Z",
  "updatedAt": "2026-09-27T10:00:00Z"
}
## GET /conversations

Lấy các Conversation của user hiện tại.

Response
[
  {
    "id": 1,
    "title": "REST API",
    "createdAt": "2026-09-27T10:00:00Z",
    "updatedAt": "2026-09-27T10:00:00Z"
  }
]
## GET /conversations/{id}

Lấy một Conversation theo ID.

## DELETE /conversations/{id}

Xóa Conversation.

Response
204 No Content
# 3. Message
## POST /conversations/{conversationId}/messages

Tạo Message.

Request
{
  "content": "REST API là gì?"
}
Response
{
  "id": 1,
  "role": "user",
  "content": "REST API là gì?",
  "model": null,
  "createdAt": "2026-09-27T10:00:00Z"
}
## GET /conversations/{conversationId}/messages

Lấy danh sách Message của Conversation.

Response
[
  {
    "id": 1,
    "role": "user",
    "content": "REST API là gì?",
    "model": null,
    "createdAt": "2026-09-27T10:00:00Z"
  }
]
# 4. AI
## POST /ai/chat

Gửi yêu cầu chat đến AI.

Request
{
  "conversationId": 1,
  "model": "gemini-3.8-flash",
  "message": "REST API là gì?"
}

conversationId có thể là null khi tạo Conversation mới.

Response
{
  "conversationId": 1,
  "model": "gemini-3.8-flash",
  "message": "REST API là...",
  "usage": {
    "inputTokens": 25,
    "outputTokens": 80,
    "totalTokens": 105
  }
}
# 5. Usage
## GET /usage

Lấy thống kê sử dụng AI của user hiện tại.

Response
{
  "requests": 120,
  "inputTokens": 3500,
  "outputTokens": 8200,
  "totalTokens": 11700,
  "averageLatencyMs": 1230,
  "errorRate": 0.05
}