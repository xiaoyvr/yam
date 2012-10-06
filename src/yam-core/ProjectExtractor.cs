using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.XPath;

namespace Yam.Core
{
    public class ProjectExtractor
    {
        private readonly ResolveConfig config;
        private readonly XPathNavigator navigator;
        private readonly XmlNamespaceManager ns;

        public ProjectExtractor(string projectPath, ResolveConfig config)
        {
            this.config = config;
            try
            {
                var pathDocument = new XPathDocument(projectPath);
                navigator = pathDocument.CreateNavigator();
                if (navigator.NameTable != null) 
                    ns = new XmlNamespaceManager(navigator.NameTable);
                ns.AddNamespace("n", "http://schemas.microsoft.com/developer/msbuild/2003");
            }
            catch (Exception exception)
            {
                throw new Exception(projectPath, exception);
            }
        }

        public Guid GetId()
        {
            var node = navigator.SelectSingleNode("/n:Project/n:PropertyGroup/n:ProjectGuid", ns);
            if (node != null) 
                return new Guid(node.Value);
            throw new ApplicationException("Project Guid is empty!");
        }

        public IEnumerable<ReferenceNode> GetAssemblyReferenceNodes()
        {
            return GetNodesByXPath("/n:Project/n:ItemGroup/n:Reference").Select(CreateNode);
        }


        public IEnumerable<ReferenceNode> GetProjectReferenceNodes()
        {
            return GetNodesByXPath("/n:Project/n:ItemGroup/n:ProjectReference").Select(CreateNode);
        }

        public IEnumerable<ReferenceNode> GetCommonRuntimeReferenceNodes()
        {
            return GetNodesByXPath("/n:Project/n:ProjectExtensions/n:Runtime/n:Common/n:Reference").Select(CreateNode);
        }

        public IEnumerable<ReferenceNode> GetRuntimeReferenceNodes(string profile)
        {
            return
                GetNodesByXPath(string.Format("/n:Project/n:ProjectExtensions/n:Runtime/n:{0}/n:Reference", profile)).Select(CreateNode);
        }

        public IEnumerable<ReferenceNode> GetDefaultRuntimeReferenceNodes()
        {
            return
               GetNodesByXPath("/n:Project/n:ProjectExtensions/n:Runtime/n:Default/n:Reference").Select(CreateNode);
        }

        private ReferenceNode CreateNode(XPathNavigator nav)
        {
            var hintPathNode = nav.SelectSingleNode("n:HintPath", ns);
            var isPrivateNode = nav.SelectSingleNode("n:Private", ns);
            var name = GetSimpleReferenceName(nav.GetAttribute("Include", ""));
            return new ReferenceNode
                       {
                           Include = name,
                           HintPath = hintPathNode == null ? string.Empty : hintPathNode.Value,
                           IsPrivate = isPrivateNode == null ? GetDefaultPrivateValue(name) : bool.Parse(isPrivateNode.Value)
                       };
        }

        private bool GetDefaultPrivateValue(string name)
        {
            return !config.IsGAC(name);
        }

        private IEnumerable<XPathNavigator> GetNodesByXPath(string xPath)
        {
            var iterator = navigator.Select(xPath, ns);
            while (iterator.MoveNext())
            {
                yield return iterator.Current;
            }
        }
        private static string GetSimpleReferenceName(string referenceName)
        {
            var strings = referenceName.Split(',');
            return strings[0].Trim();
        }

       
    }
}