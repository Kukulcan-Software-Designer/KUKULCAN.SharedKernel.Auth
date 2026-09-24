using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Configures the persisted local authentication user.</summary>
public sealed class AuthUserEntityConfiguration : IEntityTypeConfiguration<AuthUserEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuthUserEntity> builder)
    {
        builder.HasKey(entity => entity.UserId);

        builder.Property(entity => entity.Email)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(entity => entity.Email)
            .IsUnique();

        builder.Property(entity => entity.PasswordHash)
            .IsRequired();

        builder.HasMany(entity => entity.TenantMemberships)
            .WithOne(entity => entity.User)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Configures persisted tenant memberships for local authentication users.</summary>
public sealed class AuthTenantMembershipEntityConfiguration : IEntityTypeConfiguration<AuthTenantMembershipEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuthTenantMembershipEntity> builder)
    {
        builder.HasKey(entity => new { entity.UserId, entity.TenantId });
    }
}


/// <summary>Configures persisted federated identities linked to local authentication users.</summary>
public sealed class AuthFederatedIdentityEntityConfiguration : IEntityTypeConfiguration<AuthFederatedIdentityEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuthFederatedIdentityEntity> builder)
    {
        builder.HasKey(entity => new { entity.Provider, entity.Subject });

        builder.Property(entity => entity.Provider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(entity => entity.Subject)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.FederatedIdentities)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
