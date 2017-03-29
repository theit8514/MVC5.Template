using MvcTemplate.Components.Mvc;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;

namespace MvcTemplate.Tests.Unit.Components.Mvc
{
    public class MvcSiteMapParserTests
    {
        #region GetNodeTree(XElement siteMap)

        [Fact]
        public void GetNodes_ReturnsAllSiteMapNodes()
        {
            using (IEnumerator<MvcSiteMapNode> actual = ToEnumerable(new MvcSiteMapParser().GetNodeTree(CreateSiteMap())).GetEnumerator())
            using (IEnumerator<MvcSiteMapNode> expected = ToEnumerable(GetExpectedNodeTree()).GetEnumerator())
            {
                while (expected.MoveNext() | actual.MoveNext())
                {
                    Assert.Equal(expected.Current.Controller, actual.Current.Controller);
                    Assert.Equal(expected.Current.IconClass, actual.Current.IconClass);
                    Assert.Equal(expected.Current.IsMenu, actual.Current.IsMenu);
                    Assert.Equal(expected.Current.Action, actual.Current.Action);
                    Assert.Equal(expected.Current.Area, actual.Current.Area);

                    if (expected.Current.Parent != null || actual.Current.Parent != null)
                    {
                        Assert.Equal(expected.Current.Parent.Controller, actual.Current.Parent.Controller);
                        Assert.Equal(expected.Current.Parent.IconClass, actual.Current.Parent.IconClass);
                        Assert.Equal(expected.Current.Parent.IsMenu, actual.Current.Parent.IsMenu);
                        Assert.Equal(expected.Current.Parent.Action, actual.Current.Parent.Action);
                        Assert.Equal(expected.Current.Parent.Area, actual.Current.Parent.Area);
                    }
                }
            }
        }

        #endregion

        #region Test helpers

        private XElement CreateSiteMap()
        {
            return XElement.Parse(
            @"<siteMap>
              <siteMapNode controller=""Home"" action=""Index"" menu=""false"" icon=""fa fa-home"">
                <siteMapNode menu=""true"" area=""Administration"" icon=""fa fa-gears"">
                  <siteMapNode controller=""Accounts"" action=""Index"" menu=""false"" area=""Administration"" icon=""fa fa-user"">
                    <siteMapNode controller=""Accounts"" action=""Create"" menu=""true"" area=""Administration"" icon=""fa fa-file-o"" />
                  </siteMapNode>
                </siteMapNode>
              </siteMapNode>
            </siteMap>");
        }
        private IEnumerable<MvcSiteMapNode> GetExpectedNodeTree()
        {
            MvcSiteMapNode[] map =
            {
                new MvcSiteMapNode
                {
                    IconClass = "fa fa-home",

                    Controller = "Home",
                    Action = "Index",

                    Children = new List<MvcSiteMapNode>
                    {
                        new MvcSiteMapNode
                        {
                            IconClass = "fa fa-gears",
                            IsMenu = true,

                            Area = "Administration",

                            Children = new List<MvcSiteMapNode>
                            {
                                new MvcSiteMapNode
                                {
                                    IconClass = "fa fa-user",
                                    IsMenu = false,

                                    Area = "Administration",
                                    Controller = "Accounts",
                                    Action = "Index",

                                    Children = new List<MvcSiteMapNode>
                                    {
                                        new MvcSiteMapNode
                                        {
                                            IconClass = "fa fa-file-o",
                                            IsMenu = true,

                                            Area = "Administration",
                                            Controller = "Accounts",
                                            Action = "Create",

                                            Children = new List<MvcSiteMapNode>()
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            foreach (MvcSiteMapNode level1 in map)
            {
                foreach (MvcSiteMapNode level2 in level1.Children)
                {
                    level2.Parent = level1;

                    foreach (MvcSiteMapNode level3 in level2.Children)
                    {
                        level3.Parent = level2;

                        foreach (MvcSiteMapNode level4 in level3.Children)
                        {
                            level4.Parent = level3;
                        }
                    }
                }
            }

            return map;
        }

        private IEnumerable<MvcSiteMapNode> ToEnumerable(IEnumerable<MvcSiteMapNode> nodes)
        {
            List<MvcSiteMapNode> list = new List<MvcSiteMapNode>();
            foreach (MvcSiteMapNode node in nodes)
            {
                list.Add(node);
                list.AddRange(ToEnumerable(node.Children));
            }

            return list;
        }

        #endregion
    }
}
