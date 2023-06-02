using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroQL.TestApp
{
    internal class RandomQueryMethod
    {
        public RandomQueryMethod()
        {
            IParser parser = new Parser();

            var result = parser.Query("locationId", false, 1);
        }
    }

    public interface IParser
    {
        Task<object> Query(string locationId, bool incremental, int limit);
    }

    public class Parser : IParser
    {
        public Task<object> Query(string locationId, bool incremental, int limit)
        {
            return (Task<object>)Task.CompletedTask;
        }
    }
}
