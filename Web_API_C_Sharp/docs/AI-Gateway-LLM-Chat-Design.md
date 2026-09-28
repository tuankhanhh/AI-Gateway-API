LLM Integration & AI Chat Design - AI Gateway

1. Mục tiêu

Đây là bước tiếp theo sau:

Authentication

Conversation

Message

Mục tiêu của module này là:

Tạo abstraction cho LLM Provider.

Tích hợp OpenAI.

Xây dựng POST /api/ai/chat.

Nhận lịch sử Conversation.

Gửi context tới OpenAI.

Nhận kết quả từ OpenAI.

Chuẩn hóa response của Gateway.

Lưu assistant message.

Chuẩn bị dữ liệu cho bước tiếp theo là AI Request Logging, Timeout, Retry và Rate Limiting.

Trong module này chưa triển khai:

Retry

Timeout policy riêng

Rate Limiting

Usage statistics

AiRequestLog

Multi-model routing

Fallback provider

Cache

Queue

Cost estimation

Các phần đó sẽ được thực hiện sau khi Chat cơ bản hoạt động.

2. Kiến trúc

Luồng chính:

Client
   |
   | POST /api/ai/chat
   v
AiController
   |
   v
AiService
   |
   +---------------------> ConversationRepository
   |
   +---------------------> MessageRepository
   |
   +---------------------> ILLMProvider
                                   |
                                   v
                            OpenAIProvider
                                   |
                                   v
                                OpenAI

Sau khi OpenAI trả response:

OpenAI
   |
   v
OpenAIProvider
   |
   v
LLMResponse
   |
   v
AiService
   |
   +---- Save Assistant Message
   |
   v
AiController
   |
   v
Client

3. Nguyên tắc kiến trúc

Controller

AiController chỉ:

Nhận HTTP request.

Lấy CurrentUserId từ JWT.

Gọi IAiService.

Trả HTTP response.

Không gọi OpenAI trực tiếp trong Controller.

Không truy cập DbContext trực tiếp nếu project đang sử dụng Repository.

Service

AiService chịu trách nhiệm:

Validate request ở mức business.

Xác định user hiện tại.

Kiểm tra Conversation ownership.

Lấy lịch sử Message.

Tạo user message.

Gọi ILLMProvider.

Nhận assistant response.

Lưu assistant message.

Trả response chuẩn hóa.

Không chứa code HTTP chi tiết của OpenAI.

Provider

ILLMProvider chịu trách nhiệm abstraction cho LLM.

OpenAIProvider chịu trách nhiệm:

Tạo request gửi tới OpenAI.

Gửi HTTP request.

Đọc response.

Chuyển response của OpenAI thành LLMResponse.

Provider không làm:

Kiểm tra ownership.

Tạo Conversation.

Lưu Message.

Tính usage của Gateway.

Authentication của user.

4. Abstraction ILLMProvider

Tạo:

Providers/
├── Interfaces/
│   └── ILLMProvider.cs
└── OpenAI/
    └── OpenAIProvider.cs

Interface:

public interface ILLMProvider
{
    Task<LLMResponse> ChatAsync(
        LLMRequest request
    );
}

Mục đích:

AiService chỉ biết:

ILLMProvider

không biết chi tiết:

OpenAI HTTP API

Sau này nếu cần thêm Gemini:

ILLMProvider
   |
   +---- OpenAIProvider
   |
   +---- GeminiProvider

Nhưng chỉ triển khai OpenAI ở MVP.

5. LLM Request DTO

Tạo:

DTOs/
└── AI/
    ├── LLMRequest.cs
    └── LLMResponse.cs

Ví dụ:

public class LLMRequest
{
    public string Model { get; set; } = string.Empty;

    public List<LLMMessage> Messages { get; set; }
        = new();
}

LLMMessage:

public class LLMMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

6. LLM Response

Gateway tự định nghĩa format trung gian:

public class LLMResponse
{
    public string Content { get; set; } = string.Empty;

    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }
}

Mục đích:

OpenAI có response format riêng.

Gateway chuyển thành:

LLMResponse

để AiService không phụ thuộc vào OpenAI response object.

7. AI Chat Request DTO

Tạo:

DTOs/
└── AI/
    ├── ChatRequestDto.cs
    └── ChatResponseDto.cs

