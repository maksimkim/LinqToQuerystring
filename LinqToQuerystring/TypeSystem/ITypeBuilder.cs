namespace LinqToQuerystring.TypeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface ITypeBuilder
    {
        Type Build(string name, Type proptotype, IEnumerable<PropertyInfo> properties);
    }
}