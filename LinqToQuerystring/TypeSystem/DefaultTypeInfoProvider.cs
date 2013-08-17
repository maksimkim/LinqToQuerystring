namespace LinqToQuerystring.TypeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;

    public class DefaultTypeInfoProvider : ITypeInfoProvider
    {
        public virtual string GetPropertyName(PropertyInfo property)
        {
            DataMemberAttribute attr;

            return 
                (attr = property.GetCustomAttribute<DataMemberAttribute>()) != null && !string.IsNullOrWhiteSpace(attr.Name)
                    ? attr.Name
                    : property.Name;
        }

        public virtual IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }
    }
}