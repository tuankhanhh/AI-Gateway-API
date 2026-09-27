Authentication Design - AI Gateway

1. Mục tiêu

Module Authentication chịu trách nhiệm:

Đăng ký tài khoản

Đăng nhập

Hash password

Sinh Access Token (JWT)

Sinh Refresh Token

Refresh Access Token

Logout / thu hồi Refresh Token

Xác định người dùng hiện tại qua GET /api/auth/me

Phân quyền cơ bản theo Role

Luồng tổng quát:

Client
   |
   | Register / Login
   v
AuthController
   |
   v
AuthService
   |
   +----> UserRepository ------> SQL Server
   |
   +----> PasswordService
   |
   +----> TokenService

2. Phạm vi Authentication MVP

Không làm ở giai đoạn này:

- OAuth / Google Login
- Facebook Login
- 2FA
- Email verification
- Password reset qua email
- Session distributed
- IdentityServer

Chỉ tập trung vào:

Register
Login
JWT Access Token
Refresh Token
Logout
Get Current User
Role-based Authorization

3. Database liên quan

Authentication sử dụng các bảng:

Users
Roles
UserRoles
RefreshTokens

Quan hệ:

Users
  |
  +---- UserRoles ---- Roles
  |
  +---- RefreshTokens

4. Bảng Users

Users
------------------------------------------------
Id                  int / Guid        PK
Email               nvarchar(...)     UNIQUE
PasswordHash        nvarchar(...)     NOT NULL
FullName            nvarchar(...)     NOT NULL
Status              int / nvarchar    NOT NULL
CreatedAt           datetime2         NOT NULL

Quy tắc:

Email không được trùng.

Password không lưu plain text.

PasswordHash phải là kết quả của password hashing.

Tài khoản mới mặc định có trạng thái hợp lệ/active.

5. Bảng Roles

Roles
------------------------------------------------
Id                  int / Guid        PK
Name                nvarchar(...)     UNIQUE

MVP:

User
Admin

6. Bảng UserRoles

UserRoles
------------------------------------------------
UserId              FK -> Users
RoleId              FK -> Roles

PRIMARY KEY (UserId, RoleId)

Một user có thể có nhiều role.

Ví dụ:

User A
   |
   +---- User
   +---- Admin

MVP có thể chỉ gán một role User cho tài khoản đăng ký bình thường.

7. Bảng RefreshTokens

RefreshTokens
------------------------------------------------
Id                  bigint / Guid    PK
UserId              FK -> Users
TokenHash           nvarchar(...)   NOT NULL
FamilyId            uniqueidentifier / Guid
CreatedAt           datetime2       NOT NULL
ExpiresAt           datetime2       NOT NULL
RevokedAt           datetime2       NULL

Ý nghĩa:

TokenHash: lưu hash của Refresh Token, không lưu token plain text.

FamilyId: xác định các refresh token thuộc cùng một phiên đăng nhập.

ExpiresAt: thời điểm token hết hạn.

RevokedAt: token đã bị thu hồi hay chưa.

8. API Authentication

8.1 Register

POST /api/auth/register

Request:

{
  "email": "user@gmail.com",
  "password": "123456",
  "fullName": "Nguyen Van A"
}

Response:

{
  "message": "Register successful"
}

Xử lý

Request
   |
   v
Validate dữ liệu
   |
   v
Kiểm tra Email đã tồn tại?
   |
   +---- Có ----> 400 Bad Request
   |
   +---- Không
            |
            v
      Hash Password
            |
            v
        Create User
            |
            v
      Gán Role = User
            |
            v
        Save Database
            |
            v
         Response

9. Login

POST /api/auth/login

Request:

{
  "email": "user@gmail.com",
  "password": "123456"
}

Response:

{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 900
}

Xử lý

Request
   |
   v
Tìm User theo Email
   |
   +---- Không có ----> 401 Unauthorized
   |
   v
