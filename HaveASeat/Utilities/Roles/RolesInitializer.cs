using HaveASeat.Utilities.Constants;
using Microsoft.AspNetCore.Identity;

namespace HaveASeat.Utilities.Roles
{
	public static class RolesInitializer
	{
		public static async Task InitializeRoles(IServiceProvider serviceProvider)
		{
			var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

			if (!await roleManager.RoleExistsAsync(RolesConstants.Admin))
			{
				var adminRole = new IdentityRole(RolesConstants.Admin)
				{
					Id = RolesConstants.AdminId.ToString()
				};
				await roleManager.CreateAsync(adminRole);
			}

			if (!await roleManager.RoleExistsAsync(RolesConstants.User))
			{
				var userRole = new IdentityRole(RolesConstants.User)
				{
					Id = RolesConstants.UserId.ToString()
				};
				await roleManager.CreateAsync(userRole);
			}

			if (!await roleManager.RoleExistsAsync(RolesConstants.Partner))
			{
				var partnerRole = new IdentityRole(RolesConstants.Partner)
				{
					Id = RolesConstants.PartnerId.ToString()
				};
				await roleManager.CreateAsync(partnerRole);
			}

			if (!await roleManager.RoleExistsAsync(RolesConstants.Manager))
			{
				var managerRole = new IdentityRole(RolesConstants.Manager)
				{
					Id = RolesConstants.ManagerId.ToString()
				};
				await roleManager.CreateAsync(managerRole);
			}
		}

	}
}
