﻿using System;
using OneScript.Soap;
using ScriptEngine.Machine;

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

		}

		public void Run()
		{
			Check_AllClassesIsIValues();
		}

		public static void Main(string[] args)
		{
			var main = new MainClass();
			main.Run();
		}
	}
}
