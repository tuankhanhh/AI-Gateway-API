Conversation & Message Design - AI Gateway

1. Mục tiêu

Sau khi hoàn thành Authentication, module tiếp theo của AI Gateway là:

Quản lý Conversation

Lưu Message trong từng Conversation

Kiểm tra quyền truy cập Conversation theo User

Chuẩn bị dữ liệu để bước sau tích hợp LLM

Chưa gọi OpenAI ở module này

Luồng:

Authenticated User
        |
        v
Conversation API
        |
        v
Conversation Service
        |
        v
Conversation Repository
        |
        v
SQL Server

Message nằm bên trong Conversation:

User
  |
  +---- Conversation 1
  |          |
  |          +---- Message
  |          +---- Message
  |
  +---- Conversation 2
             |
             +---- Message
             +---- Message

2. Phạm vi

Bắt buộc

Tạo Conversation

Lấy danh sách Conversation của user hiện tại

Lấy chi tiết Conversation

Xóa Conversation

Tạo Message

Lấy Messages của Conversation

Kiểm tra ownership

Authentication bằng JWT

Authorization theo UserId

Chưa làm

Không triển khai trong module này:

OpenAI

Gemini

Claude

ILLMProvider

AI chat

Retry

Timeout

Rate Limiting

Usage

AiRequestLog

Cost

Queue

Cache

Các phần trên sẽ làm ở các bước sau.

3. Database

Sử dụng hai bảng:

Conversations
Messages

Quan hệ:

Conversations
      |
      | 1 - N
      v
Messages

4. Bảng Conversations

Conversations
------------------------------------------------
Id                  uniqueidentifier / int   PK
UserId              uniqueidentifier / int   FK -> Users
Title               nvarchar(200)            NOT NULL
CreatedAt           datetime2                NOT NULL
UpdatedAt           datetime2                NOT NULL

Ý nghĩa

Id

Định danh Conversation.

Có thể sử dụng:

Guid

hoặc int Identity

Phải thống nhất với thiết kế User hiện tại.

UserId

Chủ sở hữu Conversation.

Đây là field quan trọng để authorization.

Ví dụ:

Conversation C001
UserId = U001

thì chỉ U001 được truy cập Conversation C001.

Title

Tên Conversation.

Ví dụ:

REST API
C# EF Core
Học JWT

Ở MVP có thể nhận từ client hoặc tạo title mặc định.

CreatedAt

Thời điểm tạo Conversation.

UpdatedAt

Thời điểm Conversation được cập nhật gần nhất.

5. Bảng Messages

Messages
------------------------------------------------
Id                  uniqueidentifier / int   PK
ConversationId      uniqueidentifier / int   FK
Role                nvarchar(20)             NOT NULL
Content             nvarchar(max)            NOT NULL
Model               nvarchar(100)            NULL
CreatedAt           datetime2                NOT NULL

Role

MVP sử dụng:

user
assistant
system

Ví dụ:

Conversation C001

Message 1:
Role = user
Content = "REST API là gì?"

Message 2:
Role = assistant
Content = "REST API là..."

Model

Ở module Conversation hiện tại có thể để NULL.

Field này được chuẩn bị cho bước AI.

Ví dụ sau này:

assistant
gpt-4.1-mini

Message của user có thể:

Model = NULL

6. Quan hệ Database

Users
  |
  | 1 - N
  v
Conversations
  |
  | 1 - N
  v
Messages

SQL relationship:

Conversations.UserId
        ↓
Users.Id

Messages.ConversationId
        ↓
Conversations.Id

7. Entity Relationship Diagram

