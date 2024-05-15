using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("(default)");
    options.UseSqlite(connectionString);
});

var app = builder.Build();

var group = app.MapGroup("/categories");

// Read.
group.MapGet("/", async (DataContext context) =>
{
    var categories = await context.Categories.ToListAsync();
    return Results.Ok(categories);
});

// Read.
group.MapGet("/{id:int}", async (DataContext context, int id) =>
{
    var category = await context.Categories.FindAsync(id);
    return category is not null ? Results.Ok(category) : Results.NotFound();
});

// Create.
group.MapPost("/", async (DataContext context, Category category) =>
{
    await context.Categories.AddAsync(category);
    await context.SaveChangesAsync();

    return Results.Created($"/categories/{category.Id}", category);
});

app.Run();

class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
    : base(options)
    { }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();
}

record Category
{
    public Category()
    {
        Products = new List<Product>();
    }

    public int Id { get; set; }

    required public string Name { get; set; }

    public ICollection<Product> Products { get; set; }
}

record Product
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