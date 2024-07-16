using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XperienceCommunity.DataContext.Interfaces
{
    public interface IReusableSchemaContext<T> : IDataContext<T>
    {
        IDataContext<T> WithReusableSchemas(params string[] schemaNames);
    }
}
