AI Gateway - POST /api/ai/analyze
1. Mục tiêu

Thêm endpoint:

POST /api/ai/analyze

Endpoint dùng để gửi một đoạn text đến LLM để thực hiện một tác vụ phân tích độc lập.

Khác với:

POST /api/ai/chat

/api/ai/chat dùng cho hội thoại và Conversation/Message history.

/api/ai/analyze là một AI request độc lập:

Client
   ↓
POST /api/ai/analyze
   ↓
AiService
   ↓
ILLMProvider
   ↓
Gemini / OpenAI
   ↓
Structured result
   ↓
Client
2. Phạm vi

Chỉ triển khai:

POST /api/ai/analyze
Request DTO
Response DTO
Analyze logic trong AiService
Controller endpoint
Structured AI response
Tái sử dụng LLM Provider hiện tại
Tái sử dụng Error Handling hiện tại
Tái sử dụng Retry/Timeout/Rate Limiting nếu các cơ chế hiện tại đang áp dụng chung cho AI requests
Tái sử dụng AI Request Logging / Usage nếu hệ thống hiện tại đã áp dụng chung

Không triển khai:

Conversation
Message history
Provider mới
Multi-model routing
Fallback
Queue
Cache
Cost estimation
Dashboard

Không thay đổi behavior của:

POST /api/ai/chat
3. Authentication

Endpoint phải yêu cầu JWT:

Authorization: Bearer <access-token>

Nếu không có hoặc token không hợp lệ:

401 Unauthorized

UserId không được nhận từ request body.

Nếu cần UserId cho logging/usage:

JWT Claims
    ↓
CurrentUserId
4. Request DTO

Tạo:

DTOs/
└── AI/
    └── AnalyzeRequestDto.cs

Ví dụ:

public class AnalyzeRequestDto
{
    public string Text { get; set; } = string.Empty;
}

Request:

{
  "text": "Sản phẩm tốt nhưng giao hàng quá chậm"
}
5. Validation

Text phải:

không null;
không empty;
không chỉ chứa whitespace.

Ví dụ không hợp lệ:

{
  "text": ""
}

Response:

400 Bad Request
6. Analyze Task

MVP sử dụng một bài toán rõ ràng:

Sentiment Analysis
+
Summary

Input:

Sản phẩm tốt nhưng giao hàng quá chậm

AI được yêu cầu trả:

sentiment
summary

Không cho client tự truyền system prompt trong MVP.

7. Response DTO

Tạo:

DTOs/
└── AI/
    └── AnalyzeResponseDto.cs

Ví dụ:

