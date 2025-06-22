# FlexKit Coding Standards

**Version**: 2.0  
**Last Updated**: January 2024  
**Applies to**: .NET 9+ / C# 13+

## Table of Contents

1. [Core Principles](#core-principles)
2. [Naming Conventions](#naming-conventions)
3. [Code Organization](#code-organization)
4. [Modern C# Usage](#modern-c-usage)
5. [Async/Await Guidelines](#asyncawait-guidelines)
6. [Null Safety](#null-safety)
7. [Documentation](#documentation)
8. [Testing Standards](#testing-standards)
9. [Performance Guidelines](#performance-guidelines)
10. [Architecture Patterns](#architecture-patterns)

## Core Principles

1. **Clarity over Cleverness** - Write code for humans to read, not just computers to execute
2. **Consistency** - Follow patterns established in the codebase
3. **YAGNI** - Don't add functionality until it's needed
4. **Boy Scout Rule** - Leave code better than you found it
5. **Fail Fast** - Validate early and throw meaningful exceptions

## Naming Conventions

### Basic Rules

| Element | Convention | Example |
|---------|------------|---------|
| Classes, Records | PascalCase | `UserService`, `ConfigurationModule` |
| Interfaces | I + PascalCase | `IUserRepository`, `IFlexConfig` |
| Methods | PascalCase | `GetUserById`, `ValidateInput` |
| Properties | PascalCase | `FirstName`, `IsEnabled` |
| Private fields | _camelCase | `_userRepository`, `_logger` |
| Local variables | camelCase | `userCount`, `isValid` |
| Parameters | camelCase | `userId`, `configSection` |
| Constants | PascalCase | `DefaultTimeout`, `MaxRetries` |
| Type parameters | T + PascalCase | `TEntity`, `TResult` |
| Async methods | Suffix with Async | `GetUserAsync`, `SaveAsync` |

### Specific Guidelines

```csharp
// ✅ Good - Descriptive names
public async Task<User?> GetUserByEmailAsync(string email)
{
    var normalizedEmail = email.ToUpperInvariant();
    return await _userRepository.FindByEmailAsync(normalizedEmail);
}

// ❌ Bad - Unclear abbreviations
public async Task<User?> GetUsrByEml(string e)
{
    var ne = e.ToUpperInvariant();
    return await _repo.FindByEmlAsync(ne);
}
```

### Collection Naming

- Use plural forms: `Users` not `UserList`
- For dictionaries, consider: `UsersByEmail` or `EmailToUserMap`

## Code Organization

### File Structure

1. **One type per file** (exception: tightly coupled private classes)
2. **Filename matches type name**: `UserService.cs` contains `UserService`
3. **Organize by feature, not by type**:
   ```
   ✅ Users/
      ├── UserService.cs
      ├── UserRepository.cs
      └── UserValidator.cs
   
   ❌ Services/
      ├── UserService.cs
      └── OrderService.cs
   ```

### Class Member Organization

```csharp
public class ExampleClass
{
    // 1. Constants and static fields
    private const int DefaultTimeout = 30;
    private static readonly ILogger<ExampleClass> Logger = LogManager.GetLogger<ExampleClass>();
    
    // 2. Fields
    private readonly IUserRepository _userRepository;
    private readonly IValidator<User> _validator;
    
    // 3. Constructors
    public ExampleClass(IUserRepository userRepository, IValidator<User> validator)
    {
        _userRepository = userRepository;
        _validator = validator;
    }
    
    // 4. Properties
    public int TimeoutSeconds { get; init; } = DefaultTimeout;
    
    // 5. Public methods
    public async Task<User?> GetUserAsync(int id) { }
    
    // 6. Protected methods
    protected virtual void OnUserRetrieved(User user) { }
    
    // 7. Private methods
    private void ValidateId(int id) { }
    
    // 8. Nested types (if necessary)
    private sealed class UserComparer : IEqualityComparer<User> { }
}
```

### Size Limits

- **Methods**: Aim for < 20 lines (excluding braces and documentation)
- **Classes**: Aim for < 500 lines
- **Files**: Should not exceed 500 lines

## Modern C# Usage

### Use Modern Language Features

```csharp
// ✅ File-scoped namespaces
namespace FlexKit.Configuration;

// ✅ Primary constructors
public class UserService(IUserRepository repository, ILogger<UserService> logger)
{
    public async Task<User?> GetUserAsync(int id)
    {
        logger.LogInformation("Getting user {UserId}", id);
        return await repository.GetByIdAsync(id);
    }
}

// ✅ Target-typed new
List<User> users = new();
Dictionary<int, User> userMap = new();

// ✅ Pattern matching
public string GetStatusMessage(Status status) => status switch
{
    Status.Active => "User is active",
    Status.Inactive => "User is inactive",
    Status.Suspended => "User is suspended",
    _ => throw new ArgumentOutOfRangeException(nameof(status))
};

// ✅ Collection expressions
int[] numbers = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob", "Charlie"];
```

### Prefer Immutability

```csharp
// ✅ Records for data objects
public record UserDto(int Id, string Name, string Email);

// ✅ Init-only properties
public class Configuration
{
    public string ConnectionString { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
}

// ✅ Readonly collections
public IReadOnlyList<User> Users { get; }
```

### Use `var` When Type Is Obvious

```csharp
// ✅ Type is obvious
var user = new User();
var users = GetUsers();
var count = users.Count;
var name = user.Name;

// ❌ Type is not obvious
var result = Calculate(); // What type is this?
var data = GetData();     // Unclear without context
```

## Async/Await Guidelines

### Always Async

```csharp
// ✅ Async all the way down
public async Task<IActionResult> GetUserAsync(int id)
{
    var user = await _userService.GetUserAsync(id);
    return Ok(user);
}

// ❌ Don't block async code
public IActionResult GetUser(int id)
{
    var user = _userService.GetUserAsync(id).Result; // Can cause deadlocks!
    return Ok(user);
}
```

### ConfigureAwait Usage

```csharp
// ✅ In library code, use ConfigureAwait(false)
public async Task<User?> GetUserAsync(int id)
{
    var data = await _database.QueryAsync(id).ConfigureAwait(false);
    return MapToUser(data);
}

// ✅ In ASP.NET Core, don't use ConfigureAwait
public async Task<IActionResult> GetUserAsync(int id)
{
    var user = await _userService.GetUserAsync(id); // No ConfigureAwait
    return Ok(user);
}
```

### Cancellation Tokens

```csharp
// ✅ Accept and pass cancellation tokens
public async Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default)
{
    var query = "SELECT * FROM Users";
    return await _database.QueryAsync<User>(query, cancellationToken);
}
```

## Null Safety

### Enable Nullable Reference Types

```csharp
// ✅ Be explicit about nullability
public class UserService
{
    // Can return null
    public async Task<User?> FindUserAsync(int id) { }
    
    // Never returns null
    public async Task<List<User>> GetAllUsersAsync() { }
    
    // Parameter can be null
    public void UpdateUser(User user, string? notes = null) { }
}
```

### Null Checking

```csharp
// ✅ Modern null checking
public void ProcessUser(User? user)
{
    // Throw if null with modern syntax
    ArgumentNullException.ThrowIfNull(user);
    
    // Pattern matching
    if (user is { IsActive: true, Email: not null })
    {
        SendEmail(user.Email);
    }
    
    // Null-conditional operators
    var userName = user?.Name ?? "Unknown";
    var length = user?.Name?.Length ?? 0;
}
```

## Documentation

### XML Documentation for Public APIs

```csharp
/// <summary>
/// Retrieves a user by their unique identifier.
/// </summary>
/// <param name="id">The unique identifier of the user.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>The user if found; otherwise, <c>null</c>.</returns>
/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is less than 1.</exception>
/// <example>
/// <code>
/// var user = await userService.GetUserAsync(123);
/// if (user != null)
/// {
///     Console.WriteLine($"Found user: {user.Name}");
/// }
/// </code>
/// </example>
public async Task<User?> GetUserAsync(int id, CancellationToken cancellationToken = default)
{
    if (id < 1)
        throw new ArgumentException("User ID must be greater than 0.", nameof(id));
        
    return await _repository.GetByIdAsync(id, cancellationToken);
}
```

### Inline Comments

```csharp
// ✅ Explain why, not what
// Retry up to 3 times because the external service is occasionally flaky
for (int i = 0; i < 3; i++)
{
    try
    {
        return await CallExternalServiceAsync();
    }
    catch (HttpRequestException) when (i < 2)
    {
        // Swallow exception and retry
    }
}

// ❌ Redundant comment
// Increment i by 1
i++;
```

## Testing Standards

### Test Method Naming

```csharp
public class UserServiceTests
{
    [Fact]
    public async Task GetUserAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = 123;
        var expectedUser = new User { Id = userId, Name = "Test User" };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(expectedUser);
        
        // Act
        var result = await _userService.GetUserAsync(userId);
        
        // Assert
        result.Should().BeEquivalentTo(expectedUser);
    }
    
    [Fact]
    public async Task GetUserAsync_WithInvalidId_ThrowsArgumentException()
    {
        // Arrange
        var invalidId = 0;
        
        // Act & Assert
        await _userService.Invoking(s => s.GetUserAsync(invalidId))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("id");
    }
}
```

## Performance Guidelines

### String Operations

```csharp
// ✅ Use StringBuilder for multiple concatenations
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.AppendLine($"Item: {item}");
}

// ✅ Use string interpolation for readability
var message = $"User {user.Name} logged in at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

// ✅ Use StringComparison for comparisons
if (email.Equals(otherEmail, StringComparison.OrdinalIgnoreCase))
{
    // ...
}
```

### Collection Guidelines

```csharp
// ✅ Specify capacity when size is known
var users = new List<User>(userCount);

// ✅ Use appropriate collection types
HashSet<int> uniqueIds = new();
Dictionary<string, User> usersByEmail = new(StringComparer.OrdinalIgnoreCase);

// ✅ Prefer LINQ methods that don't materialize
var activeUsers = users.Where(u => u.IsActive); // Lazy evaluation
var firstActive = users.FirstOrDefault(u => u.IsActive); // Stops at first match
```

## Architecture Patterns

### Dependency Injection

```csharp
// ✅ Constructor injection for required dependencies
public class OrderService(
    IOrderRepository orderRepository,
    IPaymentService paymentService,
    ILogger<OrderService> logger)
{
    // Implementation
}

// ✅ Options pattern for configuration
public class EmailService(IOptions<EmailSettings> options)
{
    private readonly EmailSettings _settings = options.Value;
}
```

### Separation of Concerns

```csharp
// ✅ Keep business logic in services
public class UserService
{
    public async Task<bool> CanUserLoginAsync(User user)
    {
        return user.IsActive && 
               !user.IsLocked && 
               user.PasswordExpiry > DateTime.UtcNow;
    }
}

// ✅ Keep data access in repositories
public class UserRepository
{
    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM Users WHERE Email = @Email";
        return await _connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
    }
}

// ✅ Keep validation separate
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(u => u.Email).NotEmpty().EmailAddress();
        RuleFor(u => u.Name).NotEmpty().MaximumLength(100);
    }
}
```

### Error Handling

```csharp
// ✅ Create specific exception types
public class UserNotFoundException : Exception
{
    public UserNotFoundException(int userId) 
        : base($"User with ID {userId} was not found.")
    {
        UserId = userId;
    }
    
    public int UserId { get; }
}

// ✅ Use Result pattern for expected failures
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

## Additional Guidelines

### Avoid Magic Numbers/Strings

```csharp
// ❌ Bad
if (user.Age >= 18)
{
    // ...
}

// ✅ Good
private const int MinimumAge = 18;
if (user.Age >= MinimumAge)
{
    // ...
}
```

### Use Meaningful Constants

```csharp
// ✅ Configuration keys
public static class ConfigurationKeys
{
    public const string ConnectionString = "Database:ConnectionString";
    public const string ApiTimeout = "Api:TimeoutSeconds";
}

// Usage
var connectionString = configuration[ConfigurationKeys.ConnectionString];
```

### Enum Usage

```csharp
// ✅ Use enums for fixed sets of values
public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Deleted = 4
}

// ✅ Always validate enum values from external sources
if (!Enum.IsDefined(typeof(UserStatus), status))
{
    throw new ArgumentException($"Invalid status value: {status}");
}
```

---

*These standards are living guidelines. As C# and .NET evolve, so should our practices. Always prioritize code clarity and team consistency over strict adherence to any rule.*