# AI Gateway - Agent Instructions

## 1. Project Overview

Đây là backend AI Gateway được xây dựng bằng ASP.NET Core Web API.

Mục tiêu của hệ thống:

* Cung cấp một API trung gian giữa các ứng dụng và LLM Provider.
* Ứng dụng client không gọi trực tiếp OpenAI/Gemini/Claude.
* Gateway chịu trách nhiệm authentication, authorization, conversation, AI request logging, timeout, retry, rate limiting và usage statistics.

## 2. Technology Stack

* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQL Server
* JWT Bearer Authentication
* Swagger / OpenAPI
* Postman
* OpenAI API

Không sử dụng Docker trong phạm vi MVP.

## 3. Architecture

Sử dụng kiến trúc:

Controller
↓
Service
↓
Repository
↓
Database

Đối với AI:

Controller
↓
Service
↓
LLM Provider
↓
OpenAI

Service có thể sử dụng đồng thời Repository và LLM Provider.

## 4. Layer Responsibilities

### Controller

Controller chỉ chịu trách nhiệm:

* Nhận HTTP request.
* Validate request ở mức API/model binding.
* Gọi Service.
* Trả HTTP response.

Không đặt business logic phức tạp trong Controller.

### Service

Service chịu trách nhiệm:

* Business logic.
* Authentication logic.
* Authorization-related business rules.
* Conversation processing.
* Điều phối Repository và LLM Provider.
* Retry và timeout policy ở tầng phù hợp.
* Usage tracking.

### Repository

Repository chỉ chịu trách nhiệm làm việc với database:

* Query.
* Insert.
* Update.
* Delete.

Không đặt JWT logic hoặc gọi OpenAI trong Repository.

### Provider

Provider chịu trách nhiệm giao tiếp với LLM Provider.

Sử dụng abstraction:

ILLMProvider

Provider không chịu trách nhiệm lưu conversation hoặc AI request logs.

## 5. Dependency Injection

Sử dụng Dependency Injection của ASP.NET Core.

Ưu tiên đăng ký:

AddScoped<Interface, Implementation>()

Không tạo dependency bằng new trực tiếp trong Controller hoặc Service nếu dependency đó có thể được inject.

## 6. Database

Sử dụng:

* Entity Framework Core
* SQL Server
* Code First Migration

Các entity chính của MVP:

* User
* Role
* UserRole
* RefreshToken
* Conversation
* Message
* AiRequestLog

Không tự ý thay đổi database design nếu chưa kiểm tra tài liệu trong thư mục docs.

## 7. Authentication

Authentication sử dụng:

* JWT Access Token
* Refresh Token
* Refresh Token Rotation
* FamilyId

Password phải được hash.

Không bao giờ lưu password plain text.

Refresh Token phải được lưu dưới dạng hash trong database.

JWT secret không được hard-code trong source code.

Không commit API key, JWT secret hoặc password thật vào Git.

## 8. Authorization

Endpoint protected phải sử dụng:

[Authorize]

Endpoint yêu cầu Admin có thể sử dụng:

[Authorize(Roles = "Admin")]

User chỉ được truy cập resource thuộc về chính mình, ví dụ Conversation của chính user đó.

Không tin UserId do client gửi lên nếu UserId có thể lấy từ JWT claims.

## 9. AI Gateway Rules

Application client chỉ gọi AI Gateway.

Không để client gọi trực tiếp OpenAI.

AI logic phải thông qua:

ILLMProvider

Không để AiService phụ thuộc trực tiếp vào một implementation cụ thể nếu abstraction có thể sử dụng.

Response của Gateway phải được chuẩn hóa, không trả nguyên response format phụ thuộc vào provider cho client.

## 10. AI Request Logging

Mỗi AI request phải ghi nhận:

* User
* Model
* Timestamp
* Latency
* Input Tokens
* Output Tokens
* Status

Nếu request thất bại, phải ghi trạng thái thất bại và error code phù hợp nếu có.

## 11. Error Handling

Sử dụng error response thống nhất.

Không trả stack trace hoặc secret cho client.

HTTP status phải phù hợp với lỗi:

* 400 Bad Request
* 401 Unauthorized
* 403 Forbidden
* 404 Not Found
* 429 Too Many Requests
* 500 Internal Server Error
* 502 Bad Gateway
* 504 Gateway Timeout

## 12. Timeout

LLM request không được chạy vô hạn.

Phải có timeout rõ ràng cho request tới provider.

Timeout của provider phải được chuyển thành error phù hợp ở Gateway.

## 13. Retry

Chỉ retry các lỗi tạm thời, ví dụ:

* 429
* 500
* 502
* 503
* 504
* network/transient errors

Không retry các lỗi request không hợp lệ như:

* 400
* 401
* 403
* 404

Retry phải có số lần giới hạn.

Ưu tiên exponential backoff.

## 14. Rate Limiting

AI endpoint phải có rate limiting.

Rate limit phải được kiểm tra trước khi gọi LLM để tránh request spam làm phát sinh chi phí không cần thiết.

Khi vượt giới hạn trả:

429 Too Many Requests

## 15. Coding Rules

* Sử dụng async/await cho I/O.
* Không dùng .Result hoặc .Wait() cho async I/O.
* Không hard-code secret.
* Không tạo code trùng lặp nếu có thể tái sử dụng abstraction.
* Tên class và method phải rõ nghĩa.
* DTO dùng cho request/response.
* Không trả trực tiếp Entity Framework entity nếu DTO phù hợp hơn.
* Không tự ý thêm package khi chưa có lý do.
* Không tự ý thay đổi kiến trúc.

## 16. Development Process

Khi thực hiện một feature:

1. Đọc tài liệu liên quan trong docs.
2. Kiểm tra code hiện tại.
3. Xác định các file cần tạo hoặc sửa.
4. Thực hiện thay đổi nhỏ theo từng bước.
5. Build project sau mỗi nhóm thay đổi.
6. Nếu build lỗi, sửa lỗi trước khi sang bước tiếp theo.
7. Không tiếp tục tạo thêm nhiều code nếu bước hiện tại chưa build thành công.
8. Báo cáo các file đã tạo/sửa.
9. Đưa ra cách test feature.

## 17. Documentation

Tài liệu thiết kế nằm trong:

docs/

Các tài liệu hiện có:

* docs/authentication-design.md
* docs/database-design.md
* docs/api-design.md
* docs/architecture.md

Khi triển khai Authentication, phải đọc:

docs/authentication-design.md

Khi triển khai Database, phải đọc:

docs/database-design.md

## 18. Scope

Chỉ triển khai các yêu cầu MVP.

Không tự ý triển khai các tính năng bonus:

* Multi-model routing
* Model fallback
* Queue
* Cache
* Cost estimation
* Advanced observability

Trừ khi người dùng yêu cầu rõ ràng.

## 19. Important Rule

Không được tự ý thay đổi hoặc xóa code hiện tại chỉ để làm cho kiến trúc phù hợp với ý tưởng mới.

Trước khi refactor code hiện tại:

* Kiểm tra dependency.
* Kiểm tra nơi code đang được sử dụng.
* Giải thích lý do thay đổi.
* Ưu tiên thay đổi nhỏ và an toàn.
