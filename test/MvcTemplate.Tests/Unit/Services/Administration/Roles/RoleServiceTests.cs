using AutoMapper;
using AutoMapper.QueryableExtensions;
using MvcTemplate.Components.Extensions;
using MvcTemplate.Components.Security;
using MvcTemplate.Data.Core;
using MvcTemplate.Objects;
using MvcTemplate.Resources;
using MvcTemplate.Resources.Permission;
using MvcTemplate.Services;
using MvcTemplate.Tests.Data;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MvcTemplate.Tests.Unit.Services
{
    public class RoleServiceTests : IDisposable
    {
        private TestingContext context;
        private RoleService service;
        private Role role;

        public RoleServiceTests()
        {
            context = new TestingContext();
            Authorization.Provider = Substitute.For<IAuthorizationProvider>();
            service = Substitute.ForPartsOf<RoleService>(new UnitOfWork(context));

            context.DropData();
            SetUpData();
        }
        public void Dispose()
        {
            Authorization.Provider = null;
            context.Dispose();
            service.Dispose();
        }

        #region SeedPermissions(RoleView view)

        [Fact]
        public void SeedPermissions_FirstDepth()
        {
            RoleView view = ObjectFactory.CreateRoleView();

            service.SeedPermissions(view);

            using (IEnumerator<JsTreeNode> expected = CreatePermissions().Nodes.GetEnumerator())
            using (IEnumerator<JsTreeNode> actual = view.Permissions.Nodes.GetEnumerator())
            {
                while (expected.MoveNext() | actual.MoveNext())
                {
                    Assert.Equal(expected.Current.Id, actual.Current.Id);
                    Assert.Equal(expected.Current.Title, actual.Current.Title);
                    Assert.Equal(expected.Current.Nodes.Count, actual.Current.Nodes.Count);
                }
            }
        }

        [Fact]
        public void SeedPermissions_SecondDepth()
        {
            RoleView view = ObjectFactory.CreateRoleView();

            service.SeedPermissions(view);

            using (IEnumerator<JsTreeNode> expected = CreatePermissions().Nodes.SelectMany(node => node.Nodes).GetEnumerator())
            using (IEnumerator<JsTreeNode> actual = view.Permissions.Nodes.SelectMany(node => node.Nodes).GetEnumerator())
            {
                while (expected.MoveNext() | actual.MoveNext())
                {
                    Assert.Equal(expected.Current.Id, actual.Current.Id);
                    Assert.Equal(expected.Current.Title, actual.Current.Title);
                    Assert.Equal(expected.Current.Nodes.Count, actual.Current.Nodes.Count);
                }
            }
        }

        [Fact]
        public void SeedPermissions_ThirdDepth()
        {
            RoleView view = ObjectFactory.CreateRoleView();

            service.SeedPermissions(view);

            using (IEnumerator<JsTreeNode> expected = CreatePermissions().Nodes.SelectMany(node => node.Nodes).SelectMany(node => node.Nodes).GetEnumerator())
            using (IEnumerator<JsTreeNode> actual = view.Permissions.Nodes.SelectMany(node => node.Nodes).SelectMany(node => node.Nodes).GetEnumerator())
            {
                while (expected.MoveNext() | actual.MoveNext())
                {
                    Assert.Equal(expected.Current.Id, actual.Current.Id);
                    Assert.Equal(expected.Current.Title, actual.Current.Title);
                    Assert.Equal(expected.Current.Nodes.Count, actual.Current.Nodes.Count);
                }
            }
        }

        [Fact]
        public void SeedPermissions_BranchesWithoutId()
        {
            RoleView view = ObjectFactory.CreateRoleView();

            service.SeedPermissions(view);

            IEnumerable<JsTreeNode> nodes = view.Permissions.Nodes;
            IEnumerable<JsTreeNode> branches = GetAllBranchNodes(nodes);

            Assert.Empty(branches.Where(branch => branch.Id != null));
        }

        [Fact]
        public void SeedPermissions_LeafsWithId()
        {
            RoleView view = ObjectFactory.CreateRoleView();

            service.SeedPermissions(view);

            IEnumerable<JsTreeNode> nodes = view.Permissions.Nodes;
            IEnumerable<JsTreeNode> leafs = GetAllLeafNodes(nodes);

            Assert.Empty(leafs.Where(leaf => leaf.Id == null));
        }

        #endregion

        #region GetViews()

        [Fact]
        public void GetViews_ReturnsRoleViews()
        {
            using (IEnumerator<RoleView> actual = service.GetViews().GetEnumerator())
            using (IEnumerator<RoleView> expected = context
                .Set<Role>()
                .ProjectTo<RoleView>()
                .OrderByDescending(view => view.Id)
                .GetEnumerator())
            {
                while (expected.MoveNext() | actual.MoveNext())
                {
                    Assert.Equal(expected.Current.Permissions.SelectedIds, actual.Current.Permissions.SelectedIds);
                    Assert.Equal(expected.Current.CreationDate, actual.Current.CreationDate);
                    Assert.Equal(expected.Current.Title, actual.Current.Title);
                    Assert.Equal(expected.Current.Id, actual.Current.Id);
                }
            }
        }

        #endregion

        #region GetView(Int32 id)

        [Fact]
        public void GetView_NoRole_ReturnsNull()
        {
            Assert.Null(service.GetView(0));
        }

        [Fact]
        public void GetView_ReturnsViewById()
        {
            service.When(sub => sub.SeedPermissions(Arg.Any<RoleView>())).DoNotCallBase();

            RoleView expected = Mapper.Map<RoleView>(role);
            RoleView actual = service.GetView(role.Id);

            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.NotEmpty(actual.Permissions.SelectedIds);
            Assert.Equal(expected.Title, actual.Title);
            Assert.Equal(expected.Id, actual.Id);
        }

        [Fact]
        public void GetView_SetsSelectedIds()
        {
            service.When(sub => sub.SeedPermissions(Arg.Any<RoleView>())).DoNotCallBase();

            IEnumerable<Int32> expected = role.Permissions.Select(rolePermission => rolePermission.PermissionId).OrderBy(id => id);
            IEnumerable<Int32> actual = service.GetView(role.Id).Permissions.SelectedIds.OrderBy(id => id);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetView_SeedsPermissions()
        {
            service.When(sub => sub.SeedPermissions(Arg.Any<RoleView>())).DoNotCallBase();

            RoleView view = service.GetView(role.Id);

            service.Received().SeedPermissions(view);
        }

        #endregion

        #region Create(RoleView view)

        [Fact]
        public void Create_Role()
        {
            RoleView view = ObjectFactory.CreateRoleView(1);

            service.Create(view);

            Role actual = context.Set<Role>().AsNoTracking().Single(model => model.Id != role.Id);
            RoleView expected = view;

            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.Title, actual.Title);
        }

        [Fact]
        public void Create_RolePermissions()
        {
            RoleView view = ObjectFactory.CreateRoleView(1);
            view.Permissions = CreatePermissions();

            service.Create(view);

            IEnumerable<Int32> expected = view.Permissions.SelectedIds.OrderBy(permissionId => permissionId);
            IEnumerable<Int32> actual = context
                .Set<Role>()
                .AsNoTracking()
                .Single(model => model.Id != role.Id)
                .Permissions
                .Select(rolePermission => rolePermission.PermissionId)
                .OrderBy(permissionId => permissionId);

            Assert.Equal(expected, actual);
        }

        #endregion

        #region Edit(RoleView view)

        [Fact]
        public void Edit_Role()
        {
            RoleView view = ObjectFactory.CreateRoleView(role.Id);
            view.Title = role.Title += "Test";

            service.Edit(view);

            Role actual = context.Set<Role>().AsNoTracking().Single();
            Role expected = role;

            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.Title, actual.Title);
            Assert.Equal(expected.Id, actual.Id);
        }

        [Fact]
        public void Edit_RolePermissions()
        {
            Permission permission = ObjectFactory.CreatePermission();
            context.Set<Permission>().Add(permission);
            context.SaveChanges();

            RoleView view = ObjectFactory.CreateRoleView(role.Id);
            view.Permissions = CreatePermissions();
            view.Permissions.SelectedIds.Add(permission.Id);
            view.Permissions.SelectedIds.RemoveAt(0);

            service.Edit(view);

            IEnumerable<Int32> actual = context.Set<RolePermission>().AsNoTracking().Select(rolePermission => rolePermission.PermissionId).OrderBy(permissionId => permissionId);
            IEnumerable<Int32> expected = view.Permissions.SelectedIds.OrderBy(permissionId => permissionId);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Edit_RefreshesAuthorization()
        {
            service.Edit(ObjectFactory.CreateRoleView(role.Id));

            Authorization.Provider.Received().Refresh();
        }

        #endregion

        #region Delete(Int32 id)

        [Fact]
        public void Delete_NullsAccountRoles()
        {
            Account account = ObjectFactory.CreateAccount();
            account.RoleId = role.Id;
            account.Role = null;

            context.Set<Account>().Add(account);
            context.SaveChanges();

            service.Delete(role.Id);

            Assert.NotEmpty(context.Set<Account>().Where(model => model.Id == account.Id && model.RoleId == null));
        }

        [Fact]
        public void Delete_Role()
        {
            service.Delete(role.Id);

            Assert.Empty(context.Set<Role>());
        }

        [Fact]
        public void Delete_RolePermissions()
        {
            service.Delete(role.Id);

            Assert.Empty(context.Set<RolePermission>());
        }

        [Fact]
        public void Delete_RefreshesAuthorization()
        {
            service.Delete(role.Id);

            Authorization.Provider.Received().Refresh();
        }

        #endregion

        #region Test helpers

        private void SetUpData()
        {
            role = ObjectFactory.CreateRole();
            foreach (String controller in new[] { "Roles", "Profile" })
                foreach (String action in new[] { "Edit", "Delete" })
                {
                    RolePermission rolePermission = ObjectFactory.CreateRolePermission(role.Permissions.Count + 1);
                    rolePermission.Permission.Area = controller == "Roles" ? "Administration" : null;
                    rolePermission.Permission.Controller = controller;
                    rolePermission.Permission.Action = action;
                    rolePermission.RoleId = role.Id;
                    rolePermission.Role = null;

                    role.Permissions.Add(rolePermission);
                }

            using (TestingContext testingContext = new TestingContext())
            {
                testingContext.Set<Role>().Add(role);
                testingContext.SaveChanges();
            }
        }

        private JsTree CreatePermissions()
        {
            JsTreeNode root = new JsTreeNode(Titles.All);
            JsTree expectedTree = new JsTree();

            expectedTree.Nodes.Add(root);
            expectedTree.SelectedIds = role.Permissions.Select(rolePermission => rolePermission.PermissionId).ToList();

            IEnumerable<Permission> permissions = role
                .Permissions
                .Select(rolePermission => rolePermission.Permission)
                .Select(permission => new Permission
                {
                    Id = permission.Id,
                    Area = ResourceProvider.GetPermissionAreaTitle(permission.Area),
                    Controller = ResourceProvider.GetPermissionControllerTitle(permission.Area, permission.Controller),
                    Action = ResourceProvider.GetPermissionActionTitle(permission.Area, permission.Controller, permission.Action)
                });

            foreach (IGrouping<String, Permission> area in permissions.GroupBy(permission => permission.Area).OrderBy(permission => permission.Key ?? permission.FirstOrDefault().Controller))
            {
                JsTreeNode areaNode = new JsTreeNode(area.Key);
                foreach (IGrouping<String, Permission> controller in area.GroupBy(permission => permission.Controller).OrderBy(permission => permission.Key))
                {
                    JsTreeNode controllerNode = new JsTreeNode(controller.Key);
                    foreach (Permission permission in controller.OrderBy(permission => permission.Action))
                        controllerNode.Nodes.Add(new JsTreeNode(permission.Id, permission.Action));

                    if (areaNode.Title == null)
                        root.Nodes.Add(controllerNode);
                    else
                        areaNode.Nodes.Add(controllerNode);
                }

                if (areaNode.Title != null)
                    root.Nodes.Add(areaNode);
            }

            return expectedTree;
        }

        private IEnumerable<JsTreeNode> GetAllLeafNodes(IEnumerable<JsTreeNode> nodes)
        {
            List<JsTreeNode> leafs = nodes.Where(node => node.Nodes.Count == 0).ToList();
            IEnumerable<JsTreeNode> branches = nodes.Where(node => node.Nodes.Count > 0);

            foreach (JsTreeNode branch in branches)
                leafs.AddRange(GetAllLeafNodes(branch.Nodes));

            return leafs;
        }
        private IEnumerable<JsTreeNode> GetAllBranchNodes(IEnumerable<JsTreeNode> nodes)
        {
            List<JsTreeNode> branches = nodes.Where(node => node.Nodes.Count > 0).ToList();
            foreach (JsTreeNode branch in branches.ToArray())
                branches.AddRange(GetAllBranchNodes(branch.Nodes));

            return branches;
        }

        #endregion
    }
}
