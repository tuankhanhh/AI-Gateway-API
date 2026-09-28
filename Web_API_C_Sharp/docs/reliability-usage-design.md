## 1. Mục tiêu

Đây là module được triển khai **sau khi `POST /api/ai/chat` đã hoạt động thành công**.

Mục tiêu:

- Ghi log cho từng AI request.
- Đo latency.
- Ghi input tokens và output tokens.
- Ghi trạng thái Success/Failed.
- Xử lý lỗi tập trung.
- Timeout khi gọi LLM Provider.
- Retry các lỗi tạm thời.
- Rate limiting cho AI endpoint.
- Chuẩn bị dữ liệu cho `GET /api/usage`.

Không triển khai:

- Multi-model routing
- Fallback model/provider
- Queue
- Cache
- Cost estimation
- Dashboard
- OpenTelemetry/Prometheus/Grafana

---

# 2. Điều kiện đầu vào

Trước khi triển khai module này, các phần sau phải hoạt động:

```text
Authentication
Conversation + Message
ILLMProvider
OpenAIProvider / GeminiProvider
AiService
AiController
POST /api/ai/chat

Chat MVP phải có ít nhất một lần test:

POST /api/ai/chat
        ↓
200 OK
        ↓
assistant response
        ↓
assistant Message saved
3. Kiến trúc

Luồng hoàn chỉnh:

Client
   |
   | POST /api/ai/chat
   v
AiController
   |
   v
Rate Limiter
   |
   v
AiService
   |
   +----> ConversationRepository
   |
   +----> MessageRepository
   |
   +----> ILLMProvider
              |
              v
         OpenAI/Gemini
              |
              v
        LLM Response
   |
   +----> Measure Latency
   |
   +----> Save AiRequestLog
   |
   v
Response

Khi có lỗi:

LLM Provider
      |
      v
Provider Error
      |
      v
Retry (nếu transient)
      |
      +---- success
      |
      +---- failed
               |
               v
       AiRequestLog = Failed
               |
               v
       Global Error Handler
               |
               v
          HTTP Response
4. Database - AiRequestLogs

Tạo entity:

AiRequestLog

Bảng:

AiRequestLogs
---------------------------------------------------------
Id                  bigint / Guid       PK
UserId              FK -> Users
ConversationId      FK -> Conversations
Provider            nvarchar(50)
Model               nvarchar(100)
RequestedAt         datetime2
LatencyMs            bigint
InputTokens         int
OutputTokens        int
Status              nvarchar(20)
ErrorCode            nvarchar(100) NULL

Có thể thêm:

ErrorMessage        nvarchar(max) NULL

nhưng không lưu:

API key
JWT
Password
Sensitive secret
5. Status

Chỉ cần:

Success
Failed

Ví dụ:

Status = Success

hoặc:

Status = Failed
6. AiRequestLog Repository

Tạo:

Repository/
├── Interfaces/
│   └── IAiRequestLogRepository.cs
└── Implementations/
    └── AiRequestLogRepository.cs

Interface:

public interface IAiRequestLogRepository
{
    Task AddAsync(AiRequestLog log);

    Task<List<AiRequestLog>> GetByUserIdAsync(
        int userId
    );
}

Có thể mở rộng query theo thời gian/model sau này nếu cần cho Usage.

7. Ghi log khi request thành công

Ví dụ:

UserId = 1
Provider = Gemini
Model = gemini-3.8-flash
LatencyMs = 1230
InputTokens = 100
OutputTokens = 250
Status = Success

Lưu sau khi provider trả về thành công.

8. Ghi log khi request thất bại

Ví dụ:

UserId = 1
Provider = Gemini
Model = gemini-3.8-flash
LatencyMs = 30500
InputTokens = 0
OutputTokens = 0
Status = Failed
ErrorCode = PROVIDER_UNAVAILABLE

Nếu provider thất bại trước khi nhận usage:

InputTokens = 0
OutputTokens = 0

Không tự đoán token thật.

9. Latency Tracking

Bắt đầu timer ngay trước khi gọi provider:

var stopwatch = Stopwatch.StartNew();

var response = await _provider.ChatAsync(request);

stopwatch.Stop();

Lấy:

stopwatch.ElapsedMilliseconds

Lưu vào:

LatencyMs

Ví dụ:

Provider call:
Start
   ↓
Gemini
   ↓
1230 ms
   ↓
Stop

Latency phải bao gồm toàn bộ thời gian chờ provider.

10. Token Tracking

Provider response phải trả:

InputTokens
OutputTokens

Gateway lưu nguyên giá trị provider trả về.

Không tự tính token thật bằng:

string.Length

trong production.

Nếu Mock Provider trả token giả thì chỉ dùng để test.

11. Global Error Handling

Tạo:

Middleware/
└── GlobalExceptionMiddleware.cs

Mục tiêu:

Exception
   ↓
Global Middleware
   ↓
Map Error
   ↓
HTTP Response

Không để mỗi Controller tự xử lý toàn bộ exception.

12. Error Response

Chuẩn hóa:

{
  "success": false,
  "error": {
    "code": "PROVIDER_UNAVAILABLE",
    "message": "AI provider is temporarily unavailable"
  },
  "traceId": "..."
}

Không trả:

stack trace
API key
JWT
internal exception details
secret configuration

ra client.

13. Error Code

Tối thiểu:

INVALID_REQUEST
UNAUTHORIZED
FORBIDDEN
NOT_FOUND

PROVIDER_BAD_REQUEST
PROVIDER_UNAUTHORIZED
PROVIDER_RATE_LIMITED
PROVIDER_UNAVAILABLE
PROVIDER_TIMEOUT

INTERNAL_SERVER_ERROR
RATE_LIMIT_EXCEEDED
14. Provider Error Mapping

Các provider có thể trả:

400
401
403
429
500
502
503
504

Gateway không được biến tất cả thành:

500 Internal Server Error

Phải phân loại.

Ví dụ:

Gemini 503
   ↓
PROVIDER_UNAVAILABLE
15. HTTP Status Mapping

Khuyến nghị:

400 → 400 Bad Request
401 → 401 Unauthorized
403 → 403 Forbidden
404 → 404 Not Found

Provider 429
→ 429 Too Many Requests
hoặc xử lý retry trước

Provider 503
→ Retry
→ nếu vẫn fail → 502 Bad Gateway

Provider timeout
→ 504 Gateway Timeout

Unexpected application error
→ 500 Internal Server Error
16. Timeout

LLM request phải có timeout rõ ràng.

Ví dụ:

Timeout = 30 seconds

Luồng:

Gateway
   |
   | call provider
   v
30 seconds
   |
   X
Timeout
   |
   v
Retry nếu policy cho phép
   |
   v
Nếu vẫn timeout
   |
   v
504 Gateway Timeout

Không để request chạy vô hạn.

17. Không Retry tất cả lỗi

Retry chỉ áp dụng cho transient error.

Retry:

429
500
502
503
504
network/transient errors

Không retry:

400
401
403
404
18. Retry Policy

MVP:

MaxRetryAttempts = 3

Exponential backoff:

Attempt 1
   ↓
500 ms
   ↓
Attempt 2
   ↓
1000 ms
   ↓
Attempt 3
   ↓
2000 ms

Có thể thêm jitter.

Không retry vô hạn.

19. Retry phải nằm ở đâu?

Retry nên nằm ở tầng gọi provider / HTTP client, không nằm trong Controller.

Luồng:

AiService
   ↓
ILLMProvider
   ↓
Provider HTTP Call
   ↓
Retry Policy
   ↓
External LLM

Controller không tự viết:

for (...)
{
    ...
}
20. Retry và Logging

Một AI request có retry nhiều lần vẫn được xem là:

1 logical AI request

Khuyến nghị:

AiRequestLog

ghi một log cho logical request cuối cùng.

Ví dụ:

Attempt 1 → 503
Attempt 2 → 503
Attempt 3 → 200

Kết quả cuối:

Status = Success

Nếu muốn debug retry, có thể log thêm internal application log nhưng không tạo 3 usage records.

21. Khi retry thất bại

Ví dụ:

Attempt 1 → 503
Attempt 2 → 503
Attempt 3 → 503

Kết quả:

AiRequestLog.Status = Failed
ErrorCode = PROVIDER_UNAVAILABLE

Response:

502 Bad Gateway
22. Rate Limiting

AI endpoint phải có rate limiting.

MVP:

20 requests / minute / user

Ví dụ:

User A
Request 1
Request 2
...
Request 20
→ Allowed

Request 21
→ 429 Too Many Requests
23. Rate Limit phải chạy trước Provider

Đúng:

Client
   ↓
Authentication
   ↓
Rate Limit
   ↓
AiService
   ↓
LLM Provider

Không đúng:

Client
   ↓
AiService
   ↓
OpenAI
   ↓
Rate Limit

Mục đích:

Không để spam gọi LLM.
Không phát sinh chi phí vô ích.
Bảo vệ provider và Gateway.
24. Rate Limit Key

MVP nên giới hạn theo:

Authenticated UserId

Không chỉ giới hạn theo IP.

Ví dụ:

User A = 20/min
User B = 20/min

Hai user độc lập.

25. Rate Limit Response

Khi vượt:

429 Too Many Requests

Response:

{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many AI requests"
  }
}

Nếu framework hỗ trợ Retry-After, có thể thêm header:

Retry-After: 30
26. Usage Service

Tạo:

Services/
├── Interfaces/
│   └── IUsageService.cs
└── Implementations/
    └── UsageService.cs

Interface ví dụ:

public interface IUsageService
{
    Task<UsageResponseDto> GetMyUsageAsync(
        int userId
    );
}
27. Usage Metrics

UsageService đọc AiRequestLogs.

Tính:

Requests
InputTokens
OutputTokens
TotalTokens
AverageLatencyMs
ErrorRate
28. Công thức Requests
Requests = COUNT(AiRequestLogs)

Ví dụ:

124 logs
→ 124 requests
29. Công thức Total Tokens
TotalTokens
=
SUM(InputTokens + OutputTokens)

Ví dụ:

Input = 15000
Output = 33320

Total = 48320
30. Công thức Average Latency
AverageLatencyMs
=
AVG(LatencyMs)

Ví dụ:

1000
1200
1500

Average = 1233.33 ms

Chỉ tính trên các log có latency hợp lệ.

31. Công thức Error Rate
ErrorRate
=
FailedRequests / TotalRequests

Ví dụ:

Total = 100
Failed = 2

ErrorRate = 0.02

Không trả:

2%

trừ khi DTO được thiết kế theo phần trăm.

Nên thống nhất một kiểu.

32. Usage API
GET /api/usage

Authentication:

Authorization: Bearer <access-token>

User chỉ xem Usage của chính mình.

Không nhận:

?userId=123

để lấy usage người khác nếu endpoint là user-level.

33. Usage Response
{
  "requests": 124,
  "inputTokens": 15000,
  "outputTokens": 33320,
  "totalTokens": 48320,
  "averageLatencyMs": 1230,
  "errorRate": 0.02
}
34. Usage Controller

Tạo:

Controllers/
└── UsageController.cs

Route:

/api/usage

Luồng:

GET /api/usage
      |
      v
UsageController
      |
      v
UsageService
      |
      v
AiRequestLogRepository
      |
      v
SQL Server
35. User Isolation

Nếu User A gọi:

GET /api/usage
Authorization: Bearer <TokenA>

thì query:

WHERE UserId = UserA

User A không được thấy:

User B
36. Transaction / Data Consistency

Trong Chat flow:

Call provider
   |
   v
Provider result
   |
   +---- success
   |      |
   |      +---- Save assistant message
   |      +---- Save AiRequestLog = Success
   |
   +---- failure
          |
          +---- Save AiRequestLog = Failed
          +---- No assistant message

Assistant message không được tạo nếu LLM thất bại.

37. AiService Flow sau khi triển khai module

Flow mục tiêu:

1. Validate request
2. Validate Conversation ownership
3. Start latency timer
4. Call provider
5. Retry if transient error
6. Stop latency timer
7. If success:
      - Save assistant message
      - Save AiRequestLog Success
8. If failure:
      - Save AiRequestLog Failed
      - Throw mapped exception
9. Global middleware returns HTTP error
38. Không log secret

Không ghi vào AiRequestLogs:

OpenAI API Key
Gemini API Key
JWT
Refresh Token
Password
Authorization header
39. Testing - Logging
Test success
POST /api/ai/chat

Expected:

200

Database:

AiRequestLogs
Status = Success
LatencyMs > 0
Model != null
InputTokens >= 0
OutputTokens >= 0
40. Testing - Provider 503

Dùng Mock Provider:

SimulateError = true

hoặc chế độ mô phỏng 503.

Expected:

Retry
Retry
Retry

Sau khi hết retry:

502 Bad Gateway

Database:

Status = Failed
ErrorCode = PROVIDER_UNAVAILABLE
41. Testing - Timeout

Dùng Mock Provider:

DelayMs > Timeout

Expected:

Timeout
→ Retry nếu policy áp dụng
→ cuối cùng 504

Log:

Status = Failed
ErrorCode = PROVIDER_TIMEOUT
42. Testing - Rate Limit

Gửi hơn:

20 requests / minute

Expected:

Request 1-20 → allowed
Request 21 → 429

Request bị rate limit:

không được gọi LLM
43. Testing - Usage

Sau khi có:

10 success
2 failed

Usage:

requests = 12
errorRate = 2 / 12

Tổng tokens lấy từ:

SUM(InputTokens + OutputTokens)
44. Testing - User Isolation

User A:

GET /api/usage

chỉ thấy:

logs của User A

User B:

GET /api/usage

chỉ thấy:

logs của User B
45. Định nghĩa hoàn thành

Module hoàn thành khi:

[ ] AiRequestLog entity
[ ] Migration
[ ] Repository
[ ] Log Success
[ ] Log Failed

[ ] Latency Tracking
[ ] Token Tracking

[ ] Global Error Middleware
[ ] Error Code
[ ] Provider Error Mapping

[ ] Timeout
[ ] Retry
[ ] Exponential Backoff

[ ] Rate Limiting
[ ] User-based limit
[ ] 429 response

[ ] UsageService
[ ] UsageController
[ ] GET /api/usage

[ ] Swagger test
[ ] Postman test
[ ] Build thành công
46. Thứ tự triển khai

Triển khai chính xác theo thứ tự:

1. AiRequestLog Entity
       ↓
2. DbContext
       ↓
3. Migration
       ↓
4. AiRequestLog Repository
       ↓
5. Latency Tracking
       ↓
6. Token Tracking
       ↓
7. Success/Failed Logging
       ↓
8. Global Exception Middleware
       ↓
9. Error Mapping
       ↓
10. Timeout
       ↓
11. Retry
       ↓
12. Rate Limiting
       ↓
13. UsageService
       ↓
14. UsageController
       ↓
15. Swagger
       ↓
16. Postman
       ↓
17. Full test