ChatRequestDto

public class ChatRequestDto
{
    public int? ConversationId { get; set; }

    public string Model { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

ConversationId nullable vì:

null

có nghĩa là tạo Conversation mới.

8. Chat Response DTO

Ví dụ:

public class ChatResponseDto
{
    public int ConversationId { get; set; }

    public string Model { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public TokenUsageDto Usage { get; set; }
        = new();
}

TokenUsage:

public class TokenUsageDto
{
    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int TotalTokens { get; set; }
}

Tổng token:

TotalTokens
=
InputTokens + OutputTokens

9. API

Endpoint

POST /api/ai/chat

Authentication bắt buộc:

Authorization: Bearer <access-token>

10. Request

Ví dụ:

{
  "conversationId": 1,
  "model": "gpt-4.1-mini",
  "message": "REST API là gì?"
}

Nếu muốn tạo Conversation mới:

{
  "conversationId": null,
  "model": "gpt-4.1-mini",
  "message": "REST API là gì?"
}

11. Response

Ví dụ:

{
  "conversationId": 1,
  "model": "gpt-4.1-mini",
  "message": "REST API là một kiểu kiến trúc...",
  "usage": {
    "inputTokens": 20,
    "outputTokens": 100,
    "totalTokens": 120
  }
}

Client chỉ nhận format của AI Gateway.

Không trả nguyên JSON nội bộ của OpenAI.

12. Chat Flow - Conversation đã tồn tại

Ví dụ:

User A
Conversation 1

Request:

{
  "conversationId": 1,
  "model": "gpt-4.1-mini",
  "message": "Cho tôi ví dụ"
}

Luồng:

1. Authenticate user
       |
2. Lấy UserId từ JWT
       |
3. Lấy Conversation 1
       |
4. Kiểm tra Conversation.UserId == CurrentUserId
       |
5. Lấy Messages của Conversation
       |
6. Tạo user message
       |
7. Chuẩn bị LLMRequest
       |
8. Gọi ILLMProvider
       |
9. OpenAI trả assistant response
       |
10. Lưu assistant message
       |
11. Trả ChatResponseDto

13. Chat Flow - Conversation chưa tồn tại

Request:

{
  "conversationId": null,
  "model": "gpt-4.1-mini",
  "message": "REST API là gì?"
}

Luồng:

1. Authenticate user
       |
2. Lấy UserId
       |
3. Tạo Conversation
       |
4. Lưu user message
       |
5. Gọi OpenAI
       |
6. Nhận assistant response
       |
7. Lưu assistant message
       |
8. Trả response

14. Message gửi tới OpenAI

Giả sử database có:

Message 1
role = user
content = "REST API là gì?"

Message 2
role = assistant
content = "REST API là..."

User gửi:

"Cho tôi ví dụ"

Gateway phải tạo:

{
  "model": "gpt-4.1-mini",
  "messages": [
    {
      "role": "user",
      "content": "REST API là gì?"
    },
    {
      "role": "assistant",
      "content": "REST API là..."
    },
    {
      "role": "user",
      "content": "Cho tôi ví dụ"
    }
  ]
}

Mục đích là để LLM có context của cuộc hội thoại.

15. Không gửi toàn bộ database entity tới OpenAI

Không làm:

await provider.ChatAsync(conversation.Messages);

nếu Messages là EF Core entity.

Nên map:

Message Entity
      |
      v
LLMMessage

Chỉ gửi các field cần thiết:

Role
Content

16. User Message

Khi client gửi:

{
  "message": "REST API là gì?"
}

Gateway tạo:

Message
--------------------------------
Role = user
Content = REST API là gì?
ConversationId = 1
Model = NULL
CreatedAt = UTC

Lưu database.

17. Assistant Message

OpenAI trả:

REST API là...

Gateway tạo:

Message
--------------------------------
Role = assistant
Content = REST API là...
ConversationId = 1
Model = gpt-4.1-mini
CreatedAt = UTC

Lưu database.

18. Model

Client có thể gửi:

gpt-4.1-mini

MVP có thể:

Cho phép client chọn model nằm trong whitelist.

Hoặc sử dụng một default model trong configuration.

Khuyến nghị MVP:

Default model

và chỉ cho phép một model đã cấu hình.

Ví dụ:

{
  "OpenAI": {
    "DefaultModel": "gpt-4.1-mini"
  }
}

Không cho client gửi tùy ý model chưa được Gateway hỗ trợ.

19. OpenAI Configuration

Không hard-code API key.

Ví dụ:

{
  "OpenAI": {
    "ApiKey": "",
    "BaseUrl": "https://api.openai.com/v1",
    "DefaultModel": "gpt-4.1-mini"
  }
}

API key thật phải được cung cấp bằng:

User Secrets

Environment Variables

Secret manager của môi trường deploy

Không commit secret thật lên Git.

20. HttpClient

Nên sử dụng IHttpClientFactory.

Đăng ký trong Program.cs:

builder.Services.AddHttpClient<OpenAIProvider>();

Không tạo:

new HttpClient()

trực tiếp trong Service cho từng request.

21. OpenAIProvider

Nhiệm vụ:

LLMRequest
     |
     v
Build OpenAI HTTP Request
     |
     v
POST OpenAI
     |
     v
Read response
     |
     v
Map to LLMResponse

Không xử lý:

Conversation
User
Authorization
Database

22. AiService Interface

Tạo:

public interface IAiService
{
    Task<ChatResponseDto> ChatAsync(
        int userId,
        ChatRequestDto request
    );
}

23. AiService

AiService thực hiện:

1. Validate message
2. Validate model
3. Find/create Conversation
4. Check ownership
5. Load previous Messages
6. Save user Message
7. Build LLMRequest
8. Call ILLMProvider
9. Save assistant Message
10. Update Conversation.UpdatedAt
11. Build ChatResponseDto
12. Return

24. Ownership

Nếu:

CurrentUserId = 1

và:

Conversation.UserId = 2

thì từ chối request.

Không được gọi OpenAI.

Luồng đúng:

Request
   |
   v
Check ownership
   |
   +---- Failed
   |
   X
Không gọi OpenAI

Điều này giúp tránh:

Lộ dữ liệu.

Tốn chi phí LLM không cần thiết.

25. Validation

Message

Không cho:

""
"   "

Model

Không cho model rỗng.

Nếu sử dụng whitelist:

SupportedModels

Chỉ cho phép model được cấu hình.

ConversationId

Nếu có:

Conversation phải tồn tại
Conversation phải thuộc CurrentUser

Nếu null:

Tạo Conversation mới

26. Trạng thái thành công

Nếu OpenAI trả thành công:

1. Save assistant Message
2. Update Conversation
3. Return 200 OK

27. Trường hợp OpenAI trả lỗi

Ở module này chỉ cần:

Bắt lỗi từ provider
        |
        v
AiService trả exception/domain error
        |
        v
Global Error Middleware
        |
        v
HTTP error response

Chưa triển khai retry và timeout riêng ở bước này.

28. HTTP Status

Trường hợp

Status

Chat thành công

200

Request sai

400

Không đăng nhập

401

Conversation không thuộc user

404

Conversation không tồn tại

404

OpenAI/provider error

502

Lỗi server

500

29. Lưu dữ liệu sau một Chat thành công

Ví dụ user gửi:

REST API là gì?

Sau request thành công:

Conversations

Id = 1
UserId = 10
Title = REST API
CreatedAt = ...
UpdatedAt = ...

Messages

Id = 1
ConversationId = 1
Role = user
Content = REST API là gì?
Model = NULL

và:

Id = 2
ConversationId = 1
Role = assistant
Content = REST API là...
Model = gpt-4.1-mini

30. Transaction / SaveChanges

Cần tránh trạng thái dữ liệu không nhất quán.

Ví dụ:

Save User Message
      ↓
OpenAI
      ↓
OpenAI failed

Nếu user message đã được lưu trước khi OpenAI thất bại thì đây vẫn có thể là trạng thái hợp lệ về mặt lịch sử.

MVP có thể chấp nhận.

Nhưng Service phải đảm bảo:

Assistant message chỉ được lưu khi provider trả thành công.

Không lưu assistant message giả.

Conversation UpdatedAt phải được cập nhật phù hợp.

Chưa cần thiết kế distributed transaction.

31. Dependency Injection

Trong Program.cs:

builder.Services.AddScoped<IAiService, AiService>();

builder.Services.AddHttpClient<OpenAIProvider>();

builder.Services.AddScoped<ILLMProvider, OpenAIProvider>();

Có thể cần cấu hình OpenAIProvider bằng IConfiguration hoặc Options pattern.

32. Controller

Tạo:

Controllers/
└── AiController.cs

Route:

/api/ai

Endpoint:

POST /api/ai/chat

Controller:

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
}

CurrentUserId phải lấy từ JWT claims.

Không nhận UserId từ request body.

33. Không để Provider biết CurrentUserId

Không làm:

provider.ChatAsync(userId, request);

Provider chỉ cần:

provider.ChatAsync(llmRequest);

User/Conversation thuộc trách nhiệm của AiService.

34. Test Case

Test 1 - Chat với Conversation mới

Request:

{
  "conversationId": null,
  "message": "REST API là gì?"
}

Expected:

200 OK
Conversation mới được tạo
User message được lưu
Assistant message được lưu

Test 2 - Chat với Conversation có sẵn

{
  "conversationId": 1,
  "message": "Cho tôi ví dụ"
}

Expected:

200 OK
Message mới được thêm vào Conversation 1
LLM nhận context cũ

Test 3 - Conversation của user khác

{
  "conversationId": 999,
  "message": "Hello"
}

Nếu Conversation 999 thuộc user khác:

404 Not Found

OpenAI không được gọi.

Test 4 - Message rỗng

{
  "conversationId": 1,
  "message": ""
}

Expected:

400 Bad Request

Test 5 - Model không hợp lệ

Nếu Gateway dùng whitelist:

{
  "conversationId": 1,
  "model": "unknown-model",
  "message": "Hello"
}

Expected:

400 Bad Request

Test 6 - Không có JWT

POST /api/ai/chat

Không Authorization header.

Expected:

401 Unauthorized

35. Kiểm tra Database

Sau chat thành công, phải kiểm tra:

Conversations

có conversation.

Messages

có:

user
assistant

và:

assistant.Model

có model đã sử dụng.

36. Definition of Done

Module LLM + Chat hoàn thành khi:

[ ] ILLMProvider
[ ] LLMRequest
[ ] LLMResponse

[ ] OpenAIProvider
[ ] OpenAI configuration
[ ] IHttpClientFactory

[ ] ChatRequestDto
[ ] ChatResponseDto
[ ] TokenUsageDto

[ ] IAiService
[ ] AiService
[ ] AiController

[ ] JWT protected
[ ] CurrentUserId from JWT
[ ] Conversation ownership check
[ ] Conversation creation
[ ] User message save
[ ] Load conversation history
[ ] OpenAI call
[ ] Assistant message save
[ ] Conversation UpdatedAt

[ ] Structured response
[ ] Swagger test
[ ] Postman test
[ ] Build thành công

37. Thứ tự triển khai

Thực hiện đúng thứ tự:

1. LLMRequest / LLMMessage
       ↓
2. LLMResponse
       ↓
3. ILLMProvider
       ↓
4. OpenAI configuration
       ↓
5. OpenAIProvider
       ↓
6. Register HttpClient
       ↓
7. ChatRequestDto
       ↓
8. ChatResponseDto
       ↓
9. IAiService
       ↓
10. AiService
       ↓
11. AiController
       ↓
12. Conversation integration
       ↓
13. Message integration
       ↓
14. OpenAI integration
       ↓
15. Save assistant message
       ↓
16. Swagger test
       ↓
17. Postman test
       ↓
18. Build

38. Điều kiện trước khi sang bước Reliability

Chỉ chuyển sang bước tiếp theo khi:

POST /api/ai/chat
        ↓
JWT
        ↓
Conversation
        ↓
Message history
        ↓
OpenAI
        ↓
Assistant response
        ↓
Save assistant Message
        ↓
Structured response

đã chạy hoàn chỉnh.

Bước kế tiếp sau module này:

AI Request Logging
        +
Latency Tracking
        +
Token Usage Logging
        +
Global Error Handling
        +
Timeout
        +
Retry
        +
Rate Limiting

Đây mới là phần giúp AI Gateway đáp ứng đầy đủ các yêu cầu reliability của đề bài.