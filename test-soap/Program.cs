﻿using System;
using System.Linq;
using OneScript.Soap;
using ScriptEngine.Machine;
using TinyXdto;

namespace testsoap
{
	class MainClass
	{

		MainClass()
		{
		}

		void CheckIValueFor<Type>()
			where Type : IValue
		{
		}

		// В случае неправильного наследования код не скомпилируется
		void Check_AllClassesIsIValues()
		{
			CheckIValueFor<ParameterImpl>();
			CheckIValueFor<ParameterCollectionImpl>();

			CheckIValueFor<ServiceImpl>();
			CheckIValueFor<ServiceCollectionImpl>();

			CheckIValueFor<ReturnValueImpl>();

			CheckIValueFor<DefinitionsImpl>();

			CheckIValueFor<EndpointImpl>();
			CheckIValueFor<EndpointCollectionImpl>();

			CheckIValueFor<OperationImpl>();
			CheckIValueFor<OperationCollectionImpl>();

			CheckIValueFor<InterfaceImpl>();
			CheckIValueFor<ProxyImpl>();
		}

		public void TestWsdlNoAuth()
		{
			var def = new DefinitionsImpl("http://vm21297.hv8.ru:10080/httpservice/ws/complex.1cws?wsdl");
			Console.WriteLine ("Def has {0} services.", def.Services.Count());
			foreach (var ivService in def.Services) {
				var service = ivService as ServiceImpl;
				Console.WriteLine ("\tService {0} has {1} endpoints", service.Name, service.Endpoints.Count());

				foreach (var ivEndpoint in service.Endpoints) {
					var endpoint = ivEndpoint as EndpointImpl;
					Console.WriteLine ("\t\tEndpoint {0} as {1} operations", endpoint.Name, endpoint.Interface.Operations.Count());
					foreach (var ivOperation in endpoint.Interface.Operations) {
						var operation = ivOperation as OperationImpl;
						Console.WriteLine ("\t\t\tOperation {0}", operation.Name);
					}
				}
			}
		}

		private void StartEngine ()
		{
			var engine = new ScriptEngine.HostedScript.HostedScriptEngine ();
			engine.Initialize ();
			engine.AttachAssembly (System.Reflection.Assembly.GetAssembly (typeof (XdtoObjectTypeImpl)));
			engine.AttachAssembly (System.Reflection.Assembly.GetAssembly (typeof (DefinitionsImpl)));
		}

		public void Run()
		{
			Check_AllClassesIsIValues();

			StartEngine ();
			TestWsdlNoAuth ();
		}

		public static void Main(string[] args)
		{
			var main = new MainClass();
			main.Run();
		}
	}
}