using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.DTOs;

namespace CYHCare.Application.Services;

/// <summary>
/// Service handling user registration, credential verification, and token issuance.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user or helper with securely hashed password and generates initial JWT token.
    /// </summary>
    public async Task<AuthResponseDto> RegisterAsync(UserRegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.", nameof(request.Email));
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.", nameof(request.Password));
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("FullName is required.", nameof(request.FullName));
        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new ArgumentException("Phone number is required.", nameof(request.Phone));

        _logger.LogInformation("Processing user registration for email: {Email}, Role: {Role}", request.Email, request.Role);

        // Hash password with BCrypt
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Call repository to execute sp_User_Register
        var user = await _userRepository.RegisterAsync(
            request.FullName.Trim(),
            request.Phone.Trim(),
            request.Email.Trim().ToLowerInvariant(),
            passwordHash,
            request.Role,
            request.HourlyRate
        );

        if (user == null)
        {
            throw new InvalidOperationException("Failed to register user. Unknown database error.");
        }

        // Generate JWT Token
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            HelperId = user.HelperId,
            IsVerified = user.IsVerified,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Authenticates user credentials using BCrypt verification and issues JWT token.
    /// </summary>
    public async Task<AuthResponseDto> LoginAsync(UserLoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Email and password are required.");
        }

        _logger.LogInformation("Authenticating user for email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());

        if (user == null)
        {
            _logger.LogWarning("Authentication failed: User not found for email {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Authentication failed: User account is inactive for {Email}", request.Email);
            throw new UnauthorizedAccessException("Your account is currently disabled. Please contact support.");
        }

        // Verify BCrypt password hash
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Authentication failed: Invalid password for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Generate JWT Token
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        _logger.LogInformation("User {Email} logged in successfully as {Role}", user.Email, user.Role);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            HelperId = user.HelperId,
            IsVerified = user.IsVerified,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Gets current authenticated user details by user ID.
    /// </summary>
    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} was not found.");
        }

        return new UserDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Phone = user.Phone,
            Email = user.Email,
            Role = user.Role,
            IsVerified = user.IsVerified,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            HelperId = user.HelperId,
            KycStatus = user.KycStatus,
            IsOnDuty = user.IsOnDuty
        };
    }
}
