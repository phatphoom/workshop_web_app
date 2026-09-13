using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Dtos;
using Microsoft.IdentityModel.Tokens.Experimental;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Scalar.AspNetCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var todoGroup = app.MapGroup("/api/todos").WithTags();

#region In-Memory Endpoints

// var todos = new List<TodoGetDto>
// {
//     new(1,"Design Backend",true),
//     new(2,"Design Frontend",false),
//     new(3,"Implement Sleeping",true),
// };

// todoGroup.MapGet("/", () => Results.Ok(todos));

// todoGroup.MapGet("/{id}", (int id) =>
// {
//     var todo = todos.FirstOrDefault(t => t.Id == id);
//     return todo;
// });

// todoGroup.MapPost("/", (TodoCreateDto dto) =>
// {
//     var nextId = todos.Count == 0 ? 1 : todos.Max(t => t.Id) + 1;
//     var todo = new TodoGetDto(nextId, dto.Title, false);
//     todos.Add(todo);
//     return Results.Created($"/api/todos/{todo.Id}", todo);
// });

// todoGroup.MapPut("/{id}", (int id, TodoUpdateDto dto) =>
// {
//     try
//     {
//         var index = todos.FindIndex(x => x.Id == id);
//         if (index == -1) return Results.NotFound();

//         todos[index] = todos[index] with
//         {
//             Title = dto.Title,
//             Iscompleted = dto.Iscompleted
//         };

//         return Results.Ok(todos[index]);
//     }
//     catch (Exception ex)
//     {
//         return Results.Problem(ex.Message);
//     }
// });

// todoGroup.MapDelete("/{id}", (int id) =>
// {
//     try
//     {
//         var todo = todos.FirstOrDefault(x => x.Id == id);
//         if (todo is null)
//         {
//             return Results.NotFound($"Todo with id {id} not found");
//         }
//         else if (todo.Id <= 0)  // จะ catch negative IDs ด้วย
//         {
//             Results.NotFound();  // แต่ไม่มี return!
//         }
//         todos.Remove(todo);
//         return Results.NoContent();
//     }
//     catch (ArgumentNullException)
//     {
//         return Results.Problem("message not nulll");
//     }
//     catch (Exception ex)
//     {
//         return Results.NotFound(ex.Message);
//     }

// });

#endregion

#region Database Endpoints
todoGroup.MapGet("/", async (AppDbContext db) =>
{
    var todos = await db.Todos.ToListAsync();
    return todos.Count == 0 ? Results.NotFound() : Results.Ok(todos);
})
.RequireAuthorization();
todoGroup.MapPost("/", async (AppDbContext db, TodoCreateDto dto) =>
{
    // read the last id from the database
    var lastTodo = await db.Todos.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
    var nextId = lastTodo is null ? 1 : lastTodo.Id + 1;

    try
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return Results.Problem("Title is Require");
        }
        var todo = new TodoItem
        {
            Id = nextId,
            Title = dto.Title,
            Iscompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        // store in memory 
        // store in db

        var todoGetDto = new TodoGetDto(todo.Id, todo.Title, todo.Iscompleted);

        return Results.Created($"/{todo.Id}", todoGetDto);
    }
    catch (Exception)
    {
        return Results.Problem("something error");
    }

    // store in memory 
    // store in db

})
.RequireAuthorization();


#endregion

#region Authentication Endpoint
app.MapPost("/api/auth/login", (
    LoginDto login,
    IConfiguration configuration) =>
{
    if (login.Username != "student" || login.Password != "password")
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, login.Username)
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

    var credentials = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: configuration["Jwt:Issuer"],
        audience: configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials);

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new LoginResponseDto(tokenString));
});
#endregion
app.Run();
