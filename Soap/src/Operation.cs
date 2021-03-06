﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.Machine;
using System.Web.Services.Description;
using System.Collections.Generic;
using ScriptEngine.HostedScript.Library.Xml;
using TinyXdto;

namespace OneScript.Soap
{
	[ContextClass("WSОперация", "WSOperation")]
	public class Operation : AutoContext<Operation>, IWithName
	{
		private readonly Dictionary<string, int> _indexes = new Dictionary<string, int> ();

		internal Operation(System.Web.Services.Description.Operation operation, XdtoFactory factory)
		{
			Name = operation.Name;
			NamespaceUri = operation.PortType.ServiceDescription.TargetNamespace;
			Documentation = operation.Documentation;
			ReturnValue = new ReturnValue (operation.Messages.Output, factory);

			Parameters = ParameterCollection.Create (operation.Messages.Input,
			                                             ReturnValue,
			                                             factory);

			int argumentIndex = 0;
			foreach (var messagePart in Parameters.Parts) {
				foreach (var param in messagePart.Parameters) {
					_indexes.Add (param.Name, argumentIndex);
					++argumentIndex;
				}
			}
		}

		internal Operation (MethodInfo methodInfo, string namespaceUri)
		{
			Name = methodInfo.Name;
			Documentation = "";
			NamespaceUri = namespaceUri;
			ReturnValue = new ReturnValue (
				null,
				$"{Name}ResponseMessage",
				nillable: true);

			var messagePart = new MessagePartProxy ();
			messagePart.Name = "parameters";
			messagePart.ElementName = methodInfo.Name;
			messagePart.NamespaceUri = namespaceUri;
			messagePart.Parameters = new List<Parameter> ();

			int argumentIndex = 0;
			foreach (var paramInfo in methodInfo.Params) {
				var paramName = string.Format ("p{0}", argumentIndex);
				var param = new Parameter (paramName,
											   paramInfo.IsByValue
				                               	? ParameterDirectionEnum.In
				                               	: ParameterDirectionEnum.InOut,
				                               true,
				                               Documentation);

				messagePart.Parameters.Add (param);
				_indexes.Add (paramName, argumentIndex);
				++argumentIndex;
			}

			Parameters = new ParameterCollection (messagePart.Parameters,
													  new MessagePartProxy [] { messagePart });
		}

		[ContextProperty("ВозвращаемоеЗначение", "ReturnValue")]
		public ReturnValue ReturnValue { get; }

		[ContextProperty("Документация", "Documentation")]
		public string Documentation { get; }

		[ContextProperty("Имя", "Name")]
		public string Name { get; }

		[ContextProperty("Параметры", "Parameters")]
		public ParameterCollection Parameters { get; }

		public string NamespaceUri { get; }

		public void WriteRequestBody(XmlWriterImpl writer,
			XdtoSerializerImpl serializer,
			IValue [] arguments)
		{

			foreach (var messagePart in Parameters.Parts) {

				writer.WriteStartElement (messagePart.ElementName, messagePart.NamespaceUri);

				foreach (var param in messagePart.Parameters) {

					if (param.ParameterDirection == ParameterDirectionEnum.Out)
						continue;

					var argumentIndex = _indexes [param.Name];
					var typeAssignment = XmlTypeAssignmentEnum.Implicit;

					if (param.Type is XdtoValueType)
						typeAssignment = XmlTypeAssignmentEnum.Explicit;
					serializer.WriteXml (writer, arguments [argumentIndex], param.Name, messagePart.NamespaceUri, typeAssignment);

				}

				writer.WriteEndElement (); // messagePart.ElementName

			}

		}

		// Особенности реализации: возвращаемое значение и исходящие параметры
		// передаём ОДНИМ сообщением, хотя протокол разрешает несколько сообщений

		public void WriteResponseBody (XmlWriterImpl writer,
									   XdtoSerializerImpl serializer,
									   IValue returnValue,
									   IValue [] arguments)
		{
			writer.WriteStartElement (ReturnValue.MessagePartName, NamespaceUri);

			serializer.WriteXml (writer, returnValue, "return", NamespaceUri);
			foreach (var param in Parameters) {
				if (param.ParameterDirection == ParameterDirectionEnum.In)
					continue;

				var argumentIndex = _indexes [param.Name];
				serializer.WriteXml (writer, arguments [argumentIndex], param.Name, NamespaceUri);
			}

			writer.WriteEndElement (); // messagePartName
		}

		public IParsedResponse ParseResponse(XmlReaderImpl reader, XdtoSerializerImpl serializer)
		{

			var retValue = ValueFactory.Create ();
			var outputParams = new Dictionary<int, IValue> ();

			var xmlNodeTypeEnum = XmlNodeTypeEnum.CreateInstance ();
			var xmlElementStart = xmlNodeTypeEnum.FromNativeValue (System.Xml.XmlNodeType.Element);

			if (!reader.Read ()
				|| !reader.LocalName.Equals ("Envelope")
				|| !reader.NodeType.Equals (xmlElementStart)
			   // TODO: перевести XML на простые перечисления
			   ) {
				return new SoapExceptionResponse ("Wrong response!");
			}
			
			reader.MoveToContent ();

			if (!reader.Read ()
				|| !reader.LocalName.Equals ("Body")
				|| !reader.NodeType.Equals (xmlElementStart)
			   // TODO: перевести XML на простые перечисления
			   ) {
				return new SoapExceptionResponse ("Wrong response!");
			}

			if (!reader.Read())
			{
				return new SoapExceptionResponse ("Wrong response!");
			}

			if (reader.LocalName.Equals("Fault")
			    && reader.NodeType.Equals(xmlElementStart))
			{
				reader.Read();
				while (!(reader.LocalName.Equals("faultString")
				                          && reader.NodeType.Equals(xmlElementStart)))
				{
					if (!reader.Read())
					{
						return new SoapExceptionResponse ("Wrong response!");
					}
				}
				reader.Read();
				var faultString = reader.Value;
				return new SoapExceptionResponse (faultString);
			}

			var xdtoResult = serializer.XdtoFactory.ReadXml (reader, ReturnValue.Type) as XdtoDataObject;
			retValue = xdtoResult.Get ("return");

			if (retValue is IXdtoValue) {
				retValue = serializer.ReadXdto (retValue as IXdtoValue);
			}

			foreach (var param in Parameters) {
				if (param.ParameterDirection == ParameterDirectionEnum.In)
					continue;

				var argumentIndex = _indexes [param.Name];
				IValue paramValue = xdtoResult.Get (param.Name);
				if (paramValue is IXdtoValue) {
					paramValue = serializer.ReadXdto (paramValue as IXdtoValue);
				}
				outputParams.Add (argumentIndex, paramValue);
			}

			return new SuccessfulSoapResponse(retValue, outputParams);
		}

	}
}
