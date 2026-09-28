using LocalCommerce.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace LocalCommerce.Infrastructure.Persistence;

public sealed class CommerceDbContext : DbContext
{
    public CommerceDbContext(DbContextOptions<CommerceDbContext> options) : base(options) { }
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();
    public DbSet<IdempotencyEntity> IdempotencyRecords => Set<IdempotencyEntity>();
    public DbSet<StoreEntity> Stores => Set<StoreEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<CartEntity> Carts => Set<CartEntity>();
    public DbSet<CartLineEntity> CartLines => Set<CartLineEntity>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<OrderEntity>(e => {
            e.ToTable("Orders"); e.HasKey(x=>x.Id); e.Property(x=>x.OrderNumber).HasMaxLength(64).IsRequired();
            e.HasIndex(x=>x.OrderNumber).IsUnique(); e.Property(x=>x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            e.Property(x=>x.CreatedAt).IsRequired(); e.Property(x=>x.UpdatedAt).IsRequired();
            e.HasMany(x=>x.Items).WithOne().HasForeignKey(x=>x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<StoreEntity>().WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<OrderItemEntity>(e => {
            e.ToTable("OrderItems", t => t.HasCheckConstraint("CK_OrderItems_Quantity_Positive", ""Quantity" > 0"));
            e.HasKey(x=>x.Id); e.Property(x=>x.ProductName).HasMaxLength(256).IsRequired();
            e.Property(x=>x.VariantName).HasMaxLength(256); e.Property(x=>x.UnitPrice).HasPrecision(18,2).IsRequired();
            e.Property(x=>x.LineDiscount).HasPrecision(18,2).IsRequired(); e.Property(x=>x.LineTotal).HasPrecision(18,2).IsRequired();
        });
        m.Entity<IdempotencyEntity>(e => {
            e.ToTable("IdempotencyRecords"); e.HasKey(x=>x.Id); e.Property(x=>x.Operation).HasMaxLength(128).IsRequired();
            e.Property(x=>x.IdempotencyKey).HasMaxLength(256).IsRequired(); e.Property(x=>x.RequestFingerprint).HasMaxLength(128).IsRequired();
            e.Property(x=>x.OrderNumber).HasMaxLength(64);
            e.HasIndex(x=>new {x.CustomerId,x.Operation,x.IdempotencyKey}).IsUnique();
        });
        m.Entity<StoreEntity>(e => { e.ToTable("Stores"); e.HasKey(x=>x.Id); });
        m.Entity<ProductEntity>(e => {
            e.ToTable("Products"); e.HasKey(x=>x.Id); e.Property(x=>x.Name).HasMaxLength(256).IsRequired();
            e.Property(x=>x.VariantName).HasMaxLength(256); e.Property(x=>x.UnitPrice).HasPrecision(18,2).IsRequired();
            e.HasOne<StoreEntity>().WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<CartEntity>(e => {
            e.ToTable("Carts"); e.HasKey(x=>x.Id); e.HasOne<StoreEntity>().WithMany().HasForeignKey(x=>x.StoreId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x=>x.Lines).WithOne().HasForeignKey(x=>x.CartId).OnDelete(DeleteBehavior.Cascade);
        });
        m.Entity<CartLineEntity>(e => {
            e.ToTable("CartLines", t => t.HasCheckConstraint("CK_CartLines_Quantity_Positive", ""Quantity" > 0"));
            e.HasKey(x=>x.Id); e.Property(x=>x.VariantName).HasMaxLength(256);
            e.HasOne<ProductEntity>().WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class OrderEntity { public Guid Id {get;set;} public Guid StoreId {get;set;} public string OrderNumber {get;set;}=null!; public OrderStatus Status {get;set;} public DateTimeOffset CreatedAt {get;set;} public DateTimeOffset UpdatedAt {get;set;} public List<OrderItemEntity> Items {get;set;}=[]; }
public sealed class OrderItemEntity { public Guid Id {get;set;} public Guid OrderId {get;set;} public Guid ProductId {get;set;} public Guid StoreId {get;set;} public string ProductName {get;set;}=null!; public string? VariantName {get;set;} public decimal UnitPrice {get;set;} public int Quantity {get;set;} public decimal LineDiscount {get;set;} public decimal LineTotal {get;set;} }
public sealed class IdempotencyEntity { public Guid Id {get;set;} public Guid CustomerId {get;set;} public string Operation {get;set;}=null!; public string IdempotencyKey {get;set;}=null!; public string RequestFingerprint {get;set;}=null!; public Guid? OrderId {get;set;} public string? OrderNumber {get;set;} public DateTimeOffset CreatedAt {get;set;} public DateTimeOffset? CompletedAt {get;set;} }
public sealed class StoreEntity { public Guid Id {get;set;} public bool IsActive {get;set;} }
public sealed class ProductEntity { public Guid Id {get;set;} public Guid StoreId {get;set;} public string Name {get;set;}=null!; public string? VariantName {get;set;} public decimal UnitPrice {get;set;} public bool IsOrderable {get;set;} }
public sealed class CartEntity { public Guid Id {get;set;} public Guid CustomerId {get;set;} public Guid StoreId {get;set;} public bool IsActive {get;set;} public List<CartLineEntity> Lines {get;set;}=[]; }
public sealed class CartLineEntity { public Guid Id {get;set;} public Guid CartId {get;set;} public Guid ProductId {get;set;} public string? VariantName {get;set;} public int Quantity {get;set;} }