public class AnalyzeResponseDto
{
    public string Sentiment { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;
}

Response:

{
  "sentiment": "negative",
  "summary": "Khách hàng hài lòng với sản phẩm nhưng không hài lòng về tốc độ giao hàng."
}
8. Structured Output

Không trả nguyên response JSON của OpenAI/Gemini.

Gateway phải chuyển kết quả provider về format của chính Gateway.

Luồng:

Gemini/OpenAI response
        ↓
Parse / map
        ↓
AnalyzeResponseDto
        ↓
Client

Client chỉ biết:

{
  "sentiment": "...",
  "summary": "..."
}
9. Sử dụng ILLMProvider

Không gọi trực tiếp:

OpenAI API

hoặc:

Gemini API

từ Controller.

Luồng phải là:

AiController
      ↓
AiService
      ↓
ILLMProvider
      ↓
Configured Provider

Tái sử dụng ILLMProvider hiện tại.

Không tạo:

IAnalyzeProvider

nếu không thực sự cần.

10. Tạo LLM Request

AiService chuyển input thành format mà ILLMProvider đang sử dụng.

Ví dụ:

System:
Analyze the following text.
Return:
- sentiment
- summary

User:
Sản phẩm tốt nhưng giao hàng quá chậm

Sau đó:

LLMRequest
    ↓
ILLMProvider

Nếu project đã có cơ chế structured output/function calling thì tái sử dụng cơ chế đó.

Nếu chưa có, parse response theo implementation hiện tại nhưng phải trả AnalyzeResponseDto ổn định.

11. Không tạo Conversation

/api/ai/analyze là request độc lập.

Không làm:

Create Conversation
Create User Message
Create Assistant Message

trừ khi source code hiện tại đã thiết kế logging dùng chung và không liên quan đến conversation.

Mục tiêu:

Analyze
   ↓
LLM
   ↓
Response
12. AI Request Logging

/api/ai/analyze vẫn là AI request.

Nếu AiRequestLogs của project đã được thiết kế dùng chung cho AI request, phải ghi:

UserId
Provider
Model
RequestedAt
LatencyMs
InputTokens
OutputTokens
Status
ErrorCode

Không tạo bảng riêng cho Analyze.

Nếu provider trả token usage:

InputTokens
OutputTokens

thì lưu giá trị provider trả về.

Nếu request thất bại trước khi có usage:

InputTokens = 0
OutputTokens = 0

Không tự đoán token thật.

13. Latency

Dùng cơ chế latency hiện tại của project.

Timer bắt đầu trước provider call:

var stopwatch = Stopwatch.StartNew();

var response = await _provider.ChatAsync(request);

stopwatch.Stop();

Lưu:

LatencyMs

Nếu hệ thống đã có helper/service để đo latency thì tái sử dụng.

14. Retry

Không tạo retry riêng cho Analyze.

Nếu AI pipeline hiện tại đã có retry:

Analyze
   ↓
AiService
   ↓
ILLMProvider
   ↓
Retry Policy

Chỉ retry các lỗi transient theo policy hiện tại.

Ví dụ:

429
500
502
503
504
network/transient error

Không retry:

400
401
403
404
15. Timeout

Không tạo timeout riêng cho Analyze.

Tái sử dụng timeout hiện tại của AI pipeline.

Ví dụ:

Provider timeout
      ↓
504 Gateway Timeout
16. Rate Limiting

/api/ai/analyze là AI endpoint nên phải tuân theo rate limit hiện tại.

Rate limit phải xảy ra trước khi gọi provider:

Request
   ↓
Authentication
   ↓
Rate Limit
   ↓
AiService
   ↓
LLM Provider

Nếu vượt giới hạn:

429 Too Many Requests

Không gọi LLM.

17. Error Handling

Tái sử dụng Global Exception Handling hiện tại.

Không tạo try/catch riêng trong Controller nếu project đã có global middleware.

Các lỗi chính:

400 Bad Request
401 Unauthorized
429 Too Many Requests
502 Bad Gateway
504 Gateway Timeout
500 Internal Server Error

Không trả:

API key
JWT
password
stack trace
secret configuration

cho client.

18. Controller

Tạo hoặc cập nhật:

Controllers/
└── AiController.cs

Endpoint:

POST /api/ai/analyze

Controller phải:

nhận AnalyzeRequestDto;
yêu cầu [Authorize];
lấy current user từ JWT nếu cần;
gọi AiService;
trả AnalyzeResponseDto.

Không gọi provider trực tiếp.

19. Service

Sử dụng service hiện tại.

Nếu project đang có:

IAiService
AiService

thì thêm method:

Task<AnalyzeResponseDto> AnalyzeAsync(
    int userId,
    AnalyzeRequestDto request
);

Không tạo:

AnalyzeService

nếu không cần.

20. Analyze Flow hoàn chỉnh
Client
   |
   | POST /api/ai/analyze
   v
Authentication
   |
   v
Rate Limit
   |
   v
AiController
   |
   v
AiService
   |
   +---- Validate input
   |
   +---- Build LLM request
   |
   v
ILLMProvider
   |
   v
Gemini / OpenAI
   |
   v
LLM Response
   |
   +---- Parse structured output
   |
   +---- Capture token usage
   |
   +---- Capture latency
   |
   +---- Save AI request log
   |
   v
AnalyzeResponseDto
   |
   v
Client
21. Response Example

Request:

POST /api/ai/analyze
Authorization: Bearer <access-token>
Content-Type: application/json
{
  "text": "Sản phẩm tốt nhưng giao hàng quá chậm"
}

Response:

200 OK
{
  "sentiment": "negative",
  "summary": "Khách hàng hài lòng với sản phẩm nhưng không hài lòng về tốc độ giao hàng."
}
22. Test Cases
Test 1 - Success

Input:

{
  "text": "Sản phẩm tốt nhưng giao hàng quá chậm"
}

Expected:

200 OK

Response có:

sentiment
summary
Test 2 - Empty text
{
  "text": ""
}

Expected:

400 Bad Request
Test 3 - Không JWT

Gọi:

POST /api/ai/analyze

không có Authorization.

Expected:

401 Unauthorized
Test 4 - Rate limit

Gửi request vượt giới hạn.

Expected:

429 Too Many Requests

Và provider không được gọi.

Test 5 - Provider unavailable

Dùng Mock Provider hoặc cách mô phỏng lỗi hiện tại.

Expected:

Retry
   ↓
Nếu vẫn thất bại
   ↓
502 Bad Gateway
Test 6 - Provider timeout

Mô phỏng provider không phản hồi trong timeout.

Expected:

504 Gateway Timeout
23. Swagger

Swagger phải hiển thị:

POST /api/ai/analyze

Request body:

{
  "text": "Sản phẩm tốt nhưng giao hàng quá chậm"
}

Endpoint phải có biểu tượng khóa/Authorize vì yêu cầu JWT.

24. Definition of Done
[ ] AnalyzeRequestDto
[ ] AnalyzeResponseDto
[ ] POST /api/ai/analyze
[ ] JWT protected
[ ] Validation
[ ] ILLMProvider reused
[ ] AiService reused/extended
[ ] Structured response
[ ] Không tạo Conversation
[ ] Không tạo Message history
[ ] Existing Retry reused
[ ] Existing Timeout reused
[ ] Existing Rate Limiting reused
[ ] Existing Error Handling reused
[ ] AI Request Logging reused
[ ] Latency tracking reused
[ ] Token tracking reused
[ ] Swagger test
[ ] Build thành công
25. Thứ tự triển khai
1. Kiểm tra AiController
       ↓
2. Kiểm tra AiService
       ↓
3. Kiểm tra ILLMProvider
       ↓
4. Tạo AnalyzeRequestDto
       ↓
5. Tạo AnalyzeResponseDto
       ↓
6. Thêm AnalyzeAsync vào AiService
       ↓
7. Thêm POST /api/ai/analyze
       ↓
8. Tích hợp structured response
       ↓
9. Tái sử dụng logging/latency/token
       ↓
10. Tái sử dụng error/retry/timeout/rate limit
       ↓
11. Build
       ↓
12. Swagger test
       ↓
13. Test error cases