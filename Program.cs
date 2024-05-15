using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// https://nblumhardt.com/2024/04/serilog-net8-0-minimal/
builder.Services.AddSerilog(logging => logging
    .ReadFrom.Configuration(builder.Configuration));

builder.Services.AddDbContext<DataContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("(default)");
    options.UseSqlite(connectionString);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

MapEntity<Category>("Categories");
MapEntity<Product>("Products");

app.Run();

// Il main finisce qui

void MapEntity<TEntity>(string name)
    where TEntity : class, IHasId
{
    var prefix = $"/{name}";
    var group = app.MapGroup(prefix).WithTags(name);
    var type = typeof(TEntity);

    // Read all.
    group.MapGet("/", async (DataContext context, ILogger<TEntity> logger) =>
    {
        logger.LogInformation("Getting all entities of type {EntityType}", typeof(TEntity).Name);

        var entities = await GetEntities(context).ToListAsync();
        return Results.Ok(entities);
    })
    .WithSummary($"Get all entities of type \"{type.Name}\"")
    .WithDescription($"Get all entities of type \"{type.Name}\"");

    // Read single.
    group.MapGet("/{id:int}", async (DataContext context, int id) =>
    {
        var entity = await GetEntities(context).FindAsync(id);
        return entity is not null ? Results.Ok(entity) : Results.NotFound();
    });

    // Create.
    group.MapPost("/", async (DataContext context, TEntity entity) =>
    {
        await GetEntities(context).AddAsync(entity);
        await context.SaveChangesAsync();

        return Results.Created($"{name}/{entity.Id}", entity);
    });

    // Update
    group.MapPut("/", async (DataContext context, TEntity entity) =>
    {
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return Results.NoContent();
    });

    // Delete
    group.MapDelete("/{id:int}", async (DataContext context, int id) =>
    {
        var entities = GetEntities(context);

        var entity = await entities.FindAsync(id);
        if (entity is null) return Results.NotFound();

        entities.Remove(entity);
        await context.SaveChangesAsync();

        return Results.NoContent();
    });

    DbSet<TEntity> GetEntities(DataContext context) => context.Set<TEntity>();
}

class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
    : base(options)
    { }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();
}

interface IHasId
{
    int Id { get; set; }
}

record Category : IHasId
{
    public Category()
    {
        Products = new List<Product>();
    }

    public int Id { get; set; }

    required public string Name { get; set; }

    public ICollection<Product> Products { get; set; }
}

record Product : IHasId
{
    public Product()
    {
        Categories = new List<Category>();
    }

    public int Id { get; set; }

    required public string Title { get; set; }

    public decimal Price { get; set; }

    public decimal DiscountedPrice { get; set; }

    required public string Description { get; set; }

    required public string Image { get; set; }

    public ICollection<Category> Categories { get; set; }
}