Verify Password
   |
   +---- Sai ----------> 401 Unauthorized
   |
   v
Lấy Role
   |
   v
Generate Access Token
   |
   v
Generate Refresh Token
   |
   v
Hash Refresh Token
   |
   v
Lưu RefreshToken vào DB
   |
   v
Trả AccessToken + RefreshToken

10. Access Token

Access Token sử dụng JWT.

Token nên chứa các claim cơ bản:

sub / UserId
email
name
role
iat
exp
iss
aud

Ví dụ ý nghĩa:

UserId = 15
Email = user@gmail.com
Role = User

Gateway dùng JWT để biết request hiện tại thuộc user nào.

11. JWT Validation

Mỗi request protected:

Authorization: Bearer <access-token>

ASP.NET Core sẽ kiểm tra:

1. Token có đúng format không?
2. Signature có hợp lệ không?
3. Issuer có đúng không?
4. Audience có đúng không?
5. Token đã hết hạn chưa?

Nếu hợp lệ:

Request
   |
   v
Controller

Nếu không hợp lệ:

401 Unauthorized

12. Refresh Token

Access Token nên có thời gian sống ngắn.

Ví dụ:

Access Token = 15 phút
Refresh Token = 7 ngày

Khi Access Token hết hạn:

Client
   |
   | POST /api/auth/refresh
   | RefreshToken
   v
Gateway
   |
   v
Kiểm tra RefreshToken
   |
   +---- Invalid / Expired / Revoked
   |           |
   |           v
   |       401 Unauthorized
   |
   v
Tạo AccessToken mới
   |
   v
Rotate RefreshToken
   |
   v
Lưu token mới
   |
   v
Trả token mới

13. Refresh Token Rotation

Không sử dụng lại Refresh Token cũ sau khi refresh thành công.

Ví dụ:

RT1
 |
 | refresh
 v
RT2

Sau đó:

RT1 = Revoked
RT2 = Active

Nếu client gửi lại RT1:

RT1
 |
 v
Detected reuse
 |
 v
Thu hồi phiên / Family

14. FamilyId

Các Refresh Token thuộc cùng một phiên đăng nhập có cùng FamilyId.

Ví dụ:

Login
  |
  +---- RT1  FamilyId = F001
           |
           v
         RT2  FamilyId = F001
                  |
                  v
                RT3  FamilyId = F001

Điều này giúp server biết các token thuộc cùng một refresh-token family.

15. Logout

POST /api/auth/logout

Request có thể dùng:

Authorization: Bearer <access-token>

và Refresh Token trong body:

{
  "refreshToken": "..."
}

Xử lý:

RefreshToken
      |
      v
Tìm token trong DB
      |
      v
RevokedAt = DateTime.UtcNow

Nếu muốn logout toàn bộ phiên:

Thu hồi tất cả token có cùng FamilyId

16. Get Current User

GET /api/auth/me

Request:

Authorization: Bearer <access-token>

Response:

{
  "id": 15,
  "email": "user@gmail.com",
  "fullName": "Nguyen Van A",
  "role": "User"
}

Nguồn UserId:

JWT Claims
   |
   v
Controller / Service
   |
   v
UserId

Không nên nhận userId từ body để xác định người đang đăng nhập.

Không làm:

{
  "userId": 20
}

để API tự tin rằng đó là user hiện tại.

17. Authorization

Authentication:

"Bạn là ai?"

Authorization:

"Bạn được phép làm gì?"

Ví dụ:

[Authorize]

Cho phép user đã đăng nhập.

Admin:

[Authorize(Roles = "Admin")]

18. Service cần có

IAuthService

public interface IAuthService
{
    Task RegisterAsync(RegisterDto request);

    Task<LoginResponseDto> LoginAsync(LoginDto request);

    Task<TokenResponseDto> RefreshAsync(RefreshTokenDto request);

    Task LogoutAsync(string refreshToken);

