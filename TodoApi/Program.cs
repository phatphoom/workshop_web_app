using TodoApi.Dtos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


var todos = new List<TodoGetDto>
{
    new(1,"Design Backend",true),
    new(2,"Design Frontend",false),
    new(3,"Implement Sleeping",true),
};

app.MapGet("/api/todos", () => Results.Ok(todos));

app.MapGet("/api/todos/{id}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo;
});

app.MapPost("/api/todos", (TodoCreateDto dto) =>
{
    var nextId = todos.Count == 0 ? 1 : todos.Max(t => t.Id) + 1;
    var todo = new TodoGetDto(nextId, dto.Title, false);
    todos.Add(todo);
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

app.Run();
