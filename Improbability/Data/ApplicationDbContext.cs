﻿using System.Diagnostics;
using Improbability.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Improbability.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.LogTo(message => Debug.WriteLine(message));
        }

        public DbSet<RandomItem> RandomItems { get; set; }
        public DbSet<RandomEvent> RandomEvents { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Because this function is an "override",
            // the functions of the superclass must be called explicitly,
            // otherwise they will not be executed and errors will result.
            base.OnModelCreating(builder);

            builder?.Entity<ApplicationUser>()
                .HasMany(a => a.RandomItems)
                .WithOne()
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder?.Entity<RandomEvent>()
                .HasOne<RandomItem>()
                .WithMany()
                .HasForeignKey(r => r.RandomItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
