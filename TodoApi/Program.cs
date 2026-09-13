using Microsoft.EntityFrameworkCore;

using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Dtos;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
});
todoGroup.MapPost("/", async (AppDbContext db, TodoCreateDto dto) =>
{
    // read the last id from the database
    var lastTodo = await db.Todos.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
    var nextId = lastTodo is null ? 1 : lastTodo.Id + 1;

    var todo = new TodoItem
    {
        Id = nextId,
        Title = dto.Title,
        Iscompleted = false,
        CreatedAt = DateTime.UtcNow
    };

    // store in memory 
    db.Todos.Add(todo);
    // store in db
    await db.SaveChangesAsync();

    var todoGetDto = new TodoGetDto(todo.Id, todo.Title, todo.Iscompleted);

    return Results.Created($"/{todo.Id}", todo);
});

#endregion
app.Run();
