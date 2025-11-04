using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MbabaneHighlandersBackend2.Model;

public class MbabaneHighlandersBackend2Context : IdentityDbContext<IdentityUser>
{
    public MbabaneHighlandersBackend2Context(DbContextOptions<MbabaneHighlandersBackend2Context> options)
        : base(options)
    {
    }

    public DbSet<Contact> Contact { get; set; } = default!;
    public DbSet<Member> Member { get; set; } = default!;
    public DbSet<Fixture> Fixture { get; set; } = default!;

public DbSet<MbabaneHighlandersBackend2.Model.Product> Product { get; set; } = default!;

public DbSet<MbabaneHighlandersBackend2.Model.News> News { get; set; } = default!;

public DbSet<MbabaneHighlandersBackend2.Model.Order> Order { get; set; } = default!;
}