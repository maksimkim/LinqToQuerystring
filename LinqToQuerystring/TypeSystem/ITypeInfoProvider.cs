namespace LinqToQuerystring.TypeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface ITypeInfoProvider
    {
        IEnumerable<PropertyInfo> GetProperties(Type type);

        string GetPropertyName(PropertyInfo property);
    }
}