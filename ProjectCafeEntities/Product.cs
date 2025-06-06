﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCafeEntities
{
	public class Product
	{
		public Guid Id { get; set; }
		public Guid MenuCategoryId { get; set; }
		public Guid? SubMenuCategoryId { get; set; }
		public string Name { get; set; }
		public string? Image { get; set; }
		public double Price { get; set; }
		public bool Stock { get; set; }
		public int? StockCount { get; set; }
		public bool IsThereDiscount { get; set; }
		public double? DiscountRate { get; set; }
		public bool Active { get; set; }
		public Guid RegistrationUser { get; set; }
		public string RegistrationUserRole { get; set; }
		public DateTime RegistrationDate { get; set; }
		public Guid? CorrectionUser { get; set; }
		public string? CorrectionUserRole { get; set; }
		public DateTime? CorrectionDate { get; set; }

		public virtual SubMenuCategory? SubMenuCategory { get; set; }
		public virtual MenuCategory? MenuCategory { get; set; }
		public virtual ICollection<Order>? Orders { get; set; }
	}
}
