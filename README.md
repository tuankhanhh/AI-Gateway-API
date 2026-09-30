# AI Gateway

AI Gateway là dịch vụ backend trung gian giữa các ứng dụng và các nhà cung cấp Large Language Model (LLM) như OpenAI và Gemini.

**1. Client**

* Ứng dụng client/frontend.
* Gửi HTTP request đến AI Gateway.
* Không gọi trực tiếp OpenAI hoặc Gemini.

**2. API Layer**

* ASP.NET Core Web API.
* Gồm các Controller:

  * AuthController
  * ConversationController
  * MessageController
  * AiController
  * UsageController
* Controller nhận request, gọi Service và trả response.

**3. Business Layer**

* AuthService
* ConversationService
* MessageService
* AiService
* UsageService
* Đây là nơi xử lý business logic và điều phối các thành phần.

**4. Data Access Layer**

* Các Repository:

  * UserRepository
  * RefreshTokenRepository
  * ConversationRepository
  * MessageRepository
  * AiRequestLogRepository
* Repository giao tiếp với SQL Server thông qua Entity Framework Core.

**5. Database**

* SQL Server.
* Các bảng:

  * Users
  * Roles
  * UserRoles
  * RefreshTokens
  * Conversations
  * Messages
  * AiRequestLogs

**6. AI Integration Layer**

* `ILLMProvider` là abstraction trung gian.
* `OpenAIProvider` và `GeminiProvider` triển khai `ILLMProvider`.
* `AiService` chỉ giao tiếp với `ILLMProvider`.
* Provider chịu trách nhiệm giao tiếp với OpenAI hoặc Gemini API.

**7. Reliability**
Thể hiện các cơ chế:

* JWT Authentication / Authorization
* Rate Limiting
* Retry
* Timeout
* Global Error Handling

Các cơ chế này hỗ trợ quá trình xử lý request, đặc biệt là AI request.

**8. AI Request Logging và Usage**

* Mỗi AI request được ghi vào `AiRequestLogs`.
* Log gồm UserId, ConversationId, Provider, Model, RequestedAt, LatencyMs, InputTokens, OutputTokens, Status, ErrorCode.
* `UsageService` đọc dữ liệu từ `AiRequestLogs` để tính:

  * Requests
  * Input Tokens
  * Output Tokens
  * Total Tokens
  * Average Latency
  * Error Rate

**9. Authentication**

* Client đăng ký và đăng nhập thông qua AuthController.
* Hệ thống sử dụng JWT Access Token và Refresh Token.
* Password được hash trước khi lưu database.
* UserId được lấy từ JWT Claims.

**10. Conversation và Message**

* Một User có nhiều Conversation.
* Một Conversation có nhiều Message.
* User chỉ được truy cập Conversation của chính mình.
* Chat request sử dụng Conversation và Message History để tạo context cho LLM.

**11. Luồng chính cần thể hiện**

* Client → API Layer → Service Layer.
* Service Layer → Repository → SQL Server.
* AiService → ILLMProvider → OpenAIProvider/GeminiProvider → LLM Provider.
* AI request → AiRequestLogs → UsageService → Usage API.