erDiagram
    USERS ||--o{ CONVERSATIONS : owns
    CONVERSATIONS ||--o{ MESSAGES : contains

    USERS {
        int Id PK
        string Email
        string PasswordHash
        string FullName
    }

    CONVERSATIONS {
        int Id PK
        int UserId FK
        string Title
        datetime CreatedAt
        datetime UpdatedAt
    }

    MESSAGES {
        int Id PK
        int ConversationId FK
        string Role
        string Content
        string Model
        datetime CreatedAt
    }

Lưu ý: sơ đồ trên minh họa quan hệ. Kiểu int hay Guid phải thống nhất với entity User đã triển khai trong Authentication.

8. API

8.1 Tạo Conversation

POST /api/conversations

Authentication:

Authorization: Bearer <access-token>

Request:

{
  "title": "REST API"
}

Response:

{
  "id": 1,
  "title": "REST API",
  "createdAt": "2026-09-27T10:00:00Z",
  "updatedAt": "2026-09-27T10:00:00Z"
}

Luồng

Request
   |
   v
JWT
   |
   v
Lấy UserId từ claims
   |
   v
Validate Title
   |
   v
Create Conversation
   |
   v
Save DB
   |
   v
Return 201

Không nhận UserId từ request body.

Không làm:

{
  "userId": 20,
  "title": "REST API"
}

UserId phải lấy từ JWT.

9. Lấy danh sách Conversation

GET /api/conversations

Authentication:

Authorization: Bearer <access-token>

Chỉ trả Conversation của user hiện tại.

Ví dụ User 1:

Conversation 1 -> User 1
Conversation 2 -> User 1
Conversation 3 -> User 2

API của User 1 chỉ trả:

Conversation 1
Conversation 2

Response:

[
  {
    "id": 1,
    "title": "REST API",
    "createdAt": "2026-09-27T10:00:00Z",
    "updatedAt": "2026-09-27T10:10:00Z"
  },
  {
    "id": 2,
    "title": "JWT",
    "createdAt": "2026-09-27T11:00:00Z",
    "updatedAt": "2026-09-27T11:05:00Z"
  }
]

10. Lấy Conversation theo Id

GET /api/conversations/{id}

Authentication:

Authorization: Bearer <access-token>

Bắt buộc kiểm tra ownership

Ví dụ:

Conversation 10
UserId = 1

User 2 request:

GET /api/conversations/10

Không được trả dữ liệu.

Có thể trả:

404 Not Found

hoặc thiết kế:

403 Forbidden

Khuyến nghị MVP:

Không tìm thấy resource thuộc user hiện tại
→ 404 Not Found

Điều này cũng tránh làm lộ việc Conversation đó tồn tại.

11. Xóa Conversation

DELETE /api/conversations/{id}

Authentication:

Authorization: Bearer <access-token>

Phải kiểm tra:

Conversation.UserId == CurrentUserId

Nếu hợp lệ:

Xóa Conversation

và xử lý Messages liên quan.

Khuyến nghị cấu hình cascade delete:

Conversation
    |
    +---- Message
    +---- Message

Khi Conversation bị xóa:

Messages
    ↓
Cũng bị xóa

Nếu không dùng cascade delete, Service phải chủ động xóa Messages trước.

Response:

204 No Content

12. Tạo Message

POST /api/conversations/{conversationId}/messages

Authentication:

Authorization: Bearer <access-token>

Request:

{
  "content": "REST API là gì?"
}

Lưu ý

Module này chỉ lưu Message.

Chưa gọi LLM.

Luồng:

Request
   |
   v
JWT
   |
   v
Lấy CurrentUserId
   |
   v
Get Conversation
   |
   v
Check Conversation.UserId
   |
   v
Validate Content
   |
   v
Create Message
   |
   v
Update Conversation.UpdatedAt
   |
   v
Save DB

13. Không cho client tự ghi assistant message tùy ý

Mặc dù API trên có thể có field role, không nên để client có quyền tùy ý tạo:

{
  "role": "assistant"
}

Trong kiến trúc hoàn chỉnh:

Client
   |
   | user message
   v
AI Gateway
   |
   v
LLM
   |
   v
assistant message

Vì vậy API tạo message ở module này chỉ nhận:

role = user

system và assistant do backend tạo ở các bước AI sau này.

14. Lấy Messages

GET /api/conversations/{conversationId}/messages

Authentication:

Authorization: Bearer <access-token>

Phải kiểm tra Conversation thuộc user hiện tại.

Response:

[
  {
    "id": 1,
    "role": "user",
    "content": "REST API là gì?",
    "model": null,
    "createdAt": "2026-09-27T10:00:00Z"
  },
  {
    "id": 2,
    "role": "assistant",
    "content": "REST API là...",
    "model": "gpt-4.1-mini",
    "createdAt": "2026-09-27T10:00:03Z"
  }
]

Trong module hiện tại sẽ chủ yếu có user messages. assistant sẽ xuất hiện sau khi tích hợp AI.

15. DTOs

Tạo:

DTOs/
└── Conversation/
    ├── CreateConversationDto.cs
    ├── ConversationResponseDto.cs
    ├── MessageCreateDto.cs
    └── MessageResponseDto.cs

CreateConversationDto

public class CreateConversationDto
{
    public string Title { get; set; } = string.Empty;
}

ConversationResponseDto

public class ConversationResponseDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

Kiểu int chỉ là ví dụ. Phải dùng đúng kiểu Id của project.

MessageCreateDto

public class MessageCreateDto
{
    public string Content { get; set; } = string.Empty;
}

Không cần cho client truyền Role trong MVP nếu endpoint này chỉ dùng để tạo user message.

MessageResponseDto

public class MessageResponseDto
{
    public int Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? Model { get; set; }

    public DateTime CreatedAt { get; set; }
}

16. Repository

IConversationRepository

public interface IConversationRepository
{
    Task<List<Conversation>> GetByUserIdAsync(int userId);

    Task<Conversation?> GetByIdAsync(int conversationId);

    Task AddAsync(Conversation conversation);

    Task DeleteAsync(Conversation conversation);
}

Repository không tự quyết định user có quyền hay không.

Repository chỉ query database.

17. IMessageRepository

public interface IMessageRepository
{
    Task<List<Message>> GetByConversationIdAsync(
        int conversationId
    );

    Task AddAsync(Message message);

    Task DeleteByConversationIdAsync(
        int conversationId
    );
}

Tên method có thể thay đổi theo implementation thực tế.

18. Service

IConversationService

public interface IConversationService
{
    Task<ConversationResponseDto> CreateAsync(
        int userId,
        CreateConversationDto request
    );

    Task<List<ConversationResponseDto>> GetMyConversationsAsync(
        int userId
    );

    Task<ConversationResponseDto?> GetByIdAsync(
        int userId,
        int conversationId
    );

    Task DeleteAsync(
        int userId,
        int conversationId
    );
}

19. IMessageService

public interface IMessageService
{
    Task<MessageResponseDto> CreateUserMessageAsync(
        int userId,
        int conversationId,
        MessageCreateDto request
    );

    Task<List<MessageResponseDto>> GetMessagesAsync(
        int userId,
        int conversationId
    );
}

20. Controller

Tạo:

Controllers/
├── ConversationController.cs
└── MessageController.cs

Có thể dùng tên:

ConversationsController
MessagesController

Routes:

/api/conversations
/api/conversations/{id}
/api/conversations/{id}/messages

21. Controller Flow

Conversation

HTTP Request
     |
     v
ConversationController
     |
     v
ConversationService
     |
     v
ConversationRepository
     |
     v
EF Core
     |
     v
SQL Server

Message

HTTP Request
     |
     v
MessageController
     |
     v
MessageService
     |
     +------> ConversationRepository
     |
     +------> MessageRepository
     |
     v
SQL Server

22. Current User

Không nhận UserId từ client.

Lấy UserId từ JWT Claims.

Ví dụ:

var userId = User.FindFirst(
    ClaimTypes.NameIdentifier
)?.Value;

Hoặc sử dụng claim mà TokenService hiện tại đang tạo.

Quan trọng:

Authentication module phải thống nhất tên claim.

Conversation module phải dùng đúng claim đó.

Không tự tạo một claim name khác nếu project hiện tại đã có chuẩn.

23. Authorization Rule

Mọi request tới Conversation phải kiểm tra:

CurrentUserId
        =
Conversation.UserId

Ví dụ:

JWT
UserId = 1

Conversation 10
UserId = 1

Cho phép.

Ngược lại:

JWT
UserId = 1

Conversation 10
UserId = 2

Từ chối.

24. Validation

Conversation title

Không cho:

""
"   "

Có thể giới hạn:

1 - 200 characters

Message content

Không cho:

""
"   "

Có thể giới hạn độ dài phù hợp.

25. HTTP Status

Trường hợp

Status

Tạo Conversation thành công

201

Lấy danh sách thành công

200

Lấy Conversation thành công

200

Tạo Message thành công

201

Xóa thành công

204

Dữ liệu không hợp lệ

400

Không đăng nhập

401

Không có resource thuộc user

404

Lỗi server

500

26. Index Database

Nên có index:

Conversations.UserId
Messages.ConversationId

Mục đích:

GET /api/conversations

query theo UserId.

và:

GET /api/conversations/{id}/messages

query theo ConversationId.

27. Timestamp

Nên lưu thời gian dưới dạng UTC:

DateTime.UtcNow

Không dùng DateTime.Now cho dữ liệu server nếu không có lý do đặc biệt.

Khi hiển thị cho client/frontend, có thể chuyển sang múi giờ cần thiết.

28. Business Rules

Rule 1

User chỉ xem Conversation của chính mình.

Rule 2

User chỉ tạo Message trong Conversation của chính mình.

Rule 3

User chỉ xóa Conversation của chính mình.

Rule 4

Xóa Conversation phải xử lý Messages thuộc Conversation đó.

Rule 5

assistant message không được tạo tùy ý từ client.

Rule 6

UpdatedAt của Conversation phải cập nhật khi Conversation có thay đổi phù hợp, đặc biệt khi có Message mới.

Rule 7

Không gọi LLM trong Conversation Repository.

Rule 8

Không gọi OpenAI trong ConversationController.

29. Testing

Create Conversation

POST /api/conversations
Authorization: Bearer <token>

Expected:

201 Created

Get My Conversations

GET /api/conversations

Expected:

200 OK

Chỉ thấy Conversation của user hiện tại.

Get Conversation

GET /api/conversations/1

Expected:

200 OK

Access Another User's Conversation

Expected:

404 Not Found

Delete Conversation

DELETE /api/conversations/1

Expected:

204 No Content

Create Message

POST /api/conversations/1/messages

Request:

{
  "content": "REST API là gì?"
}

Expected:

201 Created

Get Messages

GET /api/conversations/1/messages

Expected:

200 OK

30. Definition of Done

Module Conversation + Message hoàn thành khi:

[ ] Conversation entity
[ ] Message entity

[ ] Database relationship
[ ] Foreign keys
[ ] Cascade delete hoặc delete strategy
[ ] Index UserId
[ ] Index ConversationId

[ ] Create Conversation
[ ] Get My Conversations
[ ] Get Conversation
[ ] Delete Conversation

[ ] Create User Message
[ ] Get Messages

[ ] JWT protection
[ ] Ownership check
[ ] Validation
[ ] DTOs
[ ] Repository
[ ] Service
[ ] Controller

[ ] Swagger test
[ ] Postman test
[ ] Build thành công

31. Thứ tự triển khai

Thực hiện đúng thứ tự:

1. Conversation Entity
       ↓
2. Message Entity
       ↓
3. ApplicationDbContext
       ↓
4. Migration
       ↓
5. ConversationRepository
       ↓
6. MessageRepository
       ↓
7. ConversationService
       ↓
8. MessageService
       ↓
9. ConversationController
       ↓
10. MessageController
       ↓
11. JWT / CurrentUserId integration
       ↓
12. Ownership validation
       ↓
13. Swagger test
       ↓
14. Postman test
       ↓
15. Build

32. Điều kiện trước khi chuyển sang AI

Chỉ chuyển sang bước LLM Provider + OpenAI khi luồng sau chạy ổn:

Login
  ↓
Access Token
  ↓
Create Conversation
  ↓
Get Conversation
  ↓
Create User Message
  ↓
Get Messages
  ↓
Delete Conversation
  ↓
Database lưu đúng
  ↓
Ownership đúng

Khi đó bước tiếp theo mới là:

ILLMProvider
      ↓
OpenAIProvider
      ↓
AiService
      ↓
POST /api/ai/chat

Module Conversation không được phụ thuộc vào OpenAI.