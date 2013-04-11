using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace GumUnitTests
{
    public static class TestFramework
    {
        public static void RunTests()
        {
            //Loop through all test fixtures and run tests on them
            foreach (var type in GetTypesWith<TestFixtureAttribute>(true))
            {
                RunTests(type);
            }
        }

        public static void RunTests(Type t)
        {
            //Create test fixture
            object instance = Activator.CreateInstance(t);

            //Run all tests in test fixture
            RunTestsOn(instance);
        }

        public static void RunTests<T>()
        {
            //Create test fixture
            object instance = Activator.CreateInstance(typeof(T));

            //Run all tests in test fixture
            RunTestsOn(instance);
        }

        public static void RunTests<T>(string methodName)
        {
            //Create test fixture
            object instance = Activator.CreateInstance(typeof(T));

            //Run single test in test fixture
            RunTestsOn(instance, methodName);
        }

        #region Private

        private static void RunTestsOn(object testFixture, string methodName)
        {
            RunSetup(testFixture);

            //Get requested method
            var testMethod = testFixture.GetType().GetMethod(methodName);

            //Run method if it exists, otherwise alert that the test doesn't exist
            if (testMethod != null)
            {
                testMethod.Invoke(testFixture, null);
            }
            else
            {
                throw new Exception("Test not found");
            }
        }

        private static void RunTestsOn(object testFixture)
        {
            RunSetup(testFixture);

            //Loop through all methods and run the test methods
            foreach (var method in from method
                                     in testFixture.GetType().GetMethods()
                                   let attributes = method.GetCustomAttributes(typeof(TestAttribute), true)
                                   where attributes.Length != 0
                                   select method)
            {
                method.Invoke(testFixture, null);
            }
        }

        private static void RunSetup(object testFixture)
        {
            //Loop through all methods and run the setup methods
            foreach (var method in from method
                                     in testFixture.GetType().GetMethods()
                                   let attributes = method.GetCustomAttributes(typeof(TestFixtureSetUpAttribute), true)
                                   where attributes.Length != 0
                                   select method)
            {
                method.Invoke(testFixture, null);
            }
        }

        /// <summary>
        /// Retrieves all types that are marked with an attribute
        /// </summary>
        /// <typeparam name="TAttribute">Attribute to check for</typeparam>
        /// <param name="inherit">Whether to check the inheritance chain for the attribute</param>
        /// <returns>list of types with the attribute</returns>
        private static IEnumerable<Type> GetTypesWith<TAttribute>(bool inherit) where TAttribute : Attribute
        {
            return from t in Assembly.GetAssembly(typeof(TestFramework)).GetTypes()
                   where t.IsDefined(typeof(TAttribute), inherit)
                   select t;
        }

        #endregion
    }
}
