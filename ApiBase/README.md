# This Library holds shared infrastructure code for all API projects.

### **Add a reference to this library to your API project to enable the following features:**

## GlobalExceptionHandler

This enables handling of all exceptions in the API pipeline in one central handler.

### Usage:

#### Controller-Based APIs:

```csharp
public void ConfigureServices(IServiceCollection services) 
{
    //...
    
    services.AddBaseExceptionHandler();
    
    //...
}


public void Configure(IApplicationBuilder app, IWebHostEnvironment env) 
{
    //...
    
    app.UseExceptionHandler();
    
    //...
}

```

#### Minimal APIs:

```csharp
    //...
    
    builder.Services.AddBaseExceptionHandler();
    
    //...
    
    var app = builder.Build();

    app.UseExceptionHandler();
    
    //...
```

### Exception Handling:

1. The handler logs the exception with the route it occurred in and the path of the request.
2. If the response has already started, the handler logs the exception and lets the framework handle the connection teardown.
3. The handler returns status code 500 (Internal Server Error) with the following message: "Unhandled exception occurred. View error logs on API host."

---

## AuthenticationHandlers

This relies on the built-in ASP.NET Authentication and Authorization tools and enables the usage of [Authorize] and [AllowAnonymous] attributes.

### Usage:

#### 1. Register authentication:

Register one or more authentication handlers. The scheme passed to AddAuthentication(...) is the default scheme.

```csharp
services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)     
    .AddApiKeyAuthentication();
```

Options can be set here as well (check Options Directory for possible options):

```csharp
services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddApiKeyAuthentication(o =>
    {
        o.MailThrottleInterval = TimeSpan.FromMinutes(10);
        o.SendInvalidKeyMail = true;
    });
```

Chain additional authentication handlers if needed, each takes separate options:

```csharp
services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddApiKeyAuthentication(o =>
    {
        o.MailThrottleInterval = TimeSpan.FromMinutes(10);
        o.SendInvalidKeyMail = true;
    })
    .AddQueryPasswordAuthentication(o =>
    {
        o.QueryParameterName = "password";
    });
```

#### 2. Register authorization:

FallbackPolicy sets the authorization level for all endpoints/controllers without a specific attribute.

**Single Handler** (no named policies needed):

```csharp
services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme)
        .RequireAuthenticatedUser()
        .Build());
```

**Multiple handlers** (named policy per scheme so endpoints can choose):

```csharp
var apiKeyPolicy = new AuthorizationPolicyBuilder()
    .AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme)
    .RequireAuthenticatedUser()
    .Build();

services.AddAuthorizationBuilder()
    .AddPolicy("ApiKey", apiKeyPolicy)
    .AddPolicy("QueryPass", policy => policy
        .AddAuthenticationSchemes(QueryPasswordAuthenticationOptions.DefaultScheme)
        .RequireAuthenticatedUser())
    .SetFallbackPolicy(apiKeyPolicy); // Fallback can only use one policy -> every unattributed endpoint uses this
```

#### 3. Add pipeline calls:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

#### 4. Mark Endpoints/Controllers:

**Controller-Based APIs:**

Attributes can be added to controllers for all endpoints or to specific endpoints. Adding an Attribute to an endpoints overrides controller-level attributes.

```csharp
// restricted, uses the fallback/default scheme
[Authorize]
[ApiController]
[Route("<route>")]
public class ApiController : ControllerBase { }

// public
[AllowAnonymous]
[ApiController]
[Route("<route>")]
public class ApiController : ControllerBase { }

// restricted to a specific named policy (if named policies are used)
[Authorize("ApiKey")]
[ApiController]
[Route("<route>")]
public class ApiController : ControllerBase { }
```

**Minimal APIs:**

Extension methods can be used to mark specific endpoints/controllers.

```csharp
// restricted, uses the fallback/default scheme
app.Map("/<route>", handler);

// public
app.Map("/<route>", handler).AllowAnonymous();

// restricted to a specific named policy (if named policies are used)
app.Map("/<route>", handler).RequireAuthorization("ApiKey");
```

Minimal APIs also support grouping:

```csharp
var privateGroup = app.MapGroup("/private").RequireAuthorization("QueryPass");
privateGroup.Map("/<route1", handler);
privateGroup.Map("/<route2>", handler);
```

### Requirements:

To allow the AuthenticationHandlers to work, the appSettings.json file must contain the following keys in specific sections:

```json
{
  "AppSettings": {
    "ProjectName": "<ProjectName>",
    "Secret_APIKey":"<APIKey for .AddApiKeyAuthentication>",
    "Secret_ApiPassword": "<APIPassword for .AddQueryPasswordAuthentication>"
  },
  "MailSettings": {
    "MailServer": "smtp.strato.de",
    "MailServiceUsername": "",
    "Secret_MailServicePassword": "<Mailpassword>"
  }
}
```