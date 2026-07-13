using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Basket.Data.Configurations;
internal class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.HasKey(e => e.Id); // sets primary key (aslında gerek yok)

        builder.HasIndex(e => e.UserName)
               .IsUnique();

        builder.Property(e => e.UserName)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(si => si.ShoppingCartId);
    }
}
