using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Xml.XPath;
using Lab2.Areas.HelpPage.ModelDescriptions;

#pragma warning disable 1591
namespace API.Areas.HelpPage
{
	/// <summary>
	/// A custom <see cref="IDocumentationProvider"/> that reads the API documentation from an XML documentation file.
	/// </summary>
	public class XmlDocumentationProvider : IDocumentationProvider, IModelDocumentationProvider
	{
		private readonly XPathNavigator _documentNavigator;
		private readonly XPathNavigator _altenativeDocument;
		private const string TypeExpression = "/doc/members/member[@name='T:{0}']";
		private const string MethodExpression = "/doc/members/member[@name='M:{0}']";
		private const string PropertyExpression = "/doc/members/member[@name='P:{0}']";
		private const string FieldExpression = "/doc/members/member[@name='F:{0}']";
		private const string ParameterExpression = "param[@name='{0}']";

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlDocumentationProvider"/> class.
		/// </summary>
		/// <param name="documentPath">The physical path to XML document.</param>
		/// <param name="alternativeDocPath">The physical path to XML document.</param>
		public XmlDocumentationProvider(string documentPath, string alternativeDocPath)
		{
			if (documentPath == null)
			{
				throw new ArgumentNullException("documentPath");
			}
			XPathDocument xpath = new XPathDocument(documentPath);
			_documentNavigator = xpath.CreateNavigator();

			if (!String.IsNullOrEmpty(alternativeDocPath))
			{
				XPathDocument xp = new XPathDocument(alternativeDocPath);
				_altenativeDocument = xp.CreateNavigator();
			}
		}

		public string GetDocumentation(HttpControllerDescriptor controllerDescriptor)
		{
			XPathNavigator typeNode = GetTypeNode(controllerDescriptor.ControllerType);
			return GetTagValue(typeNode, "summary");
		}

		public virtual string GetDocumentation(HttpActionDescriptor actionDescriptor)
		{
			XPathNavigator methodNode = GetMethodNode(actionDescriptor);
			return GetTagValue(methodNode, "summary");
		}

		public virtual string GetDocumentation(HttpParameterDescriptor parameterDescriptor)
		{
			ReflectedHttpParameterDescriptor reflectedParameterDescriptor = parameterDescriptor as ReflectedHttpParameterDescriptor;
			if (reflectedParameterDescriptor != null)
			{
				XPathNavigator methodNode = GetMethodNode(reflectedParameterDescriptor.ActionDescriptor);
				if (methodNode != null)
				{
					string parameterName = reflectedParameterDescriptor.ParameterInfo.Name;
					XPathNavigator parameterNode = methodNode.SelectSingleNode(String.Format(CultureInfo.InvariantCulture, ParameterExpression, parameterName));
					if (parameterNode != null)
					{
						return parameterNode.Value.Trim();
					}
				}
			}

			return null;
		}

		public string GetResponseDocumentation(HttpActionDescriptor actionDescriptor)
		{
			XPathNavigator methodNode = GetMethodNode(actionDescriptor);
			return GetTagValue(methodNode, "returns");
		}

		public string GetDocumentation(MemberInfo member)
		{
			string memberName = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", GetTypeName(member.DeclaringType), member.Name);
			string expression = member.MemberType == MemberTypes.Field ? FieldExpression : PropertyExpression;
			string selectExpression = String.Format(CultureInfo.InvariantCulture, expression, memberName);
			XPathNavigator propertyNode = SelectSingleNode(selectExpression);
			return GetTagValue(propertyNode, "summary");
		}

		public string GetDocumentation(Type type)
		{
			XPathNavigator typeNode = GetTypeNode(type);
			return GetTagValue(typeNode, "summary");
		}

		private XPathNavigator GetMethodNode(HttpActionDescriptor actionDescriptor)
		{
			ReflectedHttpActionDescriptor reflectedActionDescriptor = actionDescriptor as ReflectedHttpActionDescriptor;
			if (reflectedActionDescriptor != null)
			{
				string selectExpression = String.Format(CultureInfo.InvariantCulture, MethodExpression, GetMemberName(reflectedActionDescriptor.MethodInfo));
				return SelectSingleNode(selectExpression);
			}

			return null;
		}

		private static string GetMemberName(MethodInfo method)
		{
			string name = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", GetTypeName(method.DeclaringType), method.Name);
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length != 0)
			{
				string[] parameterTypeNames = parameters.Select(param => GetTypeName(param.ParameterType)).ToArray();
				name += String.Format(CultureInfo.InvariantCulture, "({0})", String.Join(",", parameterTypeNames));
			}

			return name;
		}

		private static string GetTagValue(XPathNavigator parentNode, string tagName)
		{
			if (parentNode != null)
			{
				XPathNavigator node = parentNode.SelectSingleNode(tagName);
				if (node != null)
				{
					string value = node.InnerXml.Trim();
					return value;
				}
			}

			return null;
		}

		private XPathNavigator GetTypeNode(Type type)
		{
			string controllerTypeName = GetTypeName(type);
			string selectExpression = String.Format(CultureInfo.InvariantCulture, TypeExpression, controllerTypeName);
			return SelectSingleNode(selectExpression);
		}

		private static string GetTypeName(Type type)
		{
			string name = type.FullName;
			if (type.IsGenericType)
			{
				// Format the generic type name to something like: Generic{System.Int32,System.String}
				Type genericType = type.GetGenericTypeDefinition();
				Type[] genericArguments = type.GetGenericArguments();
				string genericTypeName = genericType.FullName;

				// Trim the generic parameter counts from the name
				genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
				string[] argumentTypeNames = genericArguments.Select(t => GetTypeName(t)).ToArray();
				name = String.Format(CultureInfo.InvariantCulture, "{0}{{{1}}}", genericTypeName, String.Join(",", argumentTypeNames));
			}
			if (type.IsNested)
			{
				// Changing the nested type name from OuterType+InnerType to OuterType.InnerType to match the XML documentation syntax.
				name = name.Replace("+", ".");
			}

			return name;
		}

		private XPathNavigator SelectSingleNode(string selectExpression)
		{
			var result = _documentNavigator.SelectSingleNode(selectExpression);
			if (result == null
				&& _altenativeDocument != null)
			{
				result = _altenativeDocument.SelectSingleNode(selectExpression);
			}
			return result;
		}
	}
}
#pragma warning restore 1591