# AI Gateway

AI Gateway là dịch vụ backend trung gian giữa các ứng dụng và các nhà cung cấp Large Language Model (LLM) như OpenAI và Gemini.

Thay vì để mỗi ứng dụng tích hợp trực tiếp với từng LLM Provider, ứng dụng chỉ cần gọi một API duy nhất của AI Gateway.

Application
     |
     v
AI Gateway
     |
     +------> SQL Server
     |
     +------> OpenAI / Gemini

# 1. Features

Hệ thống cung cấp các chức năng chính:

* Authentication
* JWT Authorization
* Refresh Token
* Conversation Management
* Message History
* LLM Integration
* Structured AI Response
* AI Request Logging
* Latency Tracking
* Token Usage Tracking
* Error Handling
* Retry
* Timeout
* Rate Limiting
* Usage Statistics

# 2. System Architecture

AI Gateway sử dụng kiến trúc phân tầng:

Client
   |
   v
Controller
   |
   v
Service
   |
   +--------------------+
   |                    |
   v                    v
Repository         ILLMProvider
   |                    |
   v                    v
SQL Server         OpenAI/Gemini

## Controller

Controller chịu trách nhiệm:

* Nhận HTTP request
* Validate request ở mức API
* Gọi Service
* Trả HTTP response

Controller không chứa business logic phức tạp và không gọi trực tiếp LLM Provider.

## Service

Service chịu trách nhiệm:

* Business logic
* Authentication-related logic
* Kiểm tra quyền sở hữu Conversation
* Điều phối Conversation và Message
* Gọi LLM Provider
* Xử lý Retry và Timeout
* Ghi nhận AI request usage

## Repository

Repository chịu trách nhiệm:

* Query database
* Insert
* Update
* Delete

Repository không gọi trực tiếp OpenAI hoặc Gemini.

## LLM Provider

Gateway sử dụng abstraction: ILLMProvider

Các provider triển khai abstraction này:

ILLMProvider
    |
    +---- OpenAIProvider
    |
    +---- GeminiProvider

Nhờ đó Service không phụ thuộc trực tiếp vào API riêng của từng nhà cung cấp.

# 3. API Design

API được thiết kế theo RESTful HTTP API.

## Authentication

POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me

## AI

POST /api/ai/chat

## Conversations

GET    /api/conversations
GET    /api/conversations/{id}
DELETE /api/conversations/{id}

## Messages

POST /api/conversations/{conversationId}/messages
GET  /api/conversations/{conversationId}/messages

## Usage

GET /api/usage

## Example: Chat API

POST /api/ai/chat
Authorization: Bearer <access-token>
Content-Type: application/json

Request:

{
  "conversationId": 2,
  "model": "gemini-3.8-flash",
  "message": "REST API là gì?"
}

Response:

{
  "conversationId": 2,
  "model": "gemini-3.8-flash",
  "message": "REST API là một kiểu kiến trúc...",
  "usage": {
    "inputTokens": 20,
    "outputTokens": 100,
    "totalTokens": 120
  }
}

Client chỉ làm việc với format của AI Gateway và không cần biết format nội bộ của OpenAI hoặc Gemini.


# 4. Database Structure

Hệ thống sử dụng:

SQL Server
+
Entity Framework Core

Các bảng chính:

Users
Roles
UserRoles
RefreshTokens

Conversations
Messages

AiRequestLogs

## Relationship

Users
 |
 +---- UserRoles ---- Roles
 |
 +---- RefreshTokens
 |
 +---- Conversations
 |         |
 |         +---- Messages
 |
 +---- AiRequestLogs

## Users

Lưu thông tin tài khoản:

Id
Email
PasswordHash
FullName
Status
CreatedAt

## Roles

Lưu role của người dùng:

User
Admin

## RefreshTokens

Lưu trạng thái Refresh Token:

UserId
TokenHash
FamilyId
CreatedAt
ExpiresAt
RevokedAt

Refresh Token được lưu dưới dạng hash.

`FamilyId` dùng để xác định các Refresh Token thuộc cùng một phiên đăng nhập.

## Conversations

Mỗi Conversation thuộc về một User:

Conversation.UserId = CurrentUserId

User chỉ có thể truy cập Conversation của chính mình.