    Task<CurrentUserDto> GetCurrentUserAsync(string userId);
}

AuthService chịu trách nhiệm nghiệp vụ Authentication.

19. Password Service

Interface

public interface IPasswordService
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}

Nhiệm vụ:

Password
   |
   v
Hash
   |
   v
PasswordHash

Khi login:

Password nhập vào
       |
       v
Verify
       |
       v
PasswordHash trong DB

20. Token Service

Interface

public interface ITokenService
{
    string GenerateAccessToken(
        User user,
        List<string> roles
    );

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

Nhiệm vụ:

User
 +
Roles
   |
   v
Access Token

và:

Random Token
    |
    v
Refresh Token
    |
    v
Hash
    |
    v
Database

21. User Repository

Interface

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByIdAsync(int userId);

    Task<bool> ExistsByEmailAsync(string email);

    Task AddAsync(User user);

    Task<List<string>> GetRolesAsync(int userId);
}

Repository chỉ làm việc với database.

Không đặt:

JWT logic
Password hashing
Business rules

vào Repository.

22. Refresh Token Repository

Interface

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);

    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash
    );

    Task RevokeAsync(
        RefreshToken refreshToken
    );

    Task RevokeFamilyAsync(
        Guid familyId
    );
}

23. Controller

AuthController

Các endpoint:

POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me

Controller chỉ:

Receive Request
      |
      v
Call AuthService
      |
      v
Return HTTP Response

Không đặt toàn bộ logic login vào Controller.

24. DTOs

Nên tạo:

DTOs/
└── Auth/
    ├── RegisterDto.cs
    ├── LoginDto.cs
    ├── LoginResponseDto.cs
    ├── RefreshTokenDto.cs
    ├── TokenResponseDto.cs
    └── CurrentUserDto.cs

RegisterDto

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}

LoginDto

public class LoginDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

LoginResponseDto

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }
}

25. Dependency Injection

Đăng ký trong Program.cs:

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<
    IRefreshTokenRepository,
    RefreshTokenRepository
>();

builder.Services.AddScoped<
    IPasswordService,
    PasswordService
>();

builder.Services.AddScoped<
    ITokenService,
    TokenService
>();

Luồng Dependency Injection:

AuthController
      |
      v
IAuthService
      |
      v
AuthService
   |       |        |
   v       v        v
UserRepo  Token    Password

26. Authentication Request Flow

Register

Client
  |
  v
POST /api/auth/register
  |
  v
AuthController
  |
  v
AuthService
  |
  +--> UserRepository
  |
  +--> PasswordService
  |
  v
SQL Server

Login

Client
  |
  v
POST /api/auth/login
  |
  v
AuthController
  |
  v
AuthService
  |
  +--> UserRepository
  |
  +--> PasswordService
  |
  +--> TokenService
  |
  +--> RefreshTokenRepository
  |
  v
SQL Server
  |
  v
AccessToken + RefreshToken

Refresh

Client
  |
  v
POST /api/auth/refresh
  |
  v
AuthController
  |
  v
AuthService
  |
  +--> Hash RefreshToken
  |
  +--> RefreshTokenRepository
  |
  +--> TokenService
  |
  v
New AccessToken + New RefreshToken

27. HTTP Status

Trường hợp

Status

Register thành công

201 Created

Login thành công

200 OK

Refresh thành công

200 OK

Logout thành công

204 No Content

Email đã tồn tại

400 Bad Request

Email/password sai

401 Unauthorized

JWT không hợp lệ

401 Unauthorized

Không có quyền

403 Forbidden

User không tồn tại

404 Not Found

28. Error Response

Nên thống nhất format:

{
  "success": false,
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Email hoặc password không đúng"
  },
  "traceId": "abc123"
}

Một số error code:

