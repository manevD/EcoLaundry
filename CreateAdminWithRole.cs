#region License

// // Copyright (c) Grindal-IT. All Right Reserved.
// // Licensed under the Apache License, Version 2.0.

#endregion

using EcoLaundry.Data;
using Microsoft.AspNetCore.Identity;

namespace EcoLaundry;

public class CreateAdminWithRole
{
    #region Methods

    public static async Task Create(IServiceProvider serviceProvider)
    {
        _roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        _userManager =
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();


        await CreateRoleIfNotExists("Admin");
        await CreateRoleIfNotExists("Member");


        await CreateUserIfNotExists(
            email: "admin@email.com",
            password: "admin123!",
            role: "Admin");


        await CreateUserIfNotExists(
            email: "user@email.com",
            password: "user11!",
            role: "Member");
    }


    private static async Task CreateRoleIfNotExists(string roleName)
    {
        bool exists = await _roleManager.RoleExistsAsync(roleName);

        if (!exists)
        {
            var role = new IdentityRole
            {
                Name = roleName
            };

            await _roleManager.CreateAsync(role);
        }
    }



    private static async Task CreateUserIfNotExists(
        string email,
        string password,
        string role)
    {
        var userExist =
            await _userManager.FindByEmailAsync(email);


        if (userExist == null)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };


            IdentityResult result =
                await _userManager.CreateAsync(user, password);


            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }
    }

    #endregion


    #region Fields

    private static RoleManager<IdentityRole> _roleManager = null!;

    private static UserManager<ApplicationUser> _userManager = null!;

    #endregion
}