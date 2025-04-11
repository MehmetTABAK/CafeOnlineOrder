using Microsoft.EntityFrameworkCore;
using ProjectCafeEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeDataAccess
{
	public class ProjectCafeDbContext : DbContext
	{
		public virtual DbSet<Admin> Admin { get; set; }
		public virtual DbSet<Cafe> Cafe { get; set; }
		public virtual DbSet<MenuCategory> MenuCategory { get; set; }
		public virtual DbSet<Order> Order { get; set; }
		public virtual DbSet<Payment> Payment { get; set; }
		public virtual DbSet<Product> Product { get; set; }
		public virtual DbSet<SubMenuCategory> SubMenuCategory { get; set; }
		public virtual DbSet<Worker> Worker { get; set; }

		public ProjectCafeDbContext(DbContextOptions<ProjectCafeDbContext> options) : base(options)
		{
		}
	}
}