EMAIL_ALREADY_EXISTS
INVALID_CREDENTIALS
INVALID_REFRESH_TOKEN
REFRESH_TOKEN_EXPIRED
REFRESH_TOKEN_REVOKED
REFRESH_TOKEN_REUSED
USER_NOT_FOUND
UNAUTHORIZED
FORBIDDEN

29. Bảo mật bắt buộc

Password

Không bao giờ:

Password = "123456"

lưu trực tiếp vào DB.

Phải:

Password
   |
   v
Password Hash

JWT Secret

Không hard-code:

"my-secret-key"

trong source code.

Dùng:

appsettings
User Secrets
Environment Variables

OpenAI API Key

Cũng không commit API key vào GitHub.

30. appsettings.json

Ví dụ:

{
  "AppSettings": {
    "Issuer": "AIGateway",
    "Audience": "AIGatewayClient",
    "SecretKey": "CHANGE_THIS_IN_PRODUCTION"
  }
}

Production không dùng secret mẫu này.

31. Middleware Pipeline

Authentication phải đặt đúng thứ tự:

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Ý nghĩa:

Request
   |
   v
Authentication
   |
   v
Authorization
   |
   v
Controller

32. Kiểm thử Authentication

Register

Test 1 - Thành công

{
  "email": "user1@gmail.com",
  "password": "123456",
  "fullName": "User 1"
}

Expected:

201 Created

Test 2 - Email trùng

Expected:

400 Bad Request

Login

Test 1 - Đúng

Expected:

200 OK
AccessToken
RefreshToken

Test 2 - Password sai

Expected:

401 Unauthorized

Test 3 - Email không tồn tại

Expected:

401 Unauthorized

Me

Có JWT

200 OK

Không JWT

401 Unauthorized

Refresh

Refresh Token đúng

200 OK
New AccessToken
New RefreshToken

Refresh Token hết hạn

401 Unauthorized

Gửi lại Refresh Token cũ

401 Unauthorized

và xử lý reuse theo FamilyId.

Logout

Đúng token

204 No Content

Sau logout:

RefreshToken = revoked

33. Definition of Done

Module Authentication được coi là hoàn thành khi:

[ ] User model hoàn thành
[ ] Role model hoàn thành
[ ] UserRole hoàn thành
[ ] RefreshToken hoàn thành

[ ] Migration thành công
[ ] Register hoạt động
[ ] Password được hash
[ ] Login hoạt động
[ ] JWT hoạt động
[ ] Authorization hoạt động

[ ] Refresh Token hoạt động
[ ] Refresh Token Rotation hoạt động
[ ] FamilyId hoạt động
[ ] Logout hoạt động
[ ] GET /me hoạt động

[ ] Invalid JWT -> 401
[ ] Unauthorized role -> 403
[ ] Error response thống nhất

[ ] Swagger test được
[ ] Postman test được

34. Thứ tự code Authentication

Nên code đúng thứ tự này:

1. User Model
      ↓
2. Role Model
      ↓
3. UserRole Model
      ↓
4. RefreshToken Model
      ↓
5. ApplicationDbContext
      ↓
6. Migration
      ↓
7. PasswordService
      ↓
8. TokenService
      ↓
9. UserRepository
      ↓
10. RefreshTokenRepository
      ↓
11. AuthService
      ↓
12. AuthController
      ↓
13. JWT Authentication trong Program.cs
      ↓
14. Authorization
      ↓
15. /me
      ↓
16. Refresh Token Rotation
      ↓
17. Logout
      ↓
18. Test Swagger
      ↓
19. Test Postman

35. Sau khi Authentication hoàn thành

Không làm AI ngay lập tức.

Tiếp theo nên làm:

Authentication
      ↓
Conversation
      ↓
Message
      ↓
ILLMProvider
      ↓
OpenAIProvider
      ↓
POST /api/ai/chat

Khi đó request AI mới có thể xác định:

User nào?
Conversation nào?

và AiRequestLogs mới lưu được:

UserId
Model
Timestamp
Latency
InputTokens
OutputTokens
Status