## Messages

Message thuộc Conversation:

Conversation
    |
    +---- user message
    +---- assistant message

Các role:

user
assistant
system

## AiRequestLogs

Mỗi logical AI request được ghi nhận:

UserId
ConversationId
Provider
Model
RequestedAt
LatencyMs
InputTokens
OutputTokens
Status
ErrorCode

Bảng này là cơ sở để tính Usage.


# 5. AI Integration

AI Gateway không cho Client gọi trực tiếp LLM Provider.

Thay vào đó:

Client
   |
   v
POST /api/ai/chat
   |
   v
AiService
   |
   v
ILLMProvider
   |
   +---- OpenAIProvider
   |
   +---- GeminiProvider

## Chat Flow

1. Authenticate user
2. Lấy UserId từ JWT
3. Kiểm tra Conversation ownership
4. Lấy lịch sử Messages
5. Lưu user message
6. Xây dựng LLM request
7. Gọi LLM Provider
8. Nhận assistant response
9. Lưu assistant message
10. Ghi AI request log
11. Trả structured response

LLM Provider chịu trách nhiệm chuyển đổi format riêng của provider thành format chung của Gateway.

# 6. Error Handling Strategy

Gateway sử dụng Global Exception Handling để xử lý lỗi tập trung.

Response lỗi được chuẩn hóa:

{
  "success": false,
  "error": {
    "code": "PROVIDER_UNAVAILABLE",
    "message": "AI provider is temporarily unavailable"
  },
  "traceId": "..."
}

Gateway không trả cho client:

* API Key
* JWT Secret
* Password
* Refresh Token
* Stack Trace

# 7. Retry Strategy

Retry được dùng để xử lý các lỗi tạm thời từ LLM Provider.

Retry sử dụng exponential backoff với số lần retry giới hạn.

Ví dụ:

Attempt 1
   |
   +---- failed
   |
   +---- wait 500 ms
              |
Attempt 2
   |
   +---- failed
   |
   +---- wait 1000 ms
              |
Attempt 3

Một logical AI request dù được retry nhiều lần vẫn được tính là một request trong Usage.


# 8. Timeout Strategy

Request tới LLM Provider phải có timeout.

Ví dụ:

30 seconds

Nếu provider không phản hồi:

LLM Request
    |
    v
Timeout
    |
    v
Retry nếu lỗi có thể retry
    |
    v
Nếu vẫn thất bại
    |
    v
504 Gateway Timeout

Mục đích là tránh request chạy vô hạn.


# 9. Rate Limiting

AI endpoint được giới hạn theo authenticated user.

Ví dụ:

20 requests / minute / user

Luồng:

Request
   |
   v
Authentication
   |
   v
Rate Limit Check
   |
   +---- exceeded → 429
   |
   v
AiService
   |
   v
LLM Provider

Rate limiting được thực hiện trước khi gọi LLM để giảm request spam và tránh sử dụng tài nguyên LLM không cần thiết.

# 10. Usage Tracking

Mỗi AI request ghi:

User
Provider
Model
Timestamp
Latency
Input Tokens
Output Tokens
Status

## Requests

COUNT(AiRequestLogs)

## Input Tokens

SUM(InputTokens)

## Output Tokens

SUM(OutputTokens)

## Total Tokens

SUM(InputTokens + OutputTokens)

## Average Latency

AVG(LatencyMs)

## Error Rate

FailedRequests / TotalRequests

Ví dụ:

Total Requests = 100
Failed Requests = 2

Error Rate = 2 / 100 = 0.02

API:

GET /api/usage

Response:

{
  "requests": 124,
  "inputTokens": 15000,
  "outputTokens": 33320,
  "totalTokens": 48320,
  "averageLatencyMs": 1230,
  "errorRate": 0.02
}

# 11. Security

Các nguyên tắc bảo mật:

* Password được hash.
* JWT Secret không hard-code.
* LLM API Key không commit vào Git.
* Refresh Token được lưu dưới dạng hash.
* Protected endpoint yêu cầu JWT.
* UserId lấy từ JWT claims.
* User không được truy cập Conversation của user khác.
* User chỉ xem Usage của chính mình.
* Không trả secret hoặc stack trace cho client.


