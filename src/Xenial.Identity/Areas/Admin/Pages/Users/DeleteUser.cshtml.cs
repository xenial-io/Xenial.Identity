﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Xenial.Identity.Areas.Admin.Pages.Users
{
    public class DeleteUserModel : PageModel
    {
        private readonly RoleManager<IdentityRole> roleManager;

        public DeleteUserModel(RoleManager<IdentityRole> roleManager)
            => this.roleManager = roleManager;

        public class RoleOutputModel
        {
            public string Name { get; set; }
        }

        public RoleOutputModel Input { get; set; }

        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGet([FromRoute] string id)
        {
            if (Input == null)
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    StatusMessage = "Cannot find role";
                    return Page();
                }
                if (role != null)
                {
                    Input = new RoleOutputModel
                    {
                        Name = role.Name
                    };
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPost([FromRoute] string id)
        {
            if (ModelState.IsValid)
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    StatusMessage = "Cannot find role";
                    return Page();
                }
                if (role.Name == "Administrator")
                {
                    StatusMessage = "Cannot delete 'Administrator' role";
                    Input = new RoleOutputModel
                    {
                        Name = role.Name
                    };
                    return Page();
                }
                var result = await roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    return Redirect("/Admin/Roles");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Description, error.Description);
                    }
                    StatusMessage = "Error deleting role";
                    return Page();
                }
            }

            StatusMessage = "Error: Check Validation";

            return Page();
        }
    }
}