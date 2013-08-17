namespace LinqToQuerystring.TypeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    public class DefaultTypeBuilder : ITypeBuilder
    {
        private const MethodAttributes GetSetAttr = MethodAttributes.Final | MethodAttributes.Public;

        private static readonly ModuleBuilder ModuleBuilder;

        static DefaultTypeBuilder()
        {
            var asmName = new AssemblyName("DynamicTypes" + Guid.NewGuid());

            ModuleBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run).DefineDynamicModule(asmName.Name);
        }

        public Type Build(string name, Type proptotype, IEnumerable<PropertyInfo> properties)
        {
            Contract.Assert(properties != null && properties.Any());
            
            var typeBuilder = ModuleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

            SetAttributes(proptotype, typeBuilder);

            foreach (var property in properties)
                CreateProperty(property, typeBuilder);

            return typeBuilder.CreateType();
        }

        private static void CreateProperty(PropertyInfo prototype, TypeBuilder typeBuilder)
        {
            var name = prototype.Name;
            var type = prototype.PropertyType;

            var fieldBuilder = typeBuilder.DefineField("_" + name, type, FieldAttributes.Private);

            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, type, null);

            Contract.Assume(propertyBuilder != null, "Created above.");

            SetAttributes(prototype, propertyBuilder);

            var getAccessor = typeBuilder.DefineMethod(
                "get_" + name,
                GetSetAttr,
                type,
                Type.EmptyTypes);

            var getIl = getAccessor.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            var setAccessor = typeBuilder.DefineMethod(
                "set_" + name,
                GetSetAttr,
                null,
                new[] { type });

            var setIl = setAccessor.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getAccessor);
            propertyBuilder.SetSetMethod(setAccessor);
        }

        private static void SetAttributes(MemberInfo prototype, TypeBuilder typeBuilder)
        {
            var attrs = prototype.GetCustomAttributesData();

            var builders = CreateCustomAttributeBuilders(attrs);

            foreach (var builder in builders)
                typeBuilder.SetCustomAttribute(builder);
        }

        private static void SetAttributes(MemberInfo prototype, PropertyBuilder propertyBuilder)
        {
            var attrs = prototype.GetCustomAttributesData();

            var builders = CreateCustomAttributeBuilders(attrs).ToArray();

            foreach (var builder in builders)
                propertyBuilder.SetCustomAttribute(builder);
        }

        private static IEnumerable<CustomAttributeBuilder> CreateCustomAttributeBuilders(IEnumerable<CustomAttributeData> attrs)
        {
            var builders = 
                attrs
                    .Select(attr =>
                    {
                        var namedArguments = attr.NamedArguments;
                        var properties = namedArguments.Select(a => a.MemberInfo).OfType<PropertyInfo>().ToArray();
                        var values = namedArguments.Select(a => a.TypedValue.Value).ToArray();
                        var constructorArgs = attr.ConstructorArguments.Select(a => a.Value).ToArray();
                        var constructor = attr.Constructor;
                        return new CustomAttributeBuilder(constructor, constructorArgs, properties, values);
                    });

            return builders;
        }
    }
}