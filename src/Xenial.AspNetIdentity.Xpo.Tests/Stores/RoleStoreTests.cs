﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;

using DevExpress.Xpo;

using FluentAssertions;

using Microsoft.AspNetCore.Identity;

using Xenial.AspNetIdentity.Xpo.Models;
using Xenial.AspNetIdentity.Xpo.Stores;

using static Xenial.Tasty;

namespace Xenial.AspNetIdentity.Xpo.Tests.Stores
{
    public static class RoleStoreTests
    {
        public static void Tests(string dbName, string connectionString) => Describe($"{nameof(XPRoleStore<IdentityRole, string, IdentityUserRole<string>, IdentityRoleClaim<string>, XpoIdentityRole, XpoIdentityUser, XpoIdentityRoleClaim>)} using {dbName}", () =>
        {
            var dataLayer = XpoDefault.GetDataLayer(connectionString, DevExpress.Xpo.DB.AutoCreateOption.DatabaseAndSchema);

            UnitOfWork unitOfWorkFactory() => new UnitOfWork(dataLayer);

            (XPRoleStore<IdentityRole, string, IdentityUserRole<string>, IdentityRoleClaim<string>, XpoIdentityRole, XpoIdentityUser, XpoIdentityRoleClaim> store, UnitOfWork uow) CreateStore()
            {
                var uow = new UnitOfWork(dataLayer);
                var store = new XPRoleStore<
                    IdentityRole,
                    string,
                    IdentityUserRole<string>,
                    IdentityRoleClaim<string>,
                    XpoIdentityRole,
                    XpoIdentityUser,
                    XpoIdentityRoleClaim
                >(
                    uow,
                    new FakeLogger<XPRoleStore<IdentityRole, string, IdentityUserRole<string>, IdentityRoleClaim<string>, XpoIdentityRole, XpoIdentityUser, XpoIdentityRoleClaim>>(),
                    new IdentityErrorDescriber()
                );
                return (store, uow);
            }

            XpoIdentityRole CreateRole(UnitOfWork uow, string id = null) => new XpoIdentityRole(uow)
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
                NormalizedName = Guid.NewGuid().ToString(),
            };

            It("CreateAsync", async () =>
            {
                var id = Guid.NewGuid().ToString();
                var name = Guid.NewGuid().ToString();
                var normalizedName = Guid.NewGuid().ToString();

                var (store, uow) = CreateStore();
                using (uow)
                using (store)
                {
                    var result = await store.CreateAsync(new IdentityRole
                    {
                        Id = id,
                        Name = name,
                        NormalizedName = normalizedName
                    }, CancellationToken.None);
                    return result == IdentityResult.Success;
                }
            });

            It("DeleteAsync", async () =>
            {
                using var uow1 = unitOfWorkFactory();
                var role = CreateRole(uow1);
                await uow1.SaveAsync(role, CancellationToken.None);
                await uow1.CommitChangesAsync(CancellationToken.None);

                var (store, uow) = CreateStore();
                using (uow)
                using (store)
                {
                    var result = await store.DeleteAsync(new IdentityRole
                    {
                        Id = role.Id
                    }, CancellationToken.None);
                    result.Should().Be(IdentityResult.Success);
                }

                using var uow2 = unitOfWorkFactory();
                var roleInDb = await uow2.GetObjectByKeyAsync<XpoIdentityRole>(role.Id);
                return roleInDb == null;
            });

            It("UpdateAsync", async () =>
            {
                using var uow1 = unitOfWorkFactory();
                var role = CreateRole(uow1);
                await uow1.SaveAsync(role, CancellationToken.None);
                await uow1.CommitChangesAsync(CancellationToken.None);

                var newName = Guid.NewGuid().ToString();
                var (store, uow) = CreateStore();
                using (uow)
                using (store)
                {
                    var result = await store.UpdateAsync(new IdentityRole
                    {
                        Id = role.Id,
                        Name = newName,
                    }, CancellationToken.None);
                    result.Should().Be(IdentityResult.Success);
                }

                using var uow2 = unitOfWorkFactory();
                var roleInDb = await uow2.GetObjectByKeyAsync<XpoIdentityRole>(role.Id);
                roleInDb.Name.Should().Be(newName);
            });

            It("FindByIdAsync", async () =>
            {
                using var uow1 = unitOfWorkFactory();
                var role = CreateRole(uow1);
                await uow1.SaveAsync(role, CancellationToken.None);
                await uow1.CommitChangesAsync(CancellationToken.None);

                var newName = Guid.NewGuid().ToString();
                var (store, uow) = CreateStore();
                using (uow)
                using (store)
                {
                    var result = await store.FindByIdAsync(role.Id, CancellationToken.None);
                    result.Should().NotBeNull();
                    result.Name.Should().Be(role.Name);
                }
            });

            It("FindByNameAsync", async () =>
            {
                using var uow1 = unitOfWorkFactory();
                var role = CreateRole(uow1);
                await uow1.SaveAsync(role, CancellationToken.None);
                await uow1.CommitChangesAsync(CancellationToken.None);

                var newName = Guid.NewGuid().ToString();
                var (store, uow) = CreateStore();
                using (uow)
                using (store)
                {
                    var result = await store.FindByNameAsync(role.NormalizedName, CancellationToken.None);
                    result.Should().NotBeNull();
                    result.Name.Should().Be(role.Name);
                }
            });

            Describe("Claims", () =>
            {
                It("Add Claim", async () =>
                {
                    using var uow1 = unitOfWorkFactory();
                    var role = CreateRole(uow1);
                    await uow1.SaveAsync(role, CancellationToken.None);
                    await uow1.CommitChangesAsync(CancellationToken.None);

                    var claimType = Guid.NewGuid().ToString();
                    var claimValue = Guid.NewGuid().ToString();

                    var (store, uow) = CreateStore();
                    using (uow)
                    using (store)
                    {
                        var identityRole = await store.FindByIdAsync(role.Id, CancellationToken.None);

                        await store.AddClaimAsync(identityRole, new Claim(claimType, claimValue), CancellationToken.None);
                        await store.UpdateAsync(identityRole, CancellationToken.None);
                    }

                    using var uow2 = unitOfWorkFactory();
                    var roleInDb = await uow2.GetObjectByKeyAsync<XpoIdentityRole>(role.Id);
                    roleInDb.Claims.Should().NotBeEmpty();
                    roleInDb.Claims.First().Type.Should().Be(claimType);
                    roleInDb.Claims.First().Value.Should().Be(claimValue);
                });

                It("List Claims", async () =>
                {
                    using var uow1 = unitOfWorkFactory();
                    var role = CreateRole(uow1);
                    var claim = new XpoIdentityRoleClaim(uow1)
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = Guid.NewGuid().ToString(),
                        Value = Guid.NewGuid().ToString(),
                    };
                    role.Claims.Add(claim);
                    await uow1.SaveAsync(role, CancellationToken.None);
                    await uow1.CommitChangesAsync(CancellationToken.None);

                    var (store, uow) = CreateStore();
                    using (uow)
                    using (store)
                    {
                        var identityRole = await store.FindByIdAsync(role.Id, CancellationToken.None);

                        var claims = await store.GetClaimsAsync(identityRole, CancellationToken.None);
                        claims.Should().NotBeEmpty();
                        claims.First().Type.Should().Be(claim.Type);
                        claims.First().Value.Should().Be(claim.Value);
                    }
                });

                It("remove Claim", async () =>
                {
                    using var uow1 = unitOfWorkFactory();
                    var role = CreateRole(uow1);
                    var claim = new XpoIdentityRoleClaim(uow1)
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = Guid.NewGuid().ToString(),
                        Value = Guid.NewGuid().ToString(),
                    };
                    role.Claims.Add(claim);
                    await uow1.SaveAsync(role, CancellationToken.None);
                    await uow1.CommitChangesAsync(CancellationToken.None);

                    var (store, uow) = CreateStore();
                    using (uow)
                    using (store)
                    {
                        var identityRole = await store.FindByIdAsync(role.Id, CancellationToken.None);

                        await store.RemoveClaimAsync(identityRole, new Claim(claim.Type, claim.Value), CancellationToken.None);

                        await store.UpdateAsync(identityRole, CancellationToken.None);
                    }

                    using var uow2 = unitOfWorkFactory();
                    var roleInDb = await uow2.GetObjectByKeyAsync<XpoIdentityRole>(role.Id);
                    roleInDb.Claims.Should().BeEmpty();
                });
            });
        });
    }
